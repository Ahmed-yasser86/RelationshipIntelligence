using ContactsManger.Core.Domain.Entities;
using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace Entities
{

    public class Person
    {
        [Key]
        public Guid PersonId { get; set; }
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// they are notes u write about 
        /// the person and you need to quickly remember them
        /// </summary>
        public string? ContextMemory { get; set; }

        public  string ?ProfileImagePath { get; set; }

        /// <summary>
        /// origin field is about how we met 
        /// </summary>
        public string? Origin { get; set; }

        public string ? LinkedInProfile { get; set; }
        public DateTime ?DateOfBirth { get; set; }
        [EmailAddress]  
        public string? email { get; set; }

        [Phone]
        public string phone { get; set; }

        [StringLength(10)]
        public string Gender { get; set; }

        public bool ?NewsLetter { get; set; }

        [StringLength(200)]
        public string ?Address { get; set; }
        public Guid CountryId { get; set; }

        [ForeignKey("CountryId")]
        public Country? Country { get; set; }

        public string ? OtherInformation { get; set; }
        public ICollection<SocialMediaAccount> OtherSocialMediaAccounts { get; set; } = new List<SocialMediaAccount>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();
        public  ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
        public ICollection<Circle> Circles { get; set; } = new List<Circle>();
        public ICollection<ConnectionChannel> ConnectionChannels { get; set; } = new List<ConnectionChannel>();
        public ICollection<SystemStatusTag> SystemStatusTags { get; set; } = new List<SystemStatusTag>();
        public ICollection<UserDefinedTags> UserDefinedTags { get; set; } = new List<UserDefinedTags>();
        public ICollection<ContactItemRole> ContactItemRoles { get; set; } = new List<ContactItemRole>();
    }
}
