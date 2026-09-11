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
    public class ConnectionChannel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ConnectionChannelId { get; set; }

        [StringLength(100)]
        public string ConnectionChannelName { get; set; } = string.Empty;

        public ICollection<Person> People { get; set; } = new List<Person>();

    }
}
