using ToyStore.Domain.Entities;

namespace ToyStore.Models;

public class CheckoutPageViewModel
{
    public bool IsLoggedIn { get; set; }
    public Customer? Customer { get; set; }
    public GuestCheckoutInfo Guest { get; set; } = new();
    public string PaymentMethod { get; set; } = "COD";
}
