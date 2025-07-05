using MamMap.Application.System.SnackPlace;
using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.SnackPlace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SnackPlacesController : ControllerBase
    {
        private readonly ISnackPlaceService _snackPlaceService;
        private readonly MamMapDBContext _context;

        public SnackPlacesController(ISnackPlaceService snackPlaceService, MamMapDBContext context)
        {
            _snackPlaceService = snackPlaceService;
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateSnackPlace([FromBody] CreateSnackPlaceDTO dto)
        {
            var snackPlace = new SnackPlaces
            {
                SnackPlaceId = Guid.NewGuid(),
                UserId = dto.UserId,
                PlaceName = dto.PlaceName,
                OwnerName = dto.OwnerName,
                Address = dto.Address,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Description = dto.Description,
                Coordinates = dto.Coordinates,
                OpeningHour = dto.OpeningHour,
                AveragePrice = dto.AveragePrice,
                Image = dto.Image,
                BusinessModelId = dto.BusinessModelId,
                MainDish = dto.MainDish,
            };

            var created = await _snackPlaceService.CreateSnackPlaceAsync(
                snackPlace,
                dto.TasteIds,
                dto.DietIds,
                dto.FoodTypeIds
            );

            if (created != null)
            {
                return Ok(new
                {
                    status = 200,
                    message = "Tạo quán ăn vặt thành công."
                });
            }

            return BadRequest(new
            {
                status = 400,
                message = "Tạo quán ăn vặt thất bại."
            });
        }

        [HttpPost("search-snackplaces")]
        public async Task<IActionResult> SearchSnackPlaces([FromBody] SearchSnackPlaceRequest request)
        {
            var result = await _snackPlaceService.SearchSnackPlacesAsync(request);
            return Ok(result);
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllSnackPlaces()
        {
            var result = await _snackPlaceService.GetAllSnackPlacesAsync();
            return Ok(result);
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetSnackPlaceById(Guid id)
        {
            var snackPlace = await _snackPlaceService.GetSnackPlaceByIdAsync(id);

            if (snackPlace == null)
            {
                return NotFound(new
                {
                    status = 404,
                    message = "Quán ăn không tồn tại."
                });
            }

            var premiumPackage = await _context.UserPremiumPackages
                .Include(u => u.PremiumPackage)
                .Where(u => u.UserId == snackPlace.UserId && u.IsActive)
                .OrderByDescending(u => u.PurchaseDate)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                status = 200,
                message = "Lấy thông tin quán ăn thành công.",
                data = new
                {
                    snackPlaceId = snackPlace.SnackPlaceId,
                    placeName = snackPlace.PlaceName,
                    ownerName = snackPlace.OwnerName,
                    address = snackPlace.Address,
                    email = snackPlace.Email,
                    phoneNumber = snackPlace.PhoneNumber,
                    description = snackPlace.Description,
                    coordinates = snackPlace.Coordinates,
                    openingHour = snackPlace.OpeningHour,
                    averagePrice = snackPlace.AveragePrice,
                    image = snackPlace.Image,
                    mainDish = snackPlace.MainDish,
                    status = snackPlace.Status,
                    closed = snackPlace.IsTemporarilyClosed,
                    userId = snackPlace.UserId,
                    businessModelId = snackPlace.BusinessModelId,
                    businessModelName = snackPlace.BusinessModels?.Name,
                    attributes = new
                    {
                        tastes = snackPlace.SnackPlaceAttributes?
                            .Where(attr => attr.Taste != null)
                            .Select(attr => new
                            {
                                tasteId = attr.TasteId,
                                tasteName = attr.Taste!.Description
                            }).Distinct().ToList(),

                        diets = snackPlace.SnackPlaceAttributes?
                            .Where(attr => attr.Diet != null)
                            .Select(attr => new
                            {
                                dietId = attr.DietId,
                                dietName = attr.Diet!.Description
                            }).Distinct().ToList(),

                        foodTypes = snackPlace.SnackPlaceAttributes?
                            .Where(attr => attr.FoodType != null)
                            .Select(attr => new
                            {
                                foodTypeId = attr.FoodTypeId,
                                foodTypeName = attr.FoodType!.Description
                            }).Distinct().ToList()
                    },
                    premiumPackage = premiumPackage == null ? null : new
                    {
                        premiumPackageId = premiumPackage.PremiumPackageId,
                        packageName = premiumPackage.PremiumPackage.Name,
                        purchaseDate = premiumPackage.PurchaseDate,
                        isActive = premiumPackage.IsActive
                    }
                }
            });
        }

        [HttpPost("filter")]
        public async Task<IActionResult> FilterSnackPlaces([FromBody] FilterSnackPlaceRequest request)
        {
            var result = await _snackPlaceService.FilterSnackPlacesAsync(request);
            return Ok(result);
        }

        [HttpGet("getAllAttributes")]
        public async Task<IActionResult> GetAllAttributes()
        {
            var tastes = await _context.Set<Tastes>()
                .Where(t => t.Status != false)
                .Select(t => new AttributeItem
                {
                    Id = t.Id,
                    Name = t.Description,
                })
                .ToListAsync();

            var diets = await _context.Set<Diets>()
                .Where(d => d.Status != false)
                .Select(d => new AttributeItem
                {
                    Id = d.Id,
                    Name = d.Description
                })
                .ToListAsync();

            var foodTypes = await _context.Set<FoodTypes>()
                .Where(f => f.Status != false)
                .Select(f => new AttributeItem
                {
                    Id = f.Id,
                    Name = f.Description
                })
                .ToListAsync();

            var response = new AttributeGroupResponse
            {
                Tastes = tastes,
                Diets = diets,
                FoodTypes = foodTypes
            };

            return Ok(new
            {
                Status = 200,
                Message = "Attributes fetched successfully",
                Data = response
            });
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateSnackPlace([FromBody] UpdateSnackPlaceDTO dto)
        {
            var updatedPlace = await _snackPlaceService.UpdateSnackPlaceAsync(dto);

            if (updatedPlace == null)
            {
                return BadRequest(new
                {
                    status = 400,
                    message = "Không tìm thấy quán ăn hoặc cập nhật thất bại."
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Cập nhật quán ăn thành công.",
                data = new
                {
                    snackPlaceId = updatedPlace.SnackPlaceId,
                    placeName = updatedPlace.PlaceName,
                    ownerName = updatedPlace.OwnerName,
                    address = updatedPlace.Address,
                    email = updatedPlace.Email,
                    description = updatedPlace.Description,
                    coordinates = updatedPlace.Coordinates,
                    openingHour = updatedPlace.OpeningHour,
                    averagePrice = updatedPlace.AveragePrice,
                    image = updatedPlace.Image,
                    mainDish = updatedPlace.MainDish,
                    phoneNumber = updatedPlace.PhoneNumber,
                    businessModelId = updatedPlace.BusinessModelId,
                    businessModelName = updatedPlace.BusinessModels?.Name,
                    attributes = new
                    {
                        tastes = updatedPlace.SnackPlaceAttributes?
                            .Where(attr => attr.Taste != null)
                            .Select(attr => new
                            {
                                tasteId = attr.TasteId,
                                tasteName = attr.Taste!.Description
                            }).Distinct().ToList(),

                        diets = updatedPlace.SnackPlaceAttributes?
                            .Where(attr => attr.Diet != null)
                            .Select(attr => new
                            {
                                dietId = attr.DietId,
                                dietName = attr.Diet!.Description
                            }).Distinct().ToList(),

                        foodTypes = updatedPlace.SnackPlaceAttributes?
                            .Where(attr => attr.FoodType != null)
                            .Select(attr => new
                            {
                                foodTypeId = attr.FoodTypeId,
                                foodTypeName = attr.FoodType!.Description
                            }).Distinct().ToList()
                    }
                }
            });
        }

        [HttpPut("closeToggle")]
        public async Task<IActionResult> ToggleTemporaryClose([FromBody] ToggleCloseRequest request)
        {
            var (isSuccess, message) = await _snackPlaceService.ToggleTemporaryCloseAsync(request.SnackPlaceId);

            if (!isSuccess)
            {
                return NotFound(new
                {
                    status = "error",
                    message
                });
            }

            return Ok(new
            {
                status = 200,
                message
            });
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteSnackPlace([FromBody] DeleteSnackPlaceRequest request)
        {
            var (isSuccess, errorMessage) = await _snackPlaceService.DeleteSnackPlaceAsync(request.snackPlaceId);

            if (!isSuccess)
            {
                return NotFound(new
                {
                    status = "error",
                    message = errorMessage
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Xóa quán ăn thành công."
            });
        }

        [HttpPost("click")]
        public async Task<IActionResult> LogClick([FromBody] LogClickRequest request)
        {
            var (success, errorMessage) = await _snackPlaceService.LogClickAsync(request.UserId, request.SnackPlaceId);

            return Ok(new
            {
                status = 200,
                message = success ? "Click logged successfully." : errorMessage,
                success
            });
        }

        [HttpGet("getClick")]
        [Authorize]
        public async Task<IActionResult> GetClickStatistics([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new
                {
                    status = 401,
                    message = "Không tìm thấy thông tin người dùng."
                });
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest(new
                {
                    status = 400,
                    message = "UserId không hợp lệ."
                });
            }

            var result = await _snackPlaceService.GetClickStatisticsAsync(userId, startDate, endDate);

            return Ok(result);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetSnackPlaceStats(Guid id)
        {
            var result = await _snackPlaceService.GetSnackPlaceStatsAsync(id);
            return Ok(result);
        }
    }
}