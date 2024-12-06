
namespace Shared.Models
{
    #nullable disable
    public class PaginateDataView<T>
    {
        public IEnumerable<T> ListData { get; set; }
        public int totalCount { get; set; }
    }
}