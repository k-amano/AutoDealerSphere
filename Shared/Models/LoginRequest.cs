using System.ComponentModel.DataAnnotations;

namespace AutoDealerSphere.Shared.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "メールアドレスを入力してください。")]
        public string Email { get; set; } = "";
        
        [Required(ErrorMessage = "パスワードを入力してください。")]
        public string Password { get; set; } = "";
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
        public string? Token { get; set; }
    }
}