using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.DTOs.PersonDTOs
{
    public class PersonCompositeFilter
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? CircleName { get; set; }        // Organization
        public string? ContactItemRole { get; set; }    // Role
        public string? SystemStatusTagName { get; set; }
        public string? UserDefinedTagName { get; set; }
    }

}
