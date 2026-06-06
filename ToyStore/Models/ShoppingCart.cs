using System.Text.Json;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Domain.Strategies;

namespace ToyStore.Models
{
    public class ShoppingCart
    {
        public List<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
        
        /// <summary>
        /// Tên discount strategy được áp dụng (VipDiscount, SeasonalDiscount, NoDiscount)
        /// </summary>
        public string? DiscountStrategyName { get; set; }
        
        /// <summary>
        /// Tổng tiền trước khi giảm giá: ∑(Quantity × UnitPrice)
        /// </summary>
        public decimal Subtotal => Items.Sum(item => item.Total);
        
        /// <summary>
        /// Giá trị khuyến mãi (DiscountValue)
        /// </summary>
        public decimal DiscountValue
        {
            get
            {
                var strategy = DiscountStrategyFactory.CreateStrategy(DiscountStrategyName);
                return strategy.CalculateDiscount(Subtotal);
            }
        }
        
        /// <summary>
        /// Tổng tiền sau khi giảm giá: Total = ∑(Quantity × UnitPrice) – DiscountValue
        /// </summary>
        public decimal Total
        {
            get
            {
                var strategy = DiscountStrategyFactory.CreateStrategy(DiscountStrategyName);
                return strategy.CalculateTotal(Subtotal);
            }
        }
        
        public int ItemCount => Items.Sum(item => item.Quantity);
        
        public void AddItem(Product product, int quantity = 1)
        {
            var existingItem = Items.FirstOrDefault(item => item.ProductId == product.ProductId);
            
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new ShoppingCartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl
                });
            }
        }
        
        public void RemoveItem(int productId)
        {
            Items.RemoveAll(item => item.ProductId == productId);
        }
        
        public void UpdateQuantity(int productId, int quantity)
        {
            var item = Items.FirstOrDefault(item => item.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                    RemoveItem(productId);
                else
                    item.Quantity = quantity;
            }
        }
        
        public void Clear()
        {
            Items.Clear();
        }
        
        // Serialize to JSON for session storage
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
        
        // Deserialize from JSON
        public static ShoppingCart FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new ShoppingCart();
                
            try
            {
                return JsonSerializer.Deserialize<ShoppingCart>(json) ?? new ShoppingCart();
            }
            catch
            {
                return new ShoppingCart();
            }
        }
    }
    
    public class ShoppingCartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        
        public decimal Total => Price * Quantity;
    }
}
