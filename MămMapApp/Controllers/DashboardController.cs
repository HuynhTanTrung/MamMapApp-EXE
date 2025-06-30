using MamMap.Application.System.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace MamMapApp.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("totals")]
        public async Task<IActionResult> GetTotals()
        {
            var data = new
            {
                Users = await _dashboardService.GetTotalUsersAsync(),
                Merchants = await _dashboardService.GetTotalMerchantsAsync(),
                SnackPlaces = await _dashboardService.GetTotalSnackPlacesAsync(),
                Money = await _dashboardService.GetTotalMoneyAsync(),
            };

            return Ok(new { status = 200, message = "Lấy tổng số thông tin thành công", data });
        }

        [HttpGet("merchantPercentage")]
        public async Task<IActionResult> GetMerchantPercentage()
        {
            var result = await _dashboardService.GetMerchantPercentageAsync();
            return Ok(new { status = 200, data = result });
        }


        [HttpGet("revenueByDate")]
        public async Task<IActionResult> GetRevenueByDate([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _dashboardService.GetRevenueByDateAsync(from, to);
            return Ok(new { status = 200, message = "Lấy doanh thu theo ngày thành công", data = result });
        }

        [HttpGet("revenueByMonth")]
        public async Task<IActionResult> GetRevenueByMonth([FromQuery] int year)
        {
            var result = await _dashboardService.GetRevenueByMonthAsync(year);
            return Ok(new { status = 200, message = "Lấy doanh thu theo tháng thành công", data = result });
        }
    }

}
