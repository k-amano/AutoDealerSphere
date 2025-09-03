using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AutoDealerSphere.Shared.Validators;

namespace AutoDealerSphere.Shared.Models
{
    public class Invoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "請求書番号は必須です。")]
        [StringLength(20, ErrorMessage = "請求書番号は20文字以内で入力してください。")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Range(1, 99, ErrorMessage = "枝番は1～99の範囲で入力してください。")]
        public int Subnumber { get; set; } = 1;

        [Required(ErrorMessage = "顧客は必須です。")]
        [Range(1, int.MaxValue, ErrorMessage = "顧客を選択してください。")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        public int? VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public Vehicle? Vehicle { get; set; }

        [Required(ErrorMessage = "請求日は必須です。")]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "作業完了日は必須です。")]
        public DateTime WorkCompletedDate { get; set; } = DateTime.Today;

        public DateTime? NextInspectionDate { get; set; }

        public int? Mileage { get; set; }

        public decimal TaxableSubTotal { get; set; }

        public decimal NonTaxableSubTotal { get; set; }

        public decimal TaxRate { get; set; } = 10m;

        public decimal Tax { get; set; }

        public decimal Total { get; set; }

        [StringLength(500, ErrorMessage = "備考は500文字以内で入力してください。")]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}