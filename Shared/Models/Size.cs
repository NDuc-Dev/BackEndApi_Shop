using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Models
{
    public class Size
    {
        #nullable disable
        [Key]
        public int SizeId { get; set; }
        [Required(ErrorMessage = "Size value is required")]
        public int SizeValue { get; set; }
        public string CreateByUserId { get; set; }
        [JsonIgnore]
        public User CreateBy { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        [JsonIgnore]
        public ICollection<ProductColorSize> ProductColorSize { get; set; }
    }
}
