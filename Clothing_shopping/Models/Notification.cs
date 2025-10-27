using System;
using System.Collections.Generic;

namespace Clothing_shopping.Models;

public partial class Notification
{
    public long NotificationId { get; set; }

    public int NewsId { get; set; }

    public Guid? ReceiverId { get; set; }

    public int? OrderId { get; set; }

    public bool? IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual News News { get; set; } = null!;

    public virtual Order? Order { get; set; }

    public virtual User? Receiver { get; set; }
}
