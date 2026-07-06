using Entities;
using ServiceContracts.DTOs.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ServiceContracts.DTOs
{
    public class PersonAddRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Date of Birth is required")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]

        public string? phone { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public GenderOptions? Gender { get; set; }

        [StringLength(200, ErrorMessage = "Address is too long")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Please select a country")]
        public Guid? CountryId { get; set; }

        public bool? NewsLetter { get; set; }

        public string? ContextMemory { get; set; }

        public string? ProfileImagePath { get; set; }

        [StringLength(500, ErrorMessage = "Source context is too long")]
        public string? Origin { get; set; }

        public string? LinkedInProfile { get; set; }

        public string? OtherInformation { get; set; }

        public List<string>? Organizations { get; set; }

        public List<string>? CurrentRoles { get; set; }

        public List<SocialMediaAccountAddRequest>? SocialMediaAccounts { get; set; }

        public List<string>? ConnectionChannels { get; set; }

        public List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>? SystemStatusTags { get; set; }

        public List<string>? UserDefinedTags { get; set; }

        public Person ToPerson()
        {
            return new Person
            {
                Name = this.Name,
                DateOfBirth = this.DateOfBirth,
                email = this.email,
                phone = this.phone,
                Gender = this.Gender.ToString(),
                Address = this.Address,
                CountryId = (Guid)this.CountryId,
                NewsLetter = this.NewsLetter,
                ContextMemory = this.ContextMemory,
                ProfileImagePath = this.ProfileImagePath,
                Origin = this.Origin,
                LinkedInProfile = this.LinkedInProfile,
                OtherInformation = this.OtherInformation
            };
        }
    }

    public class SocialMediaAccountAddRequest
    {
        public string? Platform { get; set; }

        [Required(ErrorMessage = "URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string Url { get; set; }
    }
}