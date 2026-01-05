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
    /// Service interface for service type operations
    /// </summary>
    public interface IServiceTypeServices
    {
        Task<Result<ServiceTypeOutputDto>> GetAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<ServiceType>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<ServiceType>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxByCleaningPlaceRoomIdAsync(int cleaningPlaceRoomId, CancellationToken cancellationToken = default);
        Task<Result<int>> SaveAsync(ServiceTypeInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(ServiceTypeInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateByIdAsync(int serviceTypeId, ServiceTypeInputDto input, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
        bool Exists(int id);
    }
}
