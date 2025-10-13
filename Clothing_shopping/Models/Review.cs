using System;
using System.Collections.Generic;

namespace Clothing_shopping.models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int? ProductVariantId { get; set; }

    public Guid UserId { get; set; }

    public byte Rating { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ProductVariant? ProductVariant { get; set; }

    public virtual User User { get; set; } = null!;
}
