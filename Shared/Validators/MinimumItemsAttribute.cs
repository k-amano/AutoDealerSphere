using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace AutoDealerSphere.Shared.Validators
{
    public class MinimumItemsAttribute : ValidationAttribute
    {
        private readonly int _minimumItems;

        public MinimumItemsAttribute(int minimumItems)
        {
            _minimumItems = minimumItems;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IList list)
            {
                if (list.Count < _minimumItems)
                {
                    return new ValidationResult(ErrorMessage ?? $"最低{_minimumItems}件の明細が必要です。");
                }
            }
            return ValidationResult.Success;
        }
    }
}