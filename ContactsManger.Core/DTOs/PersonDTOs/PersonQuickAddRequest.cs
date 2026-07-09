using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ServiceContracts.DTOs
{




    /// <summary>
    /// Minimal-friction contact capture. Only the essentials needed to save
    /// a person quickly; everything else can be filled in later via the
    /// full profile update.
    /// </summary>
    public class PersonQuickAddRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string email { get; set; }

        /// <summary>
        /// Organization name(s). Free-text; backend will get-or-create
        /// matching Circle records.
        /// </summary>
        public List<string>? Organizations { get; set; }

        /// <summary>
        /// Current role/title(s) at the organization(s) above. Free-text;
        /// backend will get-or-create matching ContactItemRole records.
        /// </summary>
        public List<string>? CurrentRoles { get; set; }

        /// <summary>
        /// How you met / meeting context notes.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Source context is too long")]
        public string? Origin { get; set; }

        public Person ToPerson()
        {
            return new Person
            {
                Name = this.Name,
                email = this.email,
                Origin = this.Origin

            };
        }
    }
}