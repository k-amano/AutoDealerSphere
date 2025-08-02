using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoDealerSphere.Shared.Models
{
    public class StatutoryFee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "車両区分は必須です。")]
        public int VehicleCategoryId { get; set; }

        [ForeignKey("VehicleCategoryId")]
        public VehicleCategory? VehicleCategory { get; set; }

        [Required(ErrorMessage = "費用種別は必須です。")]
        [StringLength(50, ErrorMessage = "費用種別は50文字以内で入力してください。")]
        public string FeeType { get; set; } = string.Empty;

        [Required(ErrorMessage = "金額は必須です。")]
        [Range(0, 99999999.99, ErrorMessage = "金額は0～99,999,999.99の範囲で入力してください。")]
        public decimal Amount { get; set; }

        public bool IsTaxable { get; set; } = false;

        [Required(ErrorMessage = "適用開始日は必須です。")]
        public DateTime EffectiveFrom { get; set; } = DateTime.Today;

        public DateTime? EffectiveTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}