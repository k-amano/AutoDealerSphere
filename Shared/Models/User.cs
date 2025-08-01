using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoDealerSphere.Shared.Models
{
	public class User
	{
		[Key]
		[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		[Required(ErrorMessage = "名前を入力してください。")]
		[StringLength(40, ErrorMessage = "名前は40文字までです。")]
		public string Name { get; set; } = "";
		[Required(ErrorMessage = "メールアドレスを入力してください。")]
		[StringLength(50, ErrorMessage = "メールアドレスは50文字までです。")]
		public string Email { get; set; } = "";
		[Required(ErrorMessage = "パスワードを入力してください。")]
		[StringLength(100, ErrorMessage = "パスワードは100文字までです。")]
		public string Password { get; set; } = "";
		[Required(ErrorMessage = "権限を選択してください。")]
		[Range(1, 2, ErrorMessage = "権限は1（一般）または2（管理者）を選択してください。")]
		public int Role { get; set; } = 1;
	}
}
