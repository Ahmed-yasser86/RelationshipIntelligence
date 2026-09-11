using FluentAssertions;
using ServiceContracts.DTOs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace CRUDTests.ControllersTest
{
    public class RegisterDTOValidationTests
    {
        private static IList<ValidationResult> Validate(RegisterDTO dto)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            return results;
        }

        private static RegisterDTO Valid() => new()
        {
            PersonName = "New User",
            Email = "new@example.com",
            Phone = "123456789",
            Password = "Secret123!",
            ConfirmPassword = "Secret123!"
        };

        [Fact]
        public void Validate_ProperDetails_Passes()
        {
            Validate(Valid()).Should().BeEmpty();
        }

        [Fact]
        public void Validate_InvalidEmail_Fails()
        {
            var dto = Valid();
            dto.Email = "not-an-email";

            Validate(dto).Should().NotBeEmpty();
        }

        [Fact]
        public void Validate_NonNumericPhone_Fails()
        {
            var dto = Valid();
            dto.Phone = "12ab34";

            Validate(dto).Should().NotBeEmpty();
        }

        [Fact]
        public void Validate_MissingName_Fails()
        {
            var dto = Valid();
            dto.PersonName = string.Empty;

            Validate(dto).Should().NotBeEmpty();
        }
    }
}
