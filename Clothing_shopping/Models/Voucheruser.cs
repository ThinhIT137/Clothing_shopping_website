using System;
using System.Collections.Generic;

namespace Clothing_shopping.Models;

public partial class Voucheruser
{
    public int VoucherId { get; set; }

    public Guid UserId { get; set; }

    public int UsedCount { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
