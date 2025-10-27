using System;
using System.Collections.Generic;

namespace Clothing_shopping.Models;

public partial class FavoriteItem
{
    public int FavoriteItemsId { get; set; }

    public Guid UserId { get; set; }

    public int ProductVariantId { get; set; }

    public DateTime AddedAt { get; set; }

    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
