using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.Payment
{
    public class SearchPaymentRequest
    {
        public string? PaymentCode { get; set; }
        public int PageNum { get; set; }
        public int PageSize { get; set; }
    }


}
