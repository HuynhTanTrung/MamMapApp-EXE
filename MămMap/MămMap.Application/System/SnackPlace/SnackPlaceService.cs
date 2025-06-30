using MamMap.Application.System.Dish;
using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.SnackPlace;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MamMap.Application.System.SnackPlace
{
    public class SnackPlaceService : ISnackPlaceService
    {
        private readonly MamMapDBContext _context;

        public SnackPlaceService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<SnackPlaces?> CreateSnackPlaceAsync(SnackPlaces snackPlace, List<Guid>? tasteIds, List<Guid>? dietIds, List<Guid>? foodTypeIds)
        {
            _context.SnackPlaces.Add(snackPlace);

            var attributes = new List<SnackPlaceAttributes>();

            if (tasteIds != null)
            {
                attributes.AddRange(tasteIds.Select(tid => new SnackPlaceAttributes
                {
                    Id = Guid.NewGuid(),
                    SnackPlaceId = snackPlace.SnackPlaceId,
                    TasteId = tid
                }));
            }

            if (dietIds != null)
            {
                attributes.AddRange(dietIds.Select(did => new SnackPlaceAttributes
                {
                    Id = Guid.NewGuid(),
                    SnackPlaceId = snackPlace.SnackPlaceId,
                    DietId = did
                }));
            }

            if (foodTypeIds != null)
            {
                attributes.AddRange(foodTypeIds.Select(fid => new SnackPlaceAttributes
                {
                    Id = Guid.NewGuid(),
                    SnackPlaceId = snackPlace.SnackPlaceId,
                    FoodTypeId = fid
                }));
            }

            _context.SnackPlaceAttributes.AddRange(attributes);

            var result = await _context.SaveChangesAsync();
            return result > 0 ? snackPlace : null;
        }

        public async Task<object> SearchSnackPlacesAsync(SearchSnackPlaceRequest request)
        {
            if (request.PageNum <= 0) request.PageNum = 1;
            if (request.PageSize <= 0) request.PageSize = 10;

            var query = _context.SnackPlaces.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchKeyword))
            {
                query = query.Where(p =>
                    p.PlaceName.Contains(request.SearchKeyword) ||
                    p.OwnerName.Contains(request.SearchKeyword) ||
                    p.Address.Contains(request.SearchKeyword) ||
                    p.Email.Contains(request.SearchKeyword));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(p => p.Status == request.Status.Value);
            }

            var total = await query.CountAsync();

            var snackPlaces = await query
                .Include(sp => sp.BusinessModels)
                .Include(sp => sp.SnackPlaceAttributes)
                    .ThenInclude(attr => attr.Taste)
                .Include(sp => sp.SnackPlaceAttributes)
                    .ThenInclude(attr => attr.Diet)
                .Include(sp => sp.SnackPlaceAttributes)
                    .ThenInclude(attr => attr.FoodType)
                .Include(sp => sp.User)
                    .ThenInclude(u => u.UserPremiumPackages.Where(pp => pp.IsActive))
                    .ThenInclude(pp => pp.PremiumPackage)
                .ToListAsync();

            var averageRatingsMap = await _context.Reviews
                .Where(r => r.Status)
                .GroupBy(r => r.SnackPlaceId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => Math.Round(
                        g.Select(r => new[] {
                    Math.Clamp(r.TasteRating, 1, 5),
                    Math.Clamp(r.PriceRating, 1, 5),
                    Math.Clamp(r.SanitaryRating, 1, 5),
                    Math.Clamp(r.TextureRating, 1, 5),
                    Math.Clamp(r.ConvenienceRating, 1, 5)
                        }.Average()).Average(),
                        2
                    )
                );

            snackPlaces = snackPlaces
                .Select(sp =>
                {
                    var premiumLevel = sp.User?.UserPremiumPackages?.Where(pp => pp.IsActive).Max(pp => (int?)pp.PremiumPackageId);
                    var averageRating = averageRatingsMap.ContainsKey(sp.SnackPlaceId)
                        ? averageRatingsMap[sp.SnackPlaceId]
                        : 0;

                    var sortKey = premiumLevel.HasValue ? 1000 + premiumLevel.Value : averageRating;

                    return new { sp, sortKey };
                })
                .OrderByDescending(x => x.sortKey)
                .ThenBy(x => x.sp.PlaceName)
                .Skip((request.PageNum - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => x.sp)
                .ToList();

            var pageData = snackPlaces.Select(sp => new
            {
                snackPlaceId = sp.SnackPlaceId,
                placeName = sp.PlaceName,
                ownerName = sp.OwnerName,
                address = sp.Address,
                email = sp.Email,
                phoneNumber = sp.PhoneNumber,
                description = sp.Description,
                coordinates = sp.Coordinates,
                openingHour = sp.OpeningHour,
                averagePrice = sp.AveragePrice,
                image = sp.Image,
                mainDish = sp.MainDish,
                status = sp.Status,
                userId = sp.UserId,
                businessModelId = sp.BusinessModelId,
                businessModelName = sp.BusinessModels?.Name,
                attributes = new
                {
                    tastes = sp.SnackPlaceAttributes
                        .Where(attr => attr.Taste != null)
                        .Select(attr => new
                        {
                            tasteId = attr.TasteId,
                            tasteName = attr.Taste!.Description
                        }).Distinct().ToList(),

                    diets = sp.SnackPlaceAttributes
                        .Where(attr => attr.Diet != null)
                        .Select(attr => new
                        {
                            dietId = attr.DietId,
                            dietName = attr.Diet!.Description
                        }).Distinct().ToList(),

                    foodTypes = sp.SnackPlaceAttributes
                        .Where(attr => attr.FoodType != null)
                        .Select(attr => new
                        {
                            foodTypeId = attr.FoodTypeId,
                            foodTypeName = attr.FoodType!.Description
                        }).Distinct().ToList()
                },
                premiumPackage = sp.User?.UserPremiumPackages
                    ?.Where(pp => pp.IsActive)
                    .Select(pp => new
                    {
                        packageId = pp.PremiumPackageId,
                        packageName = pp.PremiumPackage.Name,
                        purchaseDate = pp.PurchaseDate,
                        isActive = pp.IsActive
                    })
                    .FirstOrDefault()
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy danh sách quán ăn thành công.",
                data = new
                {
                    pageData,
                    pageInfo = new
                    {
                        pageNum = request.PageNum,
                        pageSize = request.PageSize,
                        total,
                        totalPages = (int)Math.Ceiling((double)total / request.PageSize)
                    }
                }
            };
        }

        public async Task<object> GetAllSnackPlacesAsync()
        {
            var snackPlaces = await _context.SnackPlaces
                .Include(sp => sp.BusinessModels)
                .Include(sp => sp.SnackPlaceAttributes)
                    .ThenInclude(attr => attr.Taste)
                .Include(sp => sp.SnackPlaceAttributes)
                    .ThenInclude(attr => attr.Diet)
                .Include(sp => sp.SnackPlaceAttributes)
                    .ThenInclude(attr => attr.FoodType)
                .ToListAsync();

            var snackPlaceData = snackPlaces.Select(sp => new
            {
                snackPlaceId = sp.SnackPlaceId,
                placeName = sp.PlaceName,
                ownerName = sp.OwnerName,
                address = sp.Address,
                email = sp.Email,
                phoneNumber = sp.PhoneNumber,
                description = sp.Description,
                coordinates = sp.Coordinates,
                openingHour = sp.OpeningHour,
                averagePrice = sp.AveragePrice,
                image = sp.Image,
                mainDish = sp.MainDish,
                status = sp.Status,
                userId = sp.UserId,
                businessModelId = sp.BusinessModelId,
                businessModelName = sp.BusinessModels?.Name,
                attributes = new
                {
                    tastes = sp.SnackPlaceAttributes
                        .Where(attr => attr.Taste != null)
                        .Select(attr => new
                        {
                            tasteId = attr.TasteId,
                            tasteName = attr.Taste!.Description
                        }).Distinct().ToList(),

                    diets = sp.SnackPlaceAttributes
                        .Where(attr => attr.Diet != null)
                        .Select(attr => new
                        {
                            dietId = attr.DietId,
                            dietName = attr.Diet!.Description
                        }).Distinct().ToList(),

                    foodTypes = sp.SnackPlaceAttributes
                        .Where(attr => attr.FoodType != null)
                        .Select(attr => new
                        {
                            foodTypeId = attr.FoodTypeId,
                            foodTypeName = attr.FoodType!.Description
                        }).Distinct().ToList()
                }
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy tất cả quán ăn thành công.",
                data = snackPlaceData
            };
        }

        public async Task<SnackPlaces?> GetSnackPlaceByIdAsync(Guid id)
        {
            return await _context.SnackPlaces
               .Include(sp => sp.BusinessModels)
               .Include(sp => sp.SnackPlaceAttributes)
                   .ThenInclude(attr => attr.Taste)
               .Include(sp => sp.SnackPlaceAttributes)
                   .ThenInclude(attr => attr.Diet)
               .Include(sp => sp.SnackPlaceAttributes)
                   .ThenInclude(attr => attr.FoodType)
               .Include(sp => sp.User)
                    .ThenInclude(u => u.UserPremiumPackages.Where(pp => pp.IsActive))
                    .ThenInclude(pp => pp.PremiumPackage)
               .FirstOrDefaultAsync(sp => sp.SnackPlaceId == id);
        }

        public async Task<SnackPlaces?> UpdateSnackPlaceAsync(UpdateSnackPlaceDTO dto)
        {
            var place = await _context.SnackPlaces
                .Include(sp => sp.SnackPlaceAttributes)
                .FirstOrDefaultAsync(sp => sp.SnackPlaceId == dto.SnackPlaceId);

            if (place == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.PlaceName))
                place.PlaceName = dto.PlaceName;

            if (!string.IsNullOrWhiteSpace(dto.OwnerName))
                place.OwnerName = dto.OwnerName;

            if (!string.IsNullOrWhiteSpace(dto.Address))
                place.Address = dto.Address;

            if (!string.IsNullOrWhiteSpace(dto.Email))
                place.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                place.PhoneNumber = dto.PhoneNumber;

            if (dto.OpeningHour != null)
                place.OpeningHour = dto.OpeningHour;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                place.Description = dto.Description;

            if (!string.IsNullOrWhiteSpace(dto.Coordinates))
                place.Coordinates = dto.Coordinates;

            if (dto.AveragePrice.HasValue)
                place.AveragePrice = dto.AveragePrice.Value;

            if (!string.IsNullOrWhiteSpace(dto.Image))
                place.Image = dto.Image;

            if (!string.IsNullOrWhiteSpace(dto.MainDish))
                place.MainDish = dto.MainDish;

            if (dto.BusinessModelId.HasValue)
                place.BusinessModelId = dto.BusinessModelId.Value;

            _context.SnackPlaceAttributes.RemoveRange(place.SnackPlaceAttributes);

            var newAttributes = new List<SnackPlaceAttributes>();

            if (dto.TasteIds != null)
            {
                newAttributes.AddRange(dto.TasteIds.Select(id => new SnackPlaceAttributes
                {
                    SnackPlaceId = place.SnackPlaceId,
                    TasteId = id
                }));
            }

            if (dto.DietIds != null)
            {
                newAttributes.AddRange(dto.DietIds.Select(id => new SnackPlaceAttributes
                {
                    SnackPlaceId = place.SnackPlaceId,
                    DietId = id
                }));
            }

            if (dto.FoodTypeIds != null)
            {
                newAttributes.AddRange(dto.FoodTypeIds.Select(id => new SnackPlaceAttributes
                {
                    SnackPlaceId = place.SnackPlaceId,
                    FoodTypeId = id
                }));
            }

            await _context.SnackPlaceAttributes.AddRangeAsync(newAttributes);
            await _context.SaveChangesAsync();
            return place;
        }

        public async Task<(bool isSuccess, string? errorMessage)> DeleteSnackPlaceAsync(Guid id)
        {
            var Sn = await _context.SnackPlaces.FirstOrDefaultAsync(d => d.SnackPlaceId == id && d.Status != false);
            if (Sn == null)
                return (false, "Không tìm thấy quán ăn.");

            Sn.Status = false;
            _context.SnackPlaces.Update(Sn);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool Success, string ErrorMessage)> LogClickAsync(Guid userId, Guid snackPlaceId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return (false, "User not found.");

            var snackPlaceExists = await _context.SnackPlaces.AnyAsync(s => s.SnackPlaceId == snackPlaceId);
            if (!snackPlaceExists)
                return (false, "Snack place not found.");

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var alreadyClickedToday = await _context.SnackPlaceClicks
                .AnyAsync(c =>
                    c.UserId == userId &&
                    c.SnackPlaceId == snackPlaceId &&
                    c.ClickedAt >= today &&
                    c.ClickedAt < tomorrow);

            if (alreadyClickedToday)
                return (false, "You've already clicked this snack place today.");

            var click = new SnackPlaceClick
            {
                UserId = userId,
                SnackPlaceId = snackPlaceId,
                ClickedAt = DateTime.UtcNow
            };

            _context.SnackPlaceClicks.Add(click);
            await _context.SaveChangesAsync();

            return (true, "Click logged successfully.");
        }

        public async Task<object> GetClickStatisticsAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            var snackPlaceId = await _context.SnackPlaces
                .Where(sp => sp.UserId == userId)
                .Select(sp => sp.SnackPlaceId)
                .FirstOrDefaultAsync();

            if (snackPlaceId == Guid.Empty)
            {
                return new
                {
                    status = 404,
                    message = "Không tìm thấy quán ăn vặt cho người dùng hiện tại."
                };
            }

            var normalizedStartDate = startDate.Date;
            var normalizedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            var uniqueClickCount = await _context.SnackPlaceClicks
                .Where(c => c.SnackPlaceId == snackPlaceId
                            && c.ClickedAt >= normalizedStartDate
                            && c.ClickedAt <= normalizedEndDate)
                .Select(c => c.UserId)
                .Distinct()
                .CountAsync();

            var clicks = await _context.SnackPlaceClicks
                .Where(c => c.SnackPlaceId == snackPlaceId
                    && c.ClickedAt >= startDate
                    && c.ClickedAt <= normalizedEndDate)
                .Join(_context.Users,
                      click => click.UserId,
                      user => user.Id,
                      (click, user) => new
                      {
                          click.UserId,
                          click.ClickedAt,
                          user.UserName,
                          user.Image
                      })
                .ToListAsync();

            var grouped = clicks
                .GroupBy(c => c.ClickedAt.DayOfWeek)
                .OrderBy(g => ((int)g.Key + 6) % 7)
                .Select(g => new
                {
                    day = g.Key.ToString(),
                    totalClicks = g.Count(),
                    dateGroup = g.Select(c => new
                    {
                        userId = c.UserId,
                        clickedAt = c.ClickedAt,
                        userName = c.UserName,
                        image = c.Image
                    }).ToList()
                });

            return new
            {
                status = 200,
                message = "Lấy thống kê lượt click thành công.",
                data = new
                {
                    snackPlaceId,
                    uniqueClickCount,
                    startDate,
                    endDate = normalizedEndDate,
                    clicksByDayOfWeek = grouped
                }
            };
        }

        public async Task<object> GetSnackPlaceStatsAsync(Guid snackPlaceId)
        {
            var exists = await _context.SnackPlaces
                .AnyAsync(sp => sp.SnackPlaceId == snackPlaceId && sp.Status != false);

            if (!exists)
            {
                return new
                {
                    status = 404,
                    message = "Không tìm thấy quán ăn.",
                };
            }

            var reviews = await _context.Reviews
                .Where(r => r.SnackPlaceId == snackPlaceId && r.Status != false)
                .ToListAsync();

            var totalRatings = reviews
                .Select(r =>
                {
                    var ratings = new[]
                    {
                Math.Clamp(r.TasteRating, 1, 5),
                Math.Clamp(r.PriceRating, 1, 5),
                Math.Clamp(r.SanitaryRating, 1, 5),
                Math.Clamp(r.TextureRating, 1, 5),
                Math.Clamp(r.ConvenienceRating, 1, 5)
                    };
                    return ratings.Average();
                })
                .ToList();

            var averageRating = totalRatings.Count > 0
                ? Math.Round(totalRatings.Average(), 2)
                : 0.0;

            var numOfComments = reviews
                .Count(r => !string.IsNullOrWhiteSpace(r.Comment));

            double recommendPercent = 0;
            if (totalRatings.Count > 0)
            {
                var starBuckets = totalRatings
                    .Select(r => (int)Math.Round(r, MidpointRounding.AwayFromZero))
                    .Select(r => Math.Clamp(r, 1, 5))
                    .ToList();

                var ratingGroups = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
                foreach (var stars in starBuckets) ratingGroups[stars]++;

                int totalReviews = totalRatings.Count;

                recommendPercent = Math.Round(
                    (ratingGroups[5] * 1.0 +
                     (ratingGroups[1] + ratingGroups[2] + ratingGroups[3] + ratingGroups[4]) * 0.125)
                    * 100.0 / totalReviews,
                    2
                );
            }

            var numOfClicks = await _context.SnackPlaceClicks
                .CountAsync(c => c.SnackPlaceId == snackPlaceId);

            return new
            {
                status = 200,
                message = "Thống kê quán ăn thành công.",
                data = new
                {
                    averageRating,
                    numOfComments,
                    recommendPercent,
                    numOfClicks
                }
            };
        }

        public async Task<object> FilterSnackPlacesAsync(FilterSnackPlaceRequest request)
        {
            var query = _context.SnackPlaces
                .Include(sp => sp.SnackPlaceAttributes)
                .Where(sp => sp.Status);

            if (request.PriceFrom.HasValue)
                query = query.Where(sp => sp.AveragePrice >= request.PriceFrom.Value);

            if (request.PriceTo.HasValue)
                query = query.Where(sp => sp.AveragePrice <= request.PriceTo.Value);

            if (request.TasteIds.Any() || request.DietIds.Any() || request.FoodTypeIds.Any())
            {
                query = query.Where(sp => sp.SnackPlaceAttributes.Any(attr =>
                    (attr.TasteId != null && request.TasteIds.Contains(attr.TasteId.Value)) ||
                    (attr.DietId != null && request.DietIds.Contains(attr.DietId.Value)) ||
                    (attr.FoodTypeId != null && request.FoodTypeIds.Contains(attr.FoodTypeId.Value))
                ));
            }

            var snackPlaces = await query
                .Select(sp => new
                {
                    sp.SnackPlaceId,
                    sp.PlaceName,
                    sp.OwnerName,
                    sp.Address,
                    sp.Email,
                    sp.PhoneNumber,
                    sp.Description,
                    sp.Coordinates,
                    sp.OpeningHour,
                    sp.AveragePrice,
                    sp.Image,
                    sp.MainDish,
                    sp.UserId,
                    sp.BusinessModelId,
                    sp.BusinessModels.Name,
                    sp.Status,
                    attributes = new
                    {
                        tastes = sp.SnackPlaceAttributes
                        .Where(attr => attr.Taste != null)
                        .Select(attr => new
                        {
                            tasteId = attr.TasteId,
                            tasteName = attr.Taste!.Description
                        }).Distinct().ToList(),

                        diets = sp.SnackPlaceAttributes
                        .Where(attr => attr.Diet != null)
                        .Select(attr => new
                        {
                            dietId = attr.DietId,
                            dietName = attr.Diet!.Description
                        }).Distinct().ToList(),

                        foodTypes = sp.SnackPlaceAttributes
                        .Where(attr => attr.FoodType != null)
                        .Select(attr => new
                        {
                            foodTypeId = attr.FoodTypeId,
                            foodTypeName = attr.FoodType!.Description
                        }).Distinct().ToList()
                    }
                })
                .ToListAsync();

            return new
            {
                Status = 200,
                Message = "Filtered snack places retrieved successfully",
                Data = snackPlaces
            };
        }
    }
}