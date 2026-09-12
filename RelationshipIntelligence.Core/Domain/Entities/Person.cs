using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.IdentityEntities;
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

        public string? ProfileImagePath { get; set; }

        /// <summary>
        /// origin field is about how we met 
        /// </summary>
        [StringLength(2000)]
        public string? Origin { get; set; }

        [StringLength(1000)]
        public string? LinkedInProfile { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [EmailAddress]
        public string? email { get; set; }

        [Phone]
        public string? phone { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        public bool? NewsLetter { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public Guid? CountryId { get; set; }

        [ForeignKey("CountryId")]
        public Country? Country { get; set; }

        public bool IsDeleted { get; set; } = false;

        public string? OtherInformation { get; set; }

        /// <summary>
        /// Every Person must belong to exactly one owning user -- confirmed:
        /// orphaned Persons (no owner) are not a valid state in this app.
        /// Required FK, not nullable. This is also what the AppDBContext global
        /// query filter scopes reads by, so every Person MUST have this set at
        /// creation time (PersonAdderService/PersonQuickAdderService are
        /// responsible for populating it from the current authenticated user --
        /// it must never come from client-submitted request data).
        /// </summary>
        [Required]
        public Guid ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }

        public ICollection<SocialMediaAccount> OtherSocialMediaAccounts { get; set; } = new List<SocialMediaAccount>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();
        public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
        public ICollection<Circle> Circles { get; set; } = new List<Circle>();
        public ICollection<ContactChannel> ContactChannels { get; set; } = new List<ContactChannel>();
        public ICollection<SystemStatusTag> SystemStatusTags { get; set; } = new List<SystemStatusTag>();
        public ICollection<UserDefinedTags> UserDefinedTags { get; set; } = new List<UserDefinedTags>();
        public ICollection<ContactItemRole> ContactItemRoles { get; set; } = new List<ContactItemRole>();
    }
}