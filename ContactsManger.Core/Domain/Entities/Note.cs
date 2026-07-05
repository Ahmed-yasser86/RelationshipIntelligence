using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.Domain.Entities
{
    public class Note
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public Guid NoteId { get; set; }

        [Required]
        public EnNoteType NoteType { get; set; } 

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }


        public ICollection<Person> People { get; set; } = new Collection<Person>();
    }
}
