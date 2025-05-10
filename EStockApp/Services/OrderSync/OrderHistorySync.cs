using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using EStockApp.Models;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EStockApp.Services.OrderSync;

public static class OrderHistorySync
{
    private const string _loginUrl = "https://passport.jlc.com/login?redirectUrl=http%3A%2F%2Forder.szlcsc.com%2Fmember%2Forder%2Flist.html&appId=LC_PUB&backCode=1";
    private const string _listUrl = "https://order.szlcsc.com/member/history/list.html";

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private static async Task SaveCookies(IEnumerable<CookieParam> cookies)
    {
        await File.WriteAllTextAsync(".cookie.data", JsonSerializer.Serialize(cookies, options: _jsonSerializerOptions));
    }

    private static async Task<CookieParam[]?> GetCookies()
    {
        if (!File.Exists(".cookie.data"))
            return null;

        var text = await File.ReadAllTextAsync(".cookie.data");

        try
        {
            return JsonSerializer.Deserialize<CookieParam[]>(text, options: _jsonSerializerOptions)!;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task ShowLoginAsync()
    {
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = false,
            DefaultViewport = null,
            Args = new[] { "--window-size=1024,650" },
            Timeout = 60 * 1000,
        });

        await using var page = await browser.NewPageAsync();
        await InitialPageAsync(page);

        var response = await page.GoToAsync(_loginUrl, waitUntil: WaitUntilNavigation.DOMContentLoaded);

        if (response.Status != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("访问异常");
        }

        while (true)
        {
            if (!page.Url.StartsWith("https://passport.jlc.com/login"))
            {
                await SaveCookies(await page.GetCookiesAsync());

                await page.CloseAsync();
                await browser.CloseAsync();

                break;
            }

            await Task.Delay(500);
        }
    }

