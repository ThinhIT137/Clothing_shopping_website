namespace Clothing_shopping.Areas.Admin.Models
{
    public class DistributeViewModel
    {
        public int VoucherId { get; set; }
        public List<Guid> UserIds { get; set; } // UserId trong DB của bạn là Guid
    }
}
