using System;
using System.Collections.Generic;

namespace Clothing_shopping.models;

public partial class Order
{
    public int OrderId { get; set; }

    public Guid UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public string? ShippingAddress { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string OrderStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User User { get; set; } = null!;
}