    public static async IAsyncEnumerable<OrderHistorySyncResult> RunAsync(DateOnly startDate)
    {
        var browserFetcher = new BrowserFetcher();
        if (!browserFetcher.GetInstalledBrowsers().Any())
        {
            yield return new OrderHistorySyncResult(OrderHistorySyncStatus.Tips, "初始化...");
            await browserFetcher.DownloadAsync();
        }

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            DefaultViewport = null,
            Args = new[] { "--window-size=1024,650" },
            Timeout = 60 * 1000,
            // UserDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".puppeteer/user-data/{_sessionId}")
        });

        await using var page = await browser.NewPageAsync();
        await InitialPageAsync(page);

        var response = await page.GoToAsync(_listUrl, waitUntil: WaitUntilNavigation.DOMContentLoaded);

        if (response.Status != System.Net.HttpStatusCode.OK)
        {
            yield return new OrderHistorySyncResult(OrderHistorySyncStatus.RequreLogin, "请先登录");
        }

        if (!response.Url.StartsWith(_listUrl))
        {
            yield return new OrderHistorySyncResult(OrderHistorySyncStatus.RequreLogin, "请先登录");
        }

        //await page.EvaluateExpressionAsync($"document.querySelector('#start-date').value = '{startDate.ToString("yyyy-MM-dd")}'");
        //await page.ClickAsync(".to-query-btn");

        await Task.Delay(1000);

        yield return new OrderHistorySyncResult(OrderHistorySyncStatus.Tips, "正在加载...");

        var apiPage = await browser.NewPageAsync();
        await InitialPageAsync(apiPage);

        await foreach (var item in AnalyzeHtml(page, apiPage, startDate, pageNum: 1))
        {
            yield return item;
        }
    }

    private static async Task InitialPageAsync(IPage page)
    {
        //await page.SetViewportAsync(new ViewPortOptions() { Width = 1400, Height = 800 });
        await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.0.0");
        await page.SetCookieAsync(await GetCookies() ?? new CookieParam[0]);
    }

    private static async IAsyncEnumerable<OrderHistorySyncResult> AnalyzeHtml(IPage listPage, IPage apiPage, DateOnly startDate, int pageNum = 1)
    {
        // https://order.szlcsc.com/member/history/list.html?currentPage=1&catalogId=&productCode=&searchKeywords=&orderTimeBegin=2025-02-11

        var response = await listPage.GoToAsync($"https://order.szlcsc.com/member/history/list.html?currentPage={pageNum}&catalogId=&productCode=&searchKeywords=&orderTimeBegin={startDate.ToString("yyyy-MM-dd")}", waitUntil: WaitUntilNavigation.DOMContentLoaded);

        if (!response.Url.StartsWith("https://order.szlcsc.com/member/history/list.html"))
        {
            yield return new OrderHistorySyncResult(OrderHistorySyncStatus.Error, "查询失败");
        }

        await SaveCookies(await listPage.GetCookiesAsync());

        var source = await response.TextAsync();

        HtmlParser htmlParser = new HtmlParser();
        using var document = await htmlParser.ParseDocumentAsync(source);

        foreach (var item in document.QuerySelectorAll(".love-item"))
        {
            await Task.Delay(500);

            var href = item.QuerySelector("a")!.GetAttribute("href");

            var productId = ParseNumber(Url.Create(href!).Path.Replace(".html", null));

            yield return await QueryOrderItemAsync(apiPage, productId);
        }

        // next page
        var nextPage = document.QuerySelector(".page-link-page-util a[title='下一页']");
        if (nextPage != null)
        {
            await foreach (var item in AnalyzeHtml(listPage, apiPage, startDate, pageNum + 1))
            {
                yield return item;
            }
        }
    }

    private static async Task<OrderHistorySyncResult> QueryOrderItemAsync(IPage page, int productId)
    {
        var response = await page.GoToAsync($"https://order.szlcsc.com/async/order/product/history/load?currPage=1&productId={productId}");

        if (response.Status != System.Net.HttpStatusCode.OK)
        {
            return new OrderHistorySyncResult(OrderHistorySyncStatus.Error, $"获取{productId}失败: {response.Status}");
        }

        try
        {
            var tmp = await response.JsonAsync<ApiResult<ProductHistory>>();

            if (tmp?.Code != 200)
            {
                return new OrderHistorySyncResult(OrderHistorySyncStatus.Error, $"获取{productId}失败: {tmp?.Code}");
            }

            if (tmp.Result?.DataList == null || tmp.Result?.DataList?.Length == 0)
            {
                return new OrderHistorySyncResult(OrderHistorySyncStatus.Error, $"获取{productId}失败");
            }

            var dataList = tmp.Result!.DataList ?? new ProductHistory[0];
            var orderNos = dataList.Select(x => x.OrderCode).ToArray();

            var totalCount = dataList.Sum(x => x.FinalNumber);

            var item = new SyncOrderItemModel()
            {
                BrandName = dataList[0].BrandName,
                Category = dataList[0].CatalogName,
                TotalCount = totalCount,
                OrderCodes = orderNos,
                Pack = dataList[0].EncapStandard,
                ProductCode = dataList[0].ProductCode,
                ProductId = dataList[0].ProductId,
                ProductModel = dataList[0].ProductModel,
                ProductName = dataList[0].ProductName,
            };

            return new OrderHistorySyncResult(OrderHistorySyncStatus.Success, $"获取{productId}成功: {item.ProductCode} - {item.TotalCount}") { OrderItem = item };
        }
        catch (Exception ex)
        {
            return new OrderHistorySyncResult(OrderHistorySyncStatus.Error, $"获取{productId}失败: {ex.Message}");
        }
    }

    //static void AnalyzeString() {

    //var responseText = await response.TextAsync();

    //var jObject = JsonObject.Parse(responseText)!;

    //if (jObject!["code"]!.GetValue<int>() != 0)
    //{
    //    continue;
    //}

    //jObject["result"][""]

    //商品编号：C17955
    //贴片电阻
    //品牌：UNI - ROYAL(厚声)
    //20Ω ±1 % 250mW 厚膜电阻 编带
    //封装：1206

    //var b = item.QuerySelector(".love-b");
    //var c = item.QuerySelector(".love-c");

    //if (b == null || c == null)
    //{
    //    continue;
    //}

    //var bText = b.TextContent.Trim();
    //var cText = c.TextContent.Trim();

    //var lines = bText.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    //if (lines.Length != 5)
    //{
    //    continue;
    //}

    //var sku = lines[0].Remove(0, "商品编号：".Length);
    //var category = lines[1];
    //var brand = lines[2].Remove(0, "品牌：".Length);
    //var title = lines[3];
    //var pack = lines[4].Remove(0, "封装：".Length);

    //lines = cText.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    //var count = ParseNumber(lines[2].Remove(0, "数量：".Length));

    //Console.WriteLine($"{sku} {category} {brand} {title} {pack} {count}");
    // }

    private static int ParseNumber(string value)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in value)
        {
            if (char.IsDigit(item))
            {
                sb.Append(item);
            }
            else
            {
                break;
            }
        }
        return int.Parse(sb.ToString());
    }

    public class ApiResult<T>
    {
        public int Code { get; set; }
        public ApiPagerResult<T> Result { get; set; } = null!;
    }

    public class ApiPagerResult<T>
    {
        public T[] DataList { get; set; } = null!;
    }

    private class ProductHistory
    {
        public string OrderCode { get; set; } = null!;
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string CatalogName { get; set; } = null!;
        public string BrandName { get; set; } = null!;
        public string EncapStandard { get; set; } = null!;
        public string ProductModel { get; set; } = null!;
        public int FinalNumber { get; set; }
    }
}
