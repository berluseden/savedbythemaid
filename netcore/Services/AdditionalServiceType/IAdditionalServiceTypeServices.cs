using netcore.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace netcore.Services.Services
{
    public interface IAdditionalServiceTypeServices
    {
        Task<Result<AdditionalServiceType>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<AdditionalServiceType>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<int>> SaveAsync(AdditionalServiceTypeInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateByIdAsync(int id, AdditionalServiceTypeInputDto input, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
        bool Exists(int id);
    }
}
