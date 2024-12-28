using System.ComponentModel.DataAnnotations;

namespace UserApi.DTOs.User
{
    #nullable disable
    public class LoginDto
    {
        [Required (ErrorMessage = "Email is required")]
        public string UserName { get; set; }
        [Required  (ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }
}
