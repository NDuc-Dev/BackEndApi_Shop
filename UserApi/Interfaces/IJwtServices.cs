using Shared.Models;

namespace UserApi.Interfaces
{
    public interface IJwtServices
    {
        public string CreateJwt(User user, IList<string> userRole);
    }
}