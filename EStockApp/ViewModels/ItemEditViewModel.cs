using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EStockApp.ViewModels;

public partial class ItemEditViewModel : ObservableValidator
{
    [ObservableProperty]
    private bool _isAdd;


    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(minimum: 0, maximum: 9999999)]
    private decimal _unitPrice;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(minimum: 0, maximum: 9999999)]
    private decimal _totalPrice;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(50)]
    private string? _orderNo;


    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [Range(minimum: 0, maximum: 9999999)]
    private int _productId;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [MaxLength(50)]
    private string? _category;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [MaxLength(50)]
    private string? _productCode;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [MaxLength(500)]
    private string? _productName;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [MaxLength(50)]
    private string? _productModel;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(50)]
    private string? _brandName;

    [ObservableProperty]
    [Required]
    [NotifyDataErrorInfo]
    [MaxLength(50)]
    private string? _pack;

    [ObservableProperty]
    [Required]
    [NotifyDataErrorInfo]
    [MaxLength(16)]
    private string? _stockUnitName;

    [ObservableProperty]
    [Required]
    [Range(minimum: 0, maximum: 9999999)]
    private int _totalCount;

    [ObservableProperty]
    [Required]
    [Range(minimum: 0, maximum: 9999999)]
    private int _stockCount;

    public void Validate() => ValidateAllProperties();
}
