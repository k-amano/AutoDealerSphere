using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoDealerSphere.Shared.Models
{
    public class Part
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "部品名は必須です。")]
        [StringLength(100, ErrorMessage = "部品名は100文字以内で入力してください。")]
        public string PartName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "タイプは50文字以内で入力してください。")]
        public string? Type { get; set; }

        [Required(ErrorMessage = "単価は必須です。")]
        [Range(0, 99999999.99, ErrorMessage = "単価は0～99,999,999.99の範囲で入力してください。")]
        public decimal UnitPrice { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}