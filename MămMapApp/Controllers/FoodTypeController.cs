using MamMap.Application.System.FoodType;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.FoodType;
using Microsoft.AspNetCore.Mvc;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoodTypeController : ControllerBase
    {
        private readonly IFoodTypeService _foodTypeService;

        public FoodTypeController(IFoodTypeService foodTypeService)
        {
            _foodTypeService = foodTypeService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateFoodType([FromBody] CreateFoodTypeDTO dto)
        {
            var foodType = new FoodTypes
            {
                Description = dto.Name
            };

            var (isSuccess, errorMessage, createdFoodType) = await _foodTypeService.CreateFoodTypeAsync(foodType);

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
                message = "Tạo loại món ăn thành công.",
                data = new
                {
                    foodTypeId = createdFoodType.Id,
                    name = createdFoodType.Description
                }
            });
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchFoodType([FromBody] SearchFoodTypeRequest request)
        {
            var result = await _foodTypeService.SearchFoodTypesAsync(request);
            return Ok(result);
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllFoodTypes()
        {
            var result = await _foodTypeService.GetAllFoodTypesAsync();
            return Ok(result);
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var entity = await _foodTypeService.GetByIdAsync(id);
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
        public async Task<IActionResult> UpdateDiet([FromBody] UpdateFoodTypeDTO dto)
        {
            var (isSuccess, errorMessage) = await _foodTypeService.UpdateTypeAsync(dto.Id, dto.Name);

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
                message = "Cập nhập loại món ăn thành công.",
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
            var success = await _foodTypeService.DeleteAsync(id);
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
