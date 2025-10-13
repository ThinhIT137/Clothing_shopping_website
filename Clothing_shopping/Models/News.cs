using System;
using System.Collections.Generic;

namespace Clothing_shopping.models;

public partial class News
{
    public int NewsId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? ShortDesc { get; set; }

    public string? Content { get; set; }

    public Guid? AuthorId { get; set; }

    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual User? Author { get; set; }
}
