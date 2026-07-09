using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceContracts.DTOs
{
    public class PersonRespones
    {
        public Guid PersonId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? email { get; set; }

        public string phone { get; set; }

        public string Gender { get; set; }

        public string? Address { get; set; }

        public Guid? CountryId { get; set; }

        public string CountryName { get; set; }

        public bool? NewsLetter { get; set; }

        public string? ContextMemory { get; set; }

        public string? ProfileImagePath { get; set; }

        public string? Origin { get; set; }

        public string? LinkedInProfile { get; set; }

        public string? OtherInformation { get; set; }

        public List<CircleResponse> Organizations { get; set; } = new List<CircleResponse>();

        public List<ContactItemRoleResponse> CurrentRoles { get; set; } = new List<ContactItemRoleResponse>();

        public List<SocialMediaAccountResponse> SocialMediaAccounts { get; set; } = new List<SocialMediaAccountResponse>();

        public List<ConnectionChannelResponse> ConnectionChannels { get; set; } = new List<ConnectionChannelResponse>();

        public List<SystemStatusTagResponse> SystemStatusTags { get; set; } = new List<SystemStatusTagResponse>();

        public List<UserDefinedTagsResponse> UserDefinedTags { get; set; } = new List<UserDefinedTagsResponse>();

        public List<NoteResponse> Notes { get; set; } = new List<NoteResponse>();

        public List<InteractionResponse> Interactions { get; set; } = new List<InteractionResponse>();

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            PersonRespones other = (PersonRespones)obj;

            return PersonId == other.PersonId &&
                   Name == other.Name &&
                   Age == other.Age &&
                   DateOfBirth == other.DateOfBirth &&
                   email == other.email &&
                   phone == other.phone &&
                   Gender == other.Gender &&
                   Address == other.Address &&
                   CountryId == other.CountryId &&
                   NewsLetter == other.NewsLetter &&
                   CountryName == other.CountryName;
        }

        public PersonUpdateRequest ToPersonUpdateRequest()
        {
            return new PersonUpdateRequest
            {
                PersonId = this.PersonId,
                Name = this.Name,
                DateOfBirth = this.DateOfBirth,
                email = this.email,
                phone = this.phone,
                Gender = (Enums.GenderOptions)Enum.Parse(typeof(Enums.GenderOptions), Gender, true),
                Address = this.Address,
                CountryId = (Guid)this.CountryId,
                NewsLetter = this.NewsLetter,
                ContextMemory = this.ContextMemory,
                ProfileImagePath = this.ProfileImagePath,
                Origin = this.Origin,
                LinkedInProfile = this.LinkedInProfile,
                OtherInformation = this.OtherInformation,
                Organizations = this.Organizations?.Select(o => o.Name).ToList(),
                CurrentRoles = this.CurrentRoles?.Select(r => r.Role).ToList(),
                ConnectionChannels = this.ConnectionChannels?.Select(c => c.ConnectionChannelName).ToList(),
                SystemStatusTags = this.SystemStatusTags?.Select(s => s.StatusTagId).ToList(),
                UserDefinedTags = this.UserDefinedTags?.Select(t => t.TagName).ToList()
            };
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public static class PersonResponseExtension
    {
        public static PersonRespones ConvertToPersonRespons(this Person person)
        {
            if (person == null) return null;

            return new PersonRespones
            {
                PersonId = person.PersonId,
                Name = person.Name,
                DateOfBirth = person.DateOfBirth,
                email = person.email,
                phone = person.phone,
                Gender = person.Gender,
                Address = person.Address,
                CountryId = person.CountryId,
                Age = person.DateOfBirth.HasValue ? DateTime.Now.Year - person.DateOfBirth.Value.Year : 0,
                CountryName = person.Country?.CountryName,
                NewsLetter = person.NewsLetter,
                ContextMemory = person.ContextMemory,
                ProfileImagePath = person.ProfileImagePath,
                Origin = person.Origin,
                LinkedInProfile = person.LinkedInProfile,
                OtherInformation = person.OtherInformation,
                Organizations = person.Circles?.Select(c => c.ConvertToDto()).ToList() ?? new List<CircleResponse>(),
                CurrentRoles = person.ContactItemRoles?.Select(r => r.ConvertToDto()).ToList() ?? new List<ContactItemRoleResponse>(),
                SocialMediaAccounts = person.OtherSocialMediaAccounts?.Select(s => s.ConvertToDto()).ToList() ?? new List<SocialMediaAccountResponse>(),
                ConnectionChannels = person.ConnectionChannels?.Select(c => c.ConvertToDto()).ToList() ?? new List<ConnectionChannelResponse>(),
                SystemStatusTags = person.SystemStatusTags?.Select(s => s.ConvertToDto()).ToList() ?? new List<SystemStatusTagResponse>(),
                UserDefinedTags = person.UserDefinedTags?.Select(t => t.ConvertToDto()).ToList() ?? new List<UserDefinedTagsResponse>(),
                Notes = person.Notes?.Select(n => n.ConvertToDto()).ToList() ?? new List<NoteResponse>(),
                Interactions = person.Interactions?.Select(i => i.ConvertToDto()).ToList() ?? new List<InteractionResponse>()
            };
        }
    }
}