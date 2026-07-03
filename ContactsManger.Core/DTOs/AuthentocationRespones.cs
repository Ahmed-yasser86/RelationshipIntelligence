using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.DTOs
{
    public class AuthentocationRespones
    {
        public string token { get; set; } = String.Empty;
        public string personeName { get; set; } = String.Empty;
        public string personeEmail { get; set; }= String.Empty;

        public string refreshToken { get; set; } = String.Empty;
       
        public DateTime RefreshTokenExpirationTime { get; set; }
        public DateTime ExpirationTime { get; set; } 

    }
}
