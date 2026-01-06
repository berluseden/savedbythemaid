using netcore.Models;
using netcore.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace netcore.Services.Services
{
    public interface IEmployeeScheduleServices
    {
        Task<Result<EmployeeSchedule>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<EmployeeSchedule>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<List<EmployeeSchedule>>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);
        Task<Result<int>> SaveAsync(EmployeeScheduleInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateByIdAsync(int id, EmployeeScheduleInputDto input, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
        bool Exists(int id);
    }
}
