using Shared.Models;

namespace AdminApi.Interfaces
{
    public interface IJwtServices
    {
        public string CreateJwt(User user, IList<string> userRole);
    }
}