using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.Payment
{
    public class SePayWebhookDTO
    {
        public string gateway { get; set; }
        public string transactionDate { get; set; }
        public string accountNumber { get; set; }
        public string subAccount { get; set; }
        public string code { get; set; }
        public string content { get; set; }
        public string transferType { get; set; }
        public string description { get; set; }
        public long transferAmount { get; set; }
        public string referenceCode { get; set; }
        public int accumulated { get; set; }
        public long id { get; set; }
    }


}
