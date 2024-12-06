using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Models
{
    public class NameTag
    {
        #nullable disable
        [Key]
        public int NameTagId { get; set; }
        [Required(ErrorMessage = "Tag name is required")]
        public string Tag { get; set; }
        public string CreateByUserId { get; set; }
        [JsonIgnore]
        public User CreateBy { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        [JsonIgnore]
        public ICollection<ProductNameTag> ProductTags { get; set; }
    }
}
