using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.User
{
    public class SearchUserRequest
    {
        public string? SearchKeyword { get; set; } 
        public bool? Status { get; set; }
        public string? Role { get; set; }
        public int PageNum { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }


}
