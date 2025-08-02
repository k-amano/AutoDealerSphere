using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoDealerSphere.Shared.Models
{
    public class Vehicle
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        [StringLength(50)]
        public string? LicensePlateLocation { get; set; }

        [StringLength(50)]
        public string? LicensePlateClassification { get; set; }

        [StringLength(10)]
        public string? LicensePlateHiragana { get; set; }

        [StringLength(20)]
        public string? LicensePlateNumber { get; set; }

        [StringLength(20)]
        public string? KeyNumber { get; set; }

        [StringLength(50)]
        public string? ChassisNumber { get; set; }

        [StringLength(20)]
        public string? TypeCertificationNumber { get; set; }

        [StringLength(20)]
        public string? CategoryNumber { get; set; }

        [StringLength(50)]
        public string? VehicleName { get; set; }

        [StringLength(50)]
        public string? VehicleModel { get; set; }

        public decimal? Mileage { get; set; }

        public DateTime? FirstRegistrationDate { get; set; }

        [StringLength(50)]
        public string? Purpose { get; set; }

        [StringLength(20)]
        public string? PersonalBusinessUse { get; set; }

        [StringLength(20)]
        public string? BodyShape { get; set; }

        public int? SeatingCapacity { get; set; }

        public int? MaxLoadCapacity { get; set; }

        public int? VehicleWeight { get; set; }

        public int? VehicleTotalWeight { get; set; }

        public int? VehicleLength { get; set; }

        public int? VehicleWidth { get; set; }

        public int? VehicleHeight { get; set; }

        public int? FrontOverhang { get; set; }

        public int? RearOverhang { get; set; }

        [StringLength(50)]
        public string? ModelCode { get; set; }

        [StringLength(50)]
        public string? EngineModel { get; set; }

        public decimal? Displacement { get; set; }

        [StringLength(20)]
        public string? FuelType { get; set; }

        public DateTime? InspectionExpiryDate { get; set; }

        public DateTime? NextInspectionDate { get; set; }

        [StringLength(50)]
        public string? InspectionCertificateNumber { get; set; }

        [StringLength(100)]
        public string? UserNameOrCompany { get; set; }

        [StringLength(200)]
        public string? UserAddress { get; set; }

        [StringLength(50)]
        public string? BaseLocation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // 請求書システム用に追加
        public int? VehicleCategoryId { get; set; }

        [ForeignKey("VehicleCategoryId")]
        public VehicleCategory? VehicleCategory { get; set; }
    }
}