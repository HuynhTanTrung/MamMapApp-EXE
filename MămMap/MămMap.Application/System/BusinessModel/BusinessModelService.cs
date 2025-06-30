using MamMap.Application.System.BusinessModel;
using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Other;
using Microsoft.EntityFrameworkCore;

namespace MamMap.Application.System.SnackPlace
{
    public class BusinessModelService : IBusinessModelService
    {
        private readonly MamMapDBContext _context;

        public BusinessModelService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<BusinessModels?> CreateBusinessModelAsync(BusinessModels model)
        {
            _context.BusinessModels.Add(model);
            var result = await _context.SaveChangesAsync();
            return result > 0 ? model : null;
        }

        public async Task<object> SearchBusinessModelsAsync(SearchBMRequest request)
        {
            if (request.PageNum <= 0) request.PageNum = 1;
            if (request.PageSize <= 0) request.PageSize = 10;

            var query = _context.BusinessModels.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchKeyword))
            {
                query = query.Where(d =>
                    d.Name != null &&
                    d.Name.Contains(request.SearchKeyword));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(d => d.Status == request.Status.Value);
            }

            var total = await query.CountAsync();

            var Bms = await query
                .Skip((request.PageNum - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var pageData = Bms.Select(d => new
            {
                id = d.BusinessModelId,
                name = d.Name,
                status = d.Status
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy danh sách mô hình kinh doanh thành công.",
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

        public async Task<object> GetAllBusinessModelsAsync()
        {
            var models = await _context.BusinessModels
                .ToListAsync();

            var data = models.Select(m => new
            {
                m.BusinessModelId,
                m.Name,
                m.Status,
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy tất cả mô hình kinh doanh thành công.",
                data
            };
        }

        public async Task<BusinessModels?> GetByIdAsync(Guid id)
        {
            return await _context.BusinessModels.FirstOrDefaultAsync(x => x.BusinessModelId == id);
        }

        public async Task<(bool isSuccess, string? errorMessage)> UpdateBMAsync(Guid id, string newDescription)
        {
            var bm = await _context.BusinessModels.FirstOrDefaultAsync(d => d.BusinessModelId == id);
            if (bm == null)
                return (false, "Không tìm thấy mô hình kinh doanh.");

            bm.Name = newDescription;
            _context.BusinessModels.Update(bm);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.BusinessModels.FindAsync(id);
            if (entity == null || entity.Status == false)
                return false;

            entity.Status = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
