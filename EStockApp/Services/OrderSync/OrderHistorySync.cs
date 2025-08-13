using EStockApp.Models;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EStockApp.Services.OrderSync;

public class BrowserOrderHistorySync : IOrderHistorySync
{
    private const string _loginUrl = "https://passport.jlc.com/login?redirectUrl=https://member.szlcsc.com/member/history/list.html&appId=LC_PUB&backCode=1";
    private const string _historyUrl = "https://order-api.szlcsc.com/member/history/list";
    private const string _orderUrl = "https://order-api.szlcsc.com/member/order/details";
    private const string _orderListUrl = "https://order-api.szlcsc.com/member/order/list";

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    static BrowserOrderHistorySync()
    {
        _jsonSerializerOptions.Converters.Add(new CustomDateTimeConverter());
    }

    public async IAsyncEnumerable<OrderProductHistorySyncResult> GetHistoriesAsync(DateOnly startDate)
    {
        yield return new OrderProductHistorySyncResult(SyncStatus.Tips, "初始化...");

        await using var browser = await CreateBrowserAsync();

        await using var page = await browser.NewPageAsync();
        await InitialPageAsync(page);

        var response = await page.GoToAsync(_historyUrl, waitUntil: WaitUntilNavigation.DOMContentLoaded);

        if (response.Status != System.Net.HttpStatusCode.OK)
        {
            yield return new OrderProductHistorySyncResult(SyncStatus.Error, $"访问异常：{response.Status}");
        }

        if ((await response.TextAsync()).Contains("needLogin"))
        {
            yield return new OrderProductHistorySyncResult(SyncStatus.Tips, $"请登录...");
            await ShowLoginAsync();
        }

        await Task.Delay(1000);

        yield return new OrderProductHistorySyncResult(SyncStatus.Tips, "正在加载...");

        var apiPage = await browser.NewPageAsync();
        await InitialPageAsync(apiPage);

        await foreach (var item in ListOrderProductsAsync(page, startDate, pageNum: 1))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<OrderInfoSyncResult> GetOrderListAsync(DateOnly startDate)
    {
        yield return new OrderInfoSyncResult(SyncStatus.Tips, "初始化...");

        await using var browser = await CreateBrowserAsync();

        await using var page = await browser.NewPageAsync();
        await InitialPageAsync(page);

        var response = await page.GoToAsync(_orderListUrl, waitUntil: WaitUntilNavigation.DOMContentLoaded);

        if (response.Status != System.Net.HttpStatusCode.OK)
        {
            yield return new OrderInfoSyncResult(SyncStatus.Error, $"访问异常：{response.Status}");
        }

        if ((await response.TextAsync()).Contains("needLogin"))
        {
            yield return new OrderInfoSyncResult(SyncStatus.Tips, $"请登录...");
            await ShowLoginAsync();
        }

        await Task.Delay(1000);

        yield return new OrderInfoSyncResult(SyncStatus.Tips, "正在加载...");

        var apiPage = await browser.NewPageAsync();
        await InitialPageAsync(apiPage);

        await foreach (var item in ListOrdersAsync(page, startDate, pageNum: 1))
        {
            yield return item;
        }
    }


    public async Task<OrderInfoSyncModel> GetOrderAsync(string id, bool loadItems = false)
    {
        await using var browser = await CreateBrowserAsync();

        await using var page = await browser.NewPageAsync();

        return await GetOrderAsync(page, id, loadItems);
    }

    private async Task<OrderInfoSyncModel> GetOrderAsync(IPage page, string id, bool loadItems = false)
    {
        await InitialPageAsync(page);

        var url = $"{_orderUrl}?uuid={id}";

        var response = await page.GoToAsync(url, waitUntil: WaitUntilNavigation.DOMContentLoaded);

        if (response.Status != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("访问异常");
        }

        if ((await response.TextAsync()).Contains("needLogin"))
        {
            throw new Exception("请先登录");
        }

#if DEBUG
        var text = await response.TextAsync();
#endif

        var json = await response.JsonAsync();

        var result = json.RootElement.Deserialize<ApiResult>(_jsonSerializerOptions);
        if (result?.Code != 200)
        {
            throw new Exception("查询失败：" + result!.Code);
        }

        var order = json.RootElement.GetProperty("result").GetProperty("orderVO").Deserialize<OrderDetailsVO>(_jsonSerializerOptions)!;

        var orderInfo = new OrderInfoSyncModel
        {
            OrderId = id,
            OrderNo = order.OrderCode!,
            RealPrice = order.OrderTotalMoney,
            TotalDiscount = order.UseCouponMoney,
            TotalPrice = order.ProductTotalMoney,
            OrderTime = order.OrderTime,
            // ItemsCount = order.ZhNormalProductList.Count, 
        };

        if (loadItems)
        {
            var products = json.RootElement.GetProperty("result").GetProperty("zhNormalProductList").Deserialize<ProductItem[]>(_jsonSerializerOptions)!;
            orderInfo.ItemsCount = products.Length;

            foreach (var item in products)
            {
                orderInfo.Products.Add(new OrderProductItemSyncModel
                {
                    OrderId = id,
                    OrderNumber = orderInfo.OrderNo,

                    BrandName = item.BrandName,
                    Category = item.CatalogName,
                    TotalCount = item.FinalNumber,
                    Pack = item.EncapStandard,
                    ProductCode = item.ProductCode,
                    ProductId = item.ProductId,
                    ProductModel = item.ProductModel,
                    ProductName = item.ProductName,
                    // OrderTime = item.OrderTime,
                    Price = item.ProductPrice,
                    TotalPrice = item.ProductTotalMoney,
                    StockUnitName = item.StockUnitName,
                });
            }
        }

        return orderInfo;
    }

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

    protected static async Task ShowLoginAsync()
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
            if (page == null)
                break;

            if (!page.Url.StartsWith("https://passport.jlc.com"))
            {
                await SaveCookies(await page.GetCookiesAsync());

                await page.CloseAsync();
                await browser.CloseAsync();

                break;
            }

            await Task.Delay(500);
        }
    }

    private static async Task<IBrowser> CreateBrowserAsync()
    {
        var browserFetcher = new BrowserFetcher();
        if (!browserFetcher.GetInstalledBrowsers().Any())
        {
            await browserFetcher.DownloadAsync();
        }

        return await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            DefaultViewport = null,
            Args = new[] { "--window-size=1024,650" },
            Timeout = 60 * 1000,
            // UserDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".puppeteer/user-data/{_sessionId}")
        });

    }

    private static async Task InitialPageAsync(IPage page)
    {
        //await page.SetViewportAsync(new ViewPortOptions() { Width = 1400, Height = 800 });
        await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.0.0");
        await page.SetCookieAsync(await GetCookies() ?? new CookieParam[0]);
    }

    private static async IAsyncEnumerable<OrderProductHistorySyncResult> ListOrderProductsAsync(IPage page, DateOnly startDate, int pageNum = 1)
    {
        var url = $"{_historyUrl}?currentPage={pageNum}&pageSize=15&orderTimeBegin={startDate.ToString("yyyy-MM-dd")}";

        var response = await page.GoToAsync(url, waitUntil: WaitUntilNavigation.DOMContentLoaded);

        if (response.Status != System.Net.HttpStatusCode.OK)
        {
            yield return new OrderProductHistorySyncResult(SyncStatus.Error, $"查询失败: {response.Status}");
        }

        if (string.IsNullOrWhiteSpace(await response.TextAsync()))
        {
            yield return new OrderProductHistorySyncResult(SyncStatus.Error, "查询失败");
        }

#if DEBUG
        var text = await response.TextAsync();
#endif

        var json = await response.JsonAsync<ApiResult<HistoryListResult>>(_jsonSerializerOptions);

        if (json.Code != 200 || json.Result == null)
        {
            yield return new OrderProductHistorySyncResult(SyncStatus.Error, "查询失败");
        }

        if (json.Result?.HisOrderList != null)
        {
            foreach (var item in json.Result.HisOrderList)
            {
                var product = new OrderProductItemSyncModel()
                {
                    BrandName = item.BrandName,
                    Category = item.CatalogName,
                    TotalCount = item.FinalNumber,
                    OrderNumber = item.OrderCode,
                    Pack = item.EncapStandard,
                    ProductCode = item.ProductCode,
                    ProductId = item.ProductId,
                    ProductModel = item.ProductModel,
                    ProductName = item.ProductName,
                    // OrderTime = item.OrderTime,
                    Price = item.ProductPrice,
                    TotalPrice = item.ProductTotalMoney,
                    StockUnitName = item.StockUnitName,
                    OrderId = item.Uuid,
                };

                yield return new OrderProductHistorySyncResult(product, $"获取成功: 订单{item.OrderCode}，编号{item.ProductCode}，数量{item.FinalNumber}");
            }
        }

        if (json.Result?.Page?.TotalPage > pageNum)
        {
            await foreach (var item in ListOrderProductsAsync(page, startDate, pageNum + 1))
            {
                yield return item;
            }
        }
    }

    private async IAsyncEnumerable<OrderInfoSyncResult> ListOrdersAsync(IPage page, DateOnly startDate, int pageNum = 1)
    {
        var url = $"{_orderListUrl}?currentPage={pageNum}&pageSize=10&sd={startDate.ToString("yyyy-MM-dd")}+00:00:00";

        var response = await page.GoToAsync(url, waitUntil: WaitUntilNavigation.DOMContentLoaded);

        if (response.Status != System.Net.HttpStatusCode.OK)
        {
            yield return new OrderInfoSyncResult(SyncStatus.Error, $"查询失败: {response.Status}");
        }

        if (string.IsNullOrWhiteSpace(await response.TextAsync()))
        {
            yield return new OrderInfoSyncResult(SyncStatus.Error, "查询失败");
        }

#if DEBUG
        var text = await response.TextAsync();
#endif
        var json = await response.JsonAsync();
        var result = json.RootElement.Deserialize<ApiResult>(_jsonSerializerOptions);
        if (result?.Code != 200)
        {
            yield return new OrderInfoSyncResult(SyncStatus.Error, "查询失败：" + result!.Code);
        }

        var pager = json.RootElement.GetProperty("result").GetProperty("page").Deserialize<ApiPageInfoResult>(_jsonSerializerOptions)!;
        var orderList = json.RootElement.GetProperty("result").GetProperty("customerOrderList").Deserialize<OrderListItem[]>(_jsonSerializerOptions)!;

        foreach (var item in orderList)
        {
            var orderInfo = await GetOrderAsync(page, item.Uuid, true);

            yield return new OrderInfoSyncResult(orderInfo, $"获取成功: 订单{orderInfo.OrderNo}");
        }

        // 
        if (pager!.TotalPage > pageNum)
        {
            await foreach (var item in ListOrdersAsync(page, startDate, pageNum + 1))
            {
                yield return item;
            }
        }
    }

    public class ApiResult
    {
        public int Code { get; set; }
        public bool Ok { get; set; }
    }

    public class ApiResult<T>
    {
        public int Code { get; set; }
        public T? Result { get; set; }
    }

    //public class ApiPagerResult<T>
    //{
    //    public T[] DataList { get; set; } = null!;
    //}

    public class HistoryListResult
    {
        public List<ProductItem>? HisOrderList { get; set; }
        public ApiPageInfoResult? Page { get; set; }
    }

    public class ApiPageInfoResult
    {
        public int CurrPage { get; set; }
        public int PageRow { get; set; }
        public int TotalPage { get; set; }
        public int TotalRow { get; set; }
    }

    public class ProductItem
    {
        public string Uuid { get; set; } = null!;
        public string OrderCode { get; set; } = null!;
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string CatalogCode { get; set; } = null!;
        public string CatalogName { get; set; } = null!;
        public string BrandName { get; set; } = null!;
        public string EncapStandard { get; set; } = null!;
        public string ProductModel { get; set; } = null!;
        public int FinalNumber { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal ProductTotalMoney { get; set; }
        // public DateTime OrderTime { get; set; }
        public string? StockUnitName { get; set; }
    }

    public class OrderDetails
    {
        public OrderDetailsVO? OrderVO { get; set; }
    }

    public class OrderDetailsVO
    {
        public string OrderCode { get; set; } = null!;
        public DateTime OrderTime { get; set; }
        public decimal UseCouponMoney { get; set; }
        public decimal ProductTotalMoney { get; set; }
        public decimal OrderTotalMoney { get; set; }

        public string Uuid { get; set; } = null!;
    }

    public class OrderListItem
    {
        public string Uuid { get; set; } = null!;
    }

    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.MinValue;

            return DateTime.Parse(value);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
