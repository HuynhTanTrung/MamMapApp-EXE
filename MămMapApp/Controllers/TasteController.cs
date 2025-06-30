using MamMap.Application.System.Taste;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Taste;
using Microsoft.AspNetCore.Mvc;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasteController : ControllerBase
    {
        private readonly ITasteService _tasteService;

        public TasteController(ITasteService tasteService)
        {
            _tasteService = tasteService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTaste([FromBody] CreateTasteDTO dto)
        {
            var taste = new Tastes
            {
                Description = dto.Name
            };

            var (isSuccess, errorMessage, createdTaste) = await _tasteService.CreateTasteAsync(taste);

            if (!isSuccess)
            {
                return BadRequest(new
                {
                    status = 400,
                    message = errorMessage
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Tạo khẩu vị thành công.",
                data = new
                {
                    tasteId = createdTaste.Id,
                    name = createdTaste.Description
                }
            });
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchTaste([FromBody] SearchTasteRequest request)
        {
            var result = await _tasteService.SearchTastesAsync(request);
            return Ok(result);
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllTastes()
        {
            var result = await _tasteService.GetAllTastesAsync();
            return Ok(result);
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var entity = await _tasteService.GetByIdAsync(id);
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
                    id = entity.Id,
                    name = entity.Description
                }
            });
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateDiet([FromBody] updateTasteDTO dto)
        {
            var (isSuccess, errorMessage) = await _tasteService.UpdateTasteAsync(dto.Id, dto.Name);

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
                message = "Cập nhập vị món ăn thành công.",
                data = new
                {
                    dto.Id,
                    newName = dto.Name
                }
            });
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _tasteService.DeleteAsync(id);
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
