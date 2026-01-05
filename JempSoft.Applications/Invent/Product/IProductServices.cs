using JempSoft.Applications.Invent.Dto;
using JempSoft.Core.Result;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Applications.Invent
{
    public interface IProductServices
    {
        Task<Result<ProductOutputDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Result<List<ProductOutputDto>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<string>> SaveAsync(ProductInputDto input, CancellationToken cancellationToken = default);
        Task<Result> UpdateByIdAsync(string productId, ProductInputDto input, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
        bool Exists(string id);
        Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxAsync(CancellationToken cancellationToken = default);
    }
}
