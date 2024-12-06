using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Shared.Models
{
    public class ProductColor
    {
        #nullable disable
        [Key]
        public int ProductColorId { get; set; }
        public int ProductId { get; set; }
        [JsonIgnore]
        public Product Product { get; set; }
        public int ColorId { get; set; }
        [JsonIgnore]
        public Color Color { get; set; }
        [Column(TypeName = "decimal(9,0)")]
        public decimal Price { get; set; }
        public string ImagePath { get; set; }
        public ICollection<ProductColorSize> ProductColorSizes { get; set; }
    }
}
