using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.User
{
    public class RefreshTokenRequest
    {
        public string AccessToken { get; set; } 
        public string RefreshToken { get; set; }
    }
}
