using System;
using System.Collections.Generic;

namespace Clothing_shopping.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string VoucherType { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public decimal DiscountValue { get; set; }

    public virtual ICollection<Voucheruser> Voucherusers { get; set; } = new List<Voucheruser>();
}
