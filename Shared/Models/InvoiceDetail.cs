using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoDealerSphere.Shared.Models
{
    public class InvoiceDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey("InvoiceId")]
        public Invoice? Invoice { get; set; }

        public int? PartId { get; set; }

        [ForeignKey("PartId")]
        public Part? Part { get; set; }

        [Required(ErrorMessage = "項目名は必須です。")]
        [StringLength(100, ErrorMessage = "項目名は100文字以内で入力してください。")]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "タイプは50文字以内で入力してください。")]
        public string? Type { get; set; }

        [StringLength(100, ErrorMessage = "修理方法は100文字以内で入力してください。")]
        public string? RepairMethod { get; set; }

        [Required(ErrorMessage = "数量は必須です。")]
        [Range(0.01, 9999.99, ErrorMessage = "数量は0.01～9999.99の範囲で入力してください。")]
        public decimal Quantity { get; set; } = 1;

        [Required(ErrorMessage = "単価は必須です。")]
        [Range(0, 99999999.99, ErrorMessage = "単価は0～99,999,999.99の範囲で入力してください。")]
        public decimal UnitPrice { get; set; }

        [Range(0, 99999999.99, ErrorMessage = "工賃は0～99,999,999.99の範囲で入力してください。")]
        public decimal LaborCost { get; set; }

        public bool IsTaxable { get; set; } = true;

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public decimal SubTotal => (UnitPrice * Quantity) + LaborCost;
    }
}