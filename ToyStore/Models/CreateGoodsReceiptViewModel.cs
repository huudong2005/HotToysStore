using System.ComponentModel.DataAnnotations;

namespace ToyStore.Models;

public class CreateGoodsReceiptViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn ngày nhập kho")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày nhập kho")]
    public DateTime ImportDate { get; set; } = DateTime.Today;

    [Display(Name = "Ghi chú")]
    [StringLength(500)]
    public string? Note { get; set; }

    public List<GoodsReceiptLineViewModel> Details { get; set; } = new();
}

public class GoodsReceiptLineViewModel
{
    [Display(Name = "Sản phẩm")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá nhập phải lớn hơn 0")]
    [Display(Name = "Giá nhập")]
    public decimal ImportPrice { get; set; }
}
