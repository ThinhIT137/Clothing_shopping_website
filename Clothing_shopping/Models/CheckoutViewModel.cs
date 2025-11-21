using System.ComponentModel.DataAnnotations;

namespace Clothing_shopping.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        public string ReceiverName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string ShippingAddress { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string? Note { get; set; }

        // --- Dùng cho Mua từ giỏ hàng ---
        public List<int> SelectedCartItemIds { get; set; } = new List<int>();
        // --- Dùng cho Mua ngay (Direct Buy) ---
        public int? DirectProductVariantId { get; set; }
        public int? DirectQuantity { get; set; }
        public List<CartItem>? CartItemsDisplay { get; set; }

        // --- THÊM MỚI CHO VOUCHER ---
        public int? SelectedVoucherId { get; set; } // ID voucher khách chọn
        public List<Voucher>? AvailableVouchers { get; set; } // Danh sách voucher của khách
    }
}
