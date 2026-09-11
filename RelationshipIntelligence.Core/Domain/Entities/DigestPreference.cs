using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class DigestPreference
    {
        [Key]
        public Guid ApplicationUserId { get; set; }

        public bool Enabled { get; set; } = true;

        public double Threshold { get; set; } = 50;

        public int Count { get; set; } = 5;
    }
}
