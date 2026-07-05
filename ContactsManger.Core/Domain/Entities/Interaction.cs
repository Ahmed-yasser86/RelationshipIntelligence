using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactsManger.Core.Domain.Entities
{
    public class Interaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public Guid InteractionId { get; set; }

        [Required]
        public EnInteractionType InteractionType { get; set; } 

        [Required]
        [StringLength(100)]
        public string InteractionTitle { get; set; } = string.Empty;
        public string? InteractionDescription { get; set; }

        [Required]
        public DateTime TimeOfInteraction { get; set; }

        public ICollection<Person> People { get; set; } = new List<Person>();
    }
}