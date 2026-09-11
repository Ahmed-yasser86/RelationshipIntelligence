using ContactsManger.Core.CustomValidations;
using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactsManger.Core.Domain.Entities
{
    public class SystemStatusTag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        [EnumRange(typeof(EnSystemStatusTag))]

        public EnSystemStatusTag StatusTagId { get; set; } = EnSystemStatusTag.ModeratePriority;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public ICollection<Person> People { get; set; } = new List<Person>();
    }
}