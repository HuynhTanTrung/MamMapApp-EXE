using MamMap.Application.System.BusinessModel;
using MamMap.Application.System.SnackPlace;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Other;
using Microsoft.AspNetCore.Mvc;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessModelsController : ControllerBase
    {
        private readonly IBusinessModelService _businessModelService;

        public BusinessModelsController(IBusinessModelService businessModelService)
        {
            _businessModelService = businessModelService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBusinessModel([FromBody] CreateBusinessModelDTO dto)
        {
            var model = new BusinessModels
            {
                BusinessModelId = Guid.NewGuid(),
                Name = dto.Name,
            };

            var result = await _businessModelService.CreateBusinessModelAsync(model);

            if (result != null)
            {
                return Ok(new
                {
                    status = 200,
                    message = "Tạo mô hình kinh doanh thành công."
                });
            }

            return BadRequest(new
            {
                status = 400,
                message = "Tạo mô hình kinh doanh thất bại."
            });
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchBusinessModel([FromBody] SearchBMRequest request)
        {
            var result = await _businessModelService.SearchBusinessModelsAsync(request);
            return Ok(result);
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _businessModelService.GetAllBusinessModelsAsync();
            return Ok(result);
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var entity = await _businessModelService.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound(new
                {
                    status = 404,
                    message = "Không tìm thấy dữ liệu."
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Lấy thông tin thành công.",
                data = new
                {
                    id = entity.BusinessModelId,
                    name = entity.Name
                }
            });
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateBusinessModel([FromBody] updateBusinessModelDTO dto)
        {
            var (isSuccess, errorMessage) = await _businessModelService.UpdateBMAsync(dto.Id, dto.Name);

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
                message = "Cập nhật mô hình kinh doanh ăn thành công.",
                data = new
                {
                    dto.Id,
                    newNamw = dto.Name
                }
            });
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _businessModelService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new
                {
                    status = 404,
                    message = "Không tìm thấy dữ liệu để xóa."
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Xóa thành công (đã đặt trạng thái về null)."
            });
        }
    }
}