using Microsoft.AspNetCore.Mvc;

namespace Clothing_shopping.Areas.Admin.Models
{
    [Area("Admin")]
    public class ProductVariantInput
    {
        public string Size { get; set; }
        public string Color { get; set; }
        public decimal Price { get; set; }
        public decimal SalePrice { get; set; }
        public int Stock { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }
    }
}
