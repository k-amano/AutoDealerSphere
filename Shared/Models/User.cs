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
	}
}
