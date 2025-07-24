using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.AppRating;
using Microsoft.EntityFrameworkCore;

namespace MamMap.Application.System.AppRating
{
    public class AppRatingService : IAppRatingService
    {
        private readonly MamMapDBContext _context;

        private static readonly string[] ValidAppTypes = { "User", "Merchant" };

        public AppRatingService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<(bool isSuccess, string errorMessage, AppRatings? created)> CreateReviewAsync(Guid userId, CreateAppRatingDTO dto)
        {
            var appType = dto.AppType?.Trim();

            if (string.IsNullOrWhiteSpace(appType) || !ValidAppTypes.Contains(appType, StringComparer.OrdinalIgnoreCase))
                return (false, "Loại ứng dụng không hợp lệ. Chỉ chấp nhận 'User' hoặc 'Merchant'.", null);

            appType = ValidAppTypes.First(t => string.Equals(t, appType, StringComparison.OrdinalIgnoreCase));

            if (dto.Star < 1 || dto.Star > 5)
                return (false, "Số sao phải nằm trong khoảng từ 1 đến 5.", null);

            var review = new AppRatings
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AppType = appType,
                Star = dto.Star,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                Status = true
            };

            _context.AppRatings.Add(review);
            await _context.SaveChangesAsync();

            return (true, "", review);
        }

        public async Task<AppRatings?> GetByIdAsync(Guid id)
        {
            return await _context.AppRatings
                .Where(x => x.Id == id && x.Status == true)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AppRatings>> GetByUserIdAsync(Guid userId)
        {
            return await _context.AppRatings
                .Where(x => x.UserId == userId && x.Status == true)
                .ToListAsync();
        }

        public async Task<object> SearchAsync(string? appType, int? minStar, int? maxStar, int pageNum = 1, int pageSize = 10)
        {
            if (pageNum <= 0) pageNum = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.AppRatings
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(appType))
                query = query.Where(x => x.AppType.ToLower() == appType.Trim().ToLower());

            if (minStar.HasValue)
                query = query.Where(x => x.Star >= minStar.Value);

            if (maxStar.HasValue)
                query = query.Where(x => x.Star <= maxStar.Value);

            query = query.Where(x => x.Status == true);

            var total = await query.CountAsync();

            var results = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new
            {
                status = 200,
                message = "Lấy danh sách đánh giá thành công.",
                data = new
                {
                    pageData = results.Select(x => new
                    {
                        id = x.Id,
                        userId = x.UserId,
                        username = x.User.UserName,
                        star = x.Star,
                        description = x.Description,
                        appType = x.AppType,
                        createdDate = x.CreatedAt,

                    }),
                    pageInfo = new
                    {
                        pageNum,
                        pageSize,
                        total,
                        totalPages = (int)Math.Ceiling((double)total / pageSize)
                    }
                }
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var review = await _context.AppRatings.FindAsync(id);
            if (review == null || review.Status != true)
                return false;

            review.Status = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
