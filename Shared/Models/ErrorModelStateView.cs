

namespace Shared.Models
{
    #nullable disable
    public class ErrorModelStateView
    {
        public string Code { get; set; }
        public List<string> Errors { get; set; }
    }
}