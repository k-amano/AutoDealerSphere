using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoDealerSphere.Shared.Models
{
    public class EmailSettings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "SMTPホストを入力してください。")]
        [StringLength(100, ErrorMessage = "SMTPホストは100文字までです。")]
        public string SmtpHost { get; set; } = "";

        [Required(ErrorMessage = "SMTPポートを入力してください。")]
        [Range(1, 65535, ErrorMessage = "SMTPポートは1～65535の範囲で入力してください。")]
        public int SmtpPort { get; set; } = 587;

        public bool EnableSsl { get; set; } = true;

        [Required(ErrorMessage = "送信者メールアドレスを入力してください。")]
        [StringLength(100, ErrorMessage = "送信者メールアドレスは100文字までです。")]
        [EmailAddress(ErrorMessage = "正しいメールアドレス形式で入力してください。")]
        public string SenderEmail { get; set; } = "";

        [Required(ErrorMessage = "送信者名を入力してください。")]
        [StringLength(100, ErrorMessage = "送信者名は100文字までです。")]
        public string SenderName { get; set; } = "";

        [Required(ErrorMessage = "SMTP認証ユーザー名を入力してください。")]
        [StringLength(100, ErrorMessage = "SMTP認証ユーザー名は100文字までです。")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "SMTP認証パスワードを入力してください。")]
        [StringLength(500, ErrorMessage = "SMTP認証パスワードは500文字までです。")]
        public string EncryptedPassword { get; set; } = "";

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
