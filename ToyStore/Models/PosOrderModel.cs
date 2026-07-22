namespace ToyStore.Models;

public class PosOrderModel
{
    public int? CustomerId { get; set; }

    public string PaymentMethod { get; set; } = "Tiền mặt";

    public List<PosOrderItemModel> Items { get; set; } = new();
}

public class PosOrderItemModel
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
