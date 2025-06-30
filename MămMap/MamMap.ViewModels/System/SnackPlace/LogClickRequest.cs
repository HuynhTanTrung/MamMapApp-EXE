using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.SnackPlace
{
    public class LogClickRequest
    {
        public Guid UserId { get; set; }
        public Guid SnackPlaceId { get; set; }
    }

}
