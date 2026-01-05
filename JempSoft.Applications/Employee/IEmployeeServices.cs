using JempSoft.Core.Models;
using JempSoft.Core.Result;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Applications.Services
{
    public interface IEmployeeServices
    {
        Task<Result<Employee>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<Employee>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<int>> SaveAsync(EmployeeInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateByIdAsync(int id, EmployeeInputDto input, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
        bool Exists(int id);
    }
}
