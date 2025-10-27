using System;
using System.Collections.Generic;

namespace Clothing_shopping.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int? ProductVariantId { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal? TotalMoney { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ProductVariant? ProductVariant { get; set; }
}
