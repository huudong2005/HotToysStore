using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Status { get; set; }

    public string? PaymentMethod { get; set; }

    public string? DeliveryMethod { get; set; }

    [Column("ShippingCode")]
    public string? ShippingCode { get; set; }

    [Column("ShippingFee")]
    public decimal ShippingFee { get; set; }

    [Column("ShippingAddress")]
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Tên discount strategy được áp dụng (VipDiscount, SeasonalDiscount, NoDiscount)
    /// </summary>
    public string? DiscountStrategyName { get; set; }

    /// <summary>
    /// Giá trị khuyến mãi (DiscountValue)
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Tổng tiền trước khi giảm giá: ∑(Quantity × UnitPrice)
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Số tiền giảm theo ưu đãi hạng thẻ thành viên (xếp chồng với khuyến mãi/voucher).
    /// </summary>
    public decimal MembershipDiscountValue { get; set; } = 0;

    /// <summary>
    /// Loại đơn: Online hoặc POS (bán tại quầy).
    /// </summary>
    [Column("OrderType")]
    public string? OrderType { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
