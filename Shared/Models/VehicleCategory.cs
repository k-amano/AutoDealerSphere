using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoDealerSphere.Shared.Models
{
    public class VehicleCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "車両区分名は必須です。")]
        [StringLength(50, ErrorMessage = "車両区分名は50文字以内で入力してください。")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "説明は200文字以内で入力してください。")]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }
    }
}