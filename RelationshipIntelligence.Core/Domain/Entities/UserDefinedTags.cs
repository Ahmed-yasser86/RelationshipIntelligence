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
    public class UserDefinedTags
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid TagId { get; set; }

        [StringLength(100)]
        public string TagName { get; set; } = string.Empty;

        public ICollection<Person> People { get; set; } = new List<Person>();

    }
}
