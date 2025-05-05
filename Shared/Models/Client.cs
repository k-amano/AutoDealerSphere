using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoDealerSphere.Shared.Models
{
    public class Client
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "名前を入力してください。")]
        [StringLength(40, ErrorMessage = "名前は40文字までです。")]
        public string Name { get; set; } = "";
        
        [StringLength(40, ErrorMessage = "カナは40文字までです。")]
        public string? Kana { get; set; }
        
        [Required(ErrorMessage = "メールアドレスを入力してください。")]
        [StringLength(50, ErrorMessage = "メールアドレスは50文字までです。")]
        public string Email { get; set; } = "";
        
        [Required(ErrorMessage = "郵便番号を入力してください。")]
        [StringLength(8, ErrorMessage = "郵便番号は8文字までです。")]
        public string Zip { get; set; } = "";
        
        [Required(ErrorMessage = "都道府県を選択してください。")]
        public int Prefecture { get; set; }
        
        [Required(ErrorMessage = "住所を入力してください。")]
        [StringLength(100, ErrorMessage = "住所は100文字までです。")]
        public string Address { get; set; } = "";
        
        [StringLength(100, ErrorMessage = "建物名は100文字までです。")]
        public string? Building { get; set; }
        
        [StringLength(20, ErrorMessage = "電話番号は20文字までです。")]
        public string? Phone { get; set; }
    }
}
