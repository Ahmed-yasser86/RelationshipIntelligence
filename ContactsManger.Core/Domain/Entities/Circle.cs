using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.Domain.Entities
{
    public class Circle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public Guid CircleId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }


        public ICollection<Person> People { get; set; } = new List<Person>();

    }
}
