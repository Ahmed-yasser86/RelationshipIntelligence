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
    public class SocialMediaAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public Guid SocialMediaAccountId { get; set; }


        public string? Platform { get; set; } = string.Empty;

        [Required]
        public string Url { get; set; } 

        public ICollection<Person> People { get; set; } = new Collection<Person>();

    }
}
