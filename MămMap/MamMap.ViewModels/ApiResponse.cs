using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels
{
    public class ApiResponse<T>
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; }

        public ApiResponse(int status, string message, T data)
        {
            Status = status;
            Message = message;
            Data = data;
        }
    }
}
