
using ContactsManger.Core.CustomValidations;
using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactsManger.Core.Domain.Entities
{
    public class Note
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid NoteId { get; set; }

        [Required]
        [EnumRange(typeof(EnNoteType))]
        public EnNoteType NoteType { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        public Guid PersonId { get; set; }

        [ForeignKey("PersonId")]
        public Person? Person { get; set; }
    }
}

