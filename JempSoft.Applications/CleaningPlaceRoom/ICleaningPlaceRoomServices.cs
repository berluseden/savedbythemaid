using JempSoft.Core.Models;
using JempSoft.Core.Result;
using netcore.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Applications.Services
{
    public interface ICleaningPlaceRoomServices
    {
        Task<Result<CleaningPlaceRoom>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<CleaningPlaceRoom>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxOutputAsync(CancellationToken cancellationToken = default);
        Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxByCleaningPlaceIdAsync(int cleaningPlaceId, CancellationToken cancellationToken = default);
        Task<Result<int>> SaveAsync(CleaningPlaceRoomInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateByIdAsync(int id, CleaningPlaceRoomInputDto input, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
        bool Exists(int id);
    }
}
