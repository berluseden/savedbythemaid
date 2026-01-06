using netcore.Models;
using netcore.Services.ServiceMeet.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace netcore.Services.ServiceMeet
{
    /// <summary>
    /// Service interface for service meet (appointment) operations
    /// </summary>
    public interface IServiceMeetServices
    {
        Task<Result<ServiceMeetOutputDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<ServiceMeetOutputDto>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<List<ServiceMeetOutputDto>>> GetByCartItemIdAsync(int cartItemId, CancellationToken cancellationToken = default);
        Task<Result<int>> SaveAsync(ServiceMeetInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateByIdAsync(int serviceMeetId, ServiceMeetInputDto input, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
        bool Exists(int id);
        Task<Result<List<ComboBoxOutPutDto>>> GetCartItemsComboBoxAsync(CancellationToken cancellationToken = default);
    }
}
