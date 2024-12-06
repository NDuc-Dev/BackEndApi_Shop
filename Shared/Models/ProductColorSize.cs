using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Models
{
    public class ProductColorSize
    {
        #nullable disable
        [Key]
        public int ProductColorSizeId { get; set; }
        public int ProductColorId { get; set; }
        [JsonIgnore]
        public ProductColor ProductColor { get; set; }
        public int SizeId { get; set; }
        [JsonIgnore]
        public Size Size { get; set; }
        public int Quantity { get; set; }
        [JsonIgnore]
        public ICollection<OrderDetails> Details { get; set; }

    }
}
