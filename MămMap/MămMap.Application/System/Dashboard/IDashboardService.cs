using MamMap.ViewModels.System.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Dashboard
{
    public interface IDashboardService
    {
        Task<int> GetTotalUsersAsync();
        Task<int> GetTotalMerchantsAsync();
        Task<int> GetTotalSnackPlacesAsync();
        Task<decimal> GetTotalMoneyAsync();
        Task<object> GetMerchantPercentageAsync();
        Task<IEnumerable<RevenueByDateDto>> GetRevenueByDateAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<RevenueByMonthDto>> GetRevenueByMonthAsync(int year);
    }

}
