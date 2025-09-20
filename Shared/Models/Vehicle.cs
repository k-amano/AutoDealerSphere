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

        public int? SeatingCapacity { get; set; }  // 削除予定（PassengerCapacityと重複）

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

        // 新規追加フィールド
        public decimal? RatedOutput { get; set; }  // 定格出力（kW）

        [StringLength(20)]
        public string? BodyColor { get; set; }  // 車体の色

        public int? PassengerCapacity { get; set; }  // 乗車定員

        public int? FrontAxleWeight { get; set; }  // 前前軸重（kg）

        public int? RearAxleWeight { get; set; }  // 後後軸重（kg）

        public DateTime? InspectionExpiryDate { get; set; }

        [StringLength(50)]
        public string? InspectionCertificateNumber { get; set; }

        // 車検証JSON対応の新規フィールド
        public DateTime? RegistrationDate { get; set; }  // 登録年月日

        public DateTime? ManufactureDate { get; set; }  // 製造年月

        [StringLength(20)]
        public string? InspectionType { get; set; }  // 検査種別

        public DateTime? InspectionDate { get; set; }  // 検査実施日

        public DateTime? MileageUpdateDate { get; set; }  // 走行距離更新日

        [StringLength(100)]
        public string? UserNameOrCompany { get; set; }

        [StringLength(200)]
        public string? UserAddress { get; set; }

        [StringLength(10)]
        public string? UserPostalCode { get; set; }  // 使用者郵便番号

        // 所有者情報（新規追加）
        [StringLength(100)]
        public string? OwnerNameOrCompany { get; set; }  // 所有者の氏名又は名称

        [StringLength(200)]
        public string? OwnerAddress { get; set; }  // 所有者の住所

        [StringLength(10)]
        public string? OwnerPostalCode { get; set; }  // 所有者郵便番号

        [StringLength(50)]
        public string? BaseLocation { get; set; }

        // 電子車検証情報（新規追加）
        public string? QRCodeData { get; set; }  // QRコード情報（JSON）

        [StringLength(50)]
        public string? ICTagId { get; set; }  // ICタグID

        public bool ElectronicCertificateFlag { get; set; }  // 電子車検証フラグ

        public DateTime? IssueDate { get; set; }  // 発行年月日

        [StringLength(100)]
        public string? IssueOffice { get; set; }  // 発行事務所

        [StringLength(10)]
        public string? CertificateVersion { get; set; }  // 車検証バージョン

        // システム管理用
        [StringLength(20)]
        public string? ImportSource { get; set; }  // インポート元（JSON/CSV/Manual）

        public DateTime? ImportDate { get; set; }  // インポート日時

        public string? OriginalData { get; set; }  // 元データ（JSON保存用）

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // 請求書システム用に追加
        public int? VehicleCategoryId { get; set; }

        [ForeignKey("VehicleCategoryId")]
        public VehicleCategory? VehicleCategory { get; set; }

        // ハイブリッド方式: 計算プロパティとして統合された車両登録番号
        [NotMapped]
        public string RegistrationNumber
        {
            get
            {
                if (string.IsNullOrEmpty(LicensePlateLocation) &&
                    string.IsNullOrEmpty(LicensePlateClassification) &&
                    string.IsNullOrEmpty(LicensePlateHiragana) &&
                    string.IsNullOrEmpty(LicensePlateNumber))
                {
                    return string.Empty;
                }

                return $"{LicensePlateLocation ?? ""}{LicensePlateClassification ?? ""}{LicensePlateHiragana ?? ""}{LicensePlateNumber ?? ""}";
            }
        }
    }
}