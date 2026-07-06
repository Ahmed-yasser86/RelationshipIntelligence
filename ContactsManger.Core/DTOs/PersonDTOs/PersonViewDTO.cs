using Entities;
using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.DTOs.PersonDTOs
{
    public class PersonViewDTO
    {

        public Guid PersonId { get; set; }
        public string Name { get; set; }

        public string? email { get; set; }

        public string phone { get; set; }

        public string CountryName { get; set; }

        public List<CircleResponse> Organizations { get; set; } = new List<CircleResponse>();
   
        public List<ContactItemRoleResponse> CurrentRoles { get; set; } = new List<ContactItemRoleResponse>();

        /// <summary>
        /// this indecate the main connection channels of the person where i mainlly connect with him /her like email, phone, social media accounts, etc.
        /// </summary>
        public List<ConnectionChannelResponse> ConnectionChannels { get; set; } = new List<ConnectionChannelResponse>();

        public List<SystemStatusTagResponse> SystemStatusTags { get; set; } = new List<SystemStatusTagResponse>();

        public List<UserDefinedTagsResponse> UserDefinedTags { get; set; } = new List<UserDefinedTagsResponse>();
    
        public List<InteractionResponse> Interactions { get; set; } = new List<InteractionResponse>();


    }


    public static class PersonViewDTOExtension
    {
        public static PersonViewDTO ConvertToPersonViewDTO(this Person person)
        {
            if (person == null) return null;

            return new PersonViewDTO
            {
                PersonId = person.PersonId,
                Name = person.Name,
                email = person.email,
                phone = person.phone,
                CountryName = person.Country?.CountryName,
                Organizations = person.Circles?.Select(c => c.ConvertToDto()).ToList() ?? new List<CircleResponse>(),
                CurrentRoles = person.ContactItemRoles?.Select(r => r.ConvertToDto()).ToList() ?? new List<ContactItemRoleResponse>(),
                ConnectionChannels = person.ConnectionChannels?.Select(c => c.ConvertToDto()).ToList() ?? new List<ConnectionChannelResponse>(),
                SystemStatusTags = person.SystemStatusTags?.Select(s => s.ConvertToDto()).ToList() ?? new List<SystemStatusTagResponse>(),
                UserDefinedTags = person.UserDefinedTags?.Select(t => t.ConvertToDto()).ToList() ?? new List<UserDefinedTagsResponse>(),
                Interactions = person.Interactions?.Select(i => i.ConvertToDto()).ToList() ?? new List<InteractionResponse>()
            };
        }

    }
  }
