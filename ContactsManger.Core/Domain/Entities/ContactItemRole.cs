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
    public class ContactItemRole
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ContactsRoleId { get; set; }

        [StringLength(100)]
        public string Role { get; set; } = string.Empty;

        public Guid PersonId { get; set; }

        [ForeignKey("PersonId")]
        public Person? Person { get; set; }

    }
}
