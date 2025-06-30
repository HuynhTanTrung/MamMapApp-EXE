using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.SnackPlace;

namespace MamMap.Application.System.SnackPlace
{
    public interface ISnackPlaceService
    {
        Task<SnackPlaces?> CreateSnackPlaceAsync(SnackPlaces snackPlace, List<Guid>? tasteIds, List<Guid>? dietIds, List<Guid>? foodTypeIds);
        Task<object> GetAllSnackPlacesAsync();
        Task<SnackPlaces?> GetSnackPlaceByIdAsync(Guid id);
        Task<SnackPlaces?> UpdateSnackPlaceAsync(UpdateSnackPlaceDTO dto);
        Task<(bool isSuccess, string? errorMessage)> DeleteSnackPlaceAsync(Guid id);
        Task<object> SearchSnackPlacesAsync(SearchSnackPlaceRequest request);
        Task<(bool Success, string ErrorMessage)> LogClickAsync(Guid userId, Guid snackPlaceId);
        Task<object> GetClickStatisticsAsync(Guid userId, DateTime startDate, DateTime endDate);
        Task<object> GetSnackPlaceStatsAsync(Guid snackPlaceId);
        Task<object> FilterSnackPlacesAsync(FilterSnackPlaceRequest request);
    }
}
