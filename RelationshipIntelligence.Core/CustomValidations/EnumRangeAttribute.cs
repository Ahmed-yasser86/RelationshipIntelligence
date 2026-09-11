using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.CustomValidations
{
    public class EnumRangeAttribute : ValidationAttribute
    {
        private readonly Type _enumType;

        public EnumRangeAttribute(Type enumType)
        {
            if (!enumType.IsEnum)
                throw new ArgumentException("Type must be an enum.");

            _enumType = enumType;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (Enum.IsDefined(_enumType, value))
                return ValidationResult.Success;

            return new ValidationResult(
                $"{validationContext.DisplayName} contains an invalid value.");
        }
    }
}
