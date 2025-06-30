using MamMap.Application.System.Package;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Package;
using MamMap.ViewModels.System.Taste;
using Microsoft.AspNetCore.Mvc;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PremiumPackageController : ControllerBase
    {
        private readonly IPackageService _service;

        public PremiumPackageController(IPackageService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreatePremiumPackageDTO dto)
        {
            var (isSuccess, errorMessage, package) = await _service.CreateAsync(dto);

            if (!isSuccess)
                return BadRequest(new { status = 400, message = errorMessage });

            return Ok(new
            {
                status = 200,
                message = "Tạo gói premium thành công.",
                data = new
                {
                    id = package!.Id,
                    package.Name,
                    package.Price,
                    descriptions = dto.Descriptions
                }
            });
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("getById")]
        public async Task<IActionResult> GetById(int id)
        {
            var package = await _service.GetByIdAsync(id);
            if (package == null)
                return NotFound(new { status = 404, message = "Không tìm thấy gói premium." });

            return Ok(new
            {
                status = 200,
                message = "Lấy thông tin thành công.",
                data = new
                {
                    package.Id,
                    package.Name,
                    package.Price,
                    descriptions = package.Descriptions.Select(d => d.Description).ToList()
                }
            });
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchPackage([FromBody] SearchPackageRequest request)
        {
            var result = await _service.SearchPackageAsync(request);
            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdatePremiumPackage(int id, [FromBody] UpdatePremiumPackageDTO dto)
        {
            var (isSuccess, errorMessage) = await _service.UpdatePremiumPackageAsync(id, dto);

            if (!isSuccess)
            {
                return NotFound(new
                {
                    status = 404,
                    message = errorMessage
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Cập nhật gói premium thành công."
            });
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound(new { status = 404, message = "Không tìm thấy gói để xóa." });

            return Ok(new { status = 200, message = "Xóa gói premium thành công." });
        }
    }
}
