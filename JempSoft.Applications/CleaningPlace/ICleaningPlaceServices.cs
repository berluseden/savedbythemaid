using JempSoft.Applications;
using JempSoft.Core.Models;
using JempSoft.Core.POCOs;
using JempSoft.Core.Result;
using netcore.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Applications.Services
{
    /// <summary>
    /// Service interface for cleaning place operations
    /// </summary>
    public interface ICleaningPlaceServices
    {
        Task<Result<CleaningPlace>> GetAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<CleaningPlace>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<CleaningPlace>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxOutputAsync(CancellationToken cancellationToken = default);
        Task<Result<int>> SaveAsync(CleaningPlaceInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(CleaningPlaceInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateByIdAsync(int id, CleaningPlaceInputDto input, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
        bool Exists(int id);
    }
}
