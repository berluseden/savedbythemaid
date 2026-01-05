using JempSoft.Core.Repository;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Core.UnitOfWork
{
    /// <summary>
    /// Unit of Work interface with transaction and async support
    /// </summary>
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets a repository for the specified entity type
        /// </summary>
        IRepository<T> Repository<T>() where T : class;

        /// <summary>
        /// Saves all changes made in this unit of work to the database
        /// </summary>
        Task<int> CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Discards all changes made in this unit of work
        /// </summary>
        void Rollback();

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a value indicating whether there is an active transaction
        /// </summary>
        bool HasActiveTransaction { get; }

        // Legacy sync method
        [Obsolete("Use CommitAsync instead")]
        Task<int> Commit();
    }
}
