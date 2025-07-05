using MamMap.Data.Entities;
using MamMap.ViewModels.System.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Payment
{
    public interface IPaymentService
    {
        Task<bool> MarkPaymentAsPaidAsync(string paymentCode);
        Task<object?> CheckPaymentStatusAsync(Guid userId, Guid premiumPackageId);
        Task<List<object>> GetPaymentHistoryAsync(Guid userId);
        Task<List<UserPremiumPackage>> GetUserPremiumPackagesAsync(Guid userId);
        Task<object> SearchPaymentAsync(SearchPaymentRequest request);
        Task<int> CountTotalPaymentsAsync();
    }
}
