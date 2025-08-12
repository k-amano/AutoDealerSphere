using System.ComponentModel.DataAnnotations;

namespace AutoDealerSphere.Shared.Models
{
    public class IssuerInfo
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "郵便番号")]
        [Required(ErrorMessage = "郵便番号は必須です")]
        [StringLength(8, ErrorMessage = "郵便番号は8文字以内で入力してください")]
        public string PostalCode { get; set; }

        [Display(Name = "住所")]
        [Required(ErrorMessage = "住所は必須です")]
        [StringLength(200, ErrorMessage = "住所は200文字以内で入力してください")]
        public string Address { get; set; }

        [Display(Name = "社名")]
        [Required(ErrorMessage = "社名は必須です")]
        [StringLength(100, ErrorMessage = "社名は100文字以内で入力してください")]
        public string CompanyName { get; set; }

        [Display(Name = "役職")]
        [StringLength(50, ErrorMessage = "役職は50文字以内で入力してください")]
        public string Position { get; set; }

        [Display(Name = "氏名")]
        [Required(ErrorMessage = "氏名は必須です")]
        [StringLength(50, ErrorMessage = "氏名は50文字以内で入力してください")]
        public string Name { get; set; }

        [Display(Name = "電話番号")]
        [Required(ErrorMessage = "電話番号は必須です")]
        [StringLength(20, ErrorMessage = "電話番号は20文字以内で入力してください")]
        public string PhoneNumber { get; set; }

        [Display(Name = "FAX番号")]
        [StringLength(20, ErrorMessage = "FAX番号は20文字以内で入力してください")]
        public string FaxNumber { get; set; }

        // 口座1
        [Display(Name = "口座1銀行名")]
        [Required(ErrorMessage = "口座1銀行名は必須です")]
        [StringLength(50, ErrorMessage = "銀行名は50文字以内で入力してください")]
        public string Bank1Name { get; set; }

        [Display(Name = "口座1支店名")]
        [Required(ErrorMessage = "口座1支店名は必須です")]
        [StringLength(50, ErrorMessage = "支店名は50文字以内で入力してください")]
        public string Bank1BranchName { get; set; }

        [Display(Name = "口座1種別")]
        [Required(ErrorMessage = "口座1種別は必須です")]
        [StringLength(20, ErrorMessage = "口座種別は20文字以内で入力してください")]
        public string Bank1AccountType { get; set; }

        [Display(Name = "口座1番号")]
        [Required(ErrorMessage = "口座1番号は必須です")]
        [StringLength(20, ErrorMessage = "口座番号は20文字以内で入力してください")]
        public string Bank1AccountNumber { get; set; }

        [Display(Name = "口座1名義人")]
        [Required(ErrorMessage = "口座1名義人は必須です")]
        [StringLength(50, ErrorMessage = "名義人は50文字以内で入力してください")]
        public string Bank1AccountHolder { get; set; }

        // 口座2
        [Display(Name = "口座2銀行名")]
        [StringLength(50, ErrorMessage = "銀行名は50文字以内で入力してください")]
        public string Bank2Name { get; set; }

        [Display(Name = "口座2支店名")]
        [StringLength(50, ErrorMessage = "支店名は50文字以内で入力してください")]
        public string Bank2BranchName { get; set; }

        [Display(Name = "口座2種別")]
        [StringLength(20, ErrorMessage = "口座種別は20文字以内で入力してください")]
        public string Bank2AccountType { get; set; }

        [Display(Name = "口座2番号")]
        [StringLength(20, ErrorMessage = "口座番号は20文字以内で入力してください")]
        public string Bank2AccountNumber { get; set; }

        [Display(Name = "口座2名義人")]
        [StringLength(50, ErrorMessage = "名義人は50文字以内で入力してください")]
        public string Bank2AccountHolder { get; set; }
    }
}