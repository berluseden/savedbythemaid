using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JempSoft.Core.Data;
using JempSoft.Core.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JempSoft.Core.UnitOfWork
{
    /// <summary>
    /// Unit of Work implementation with transaction support
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly JempSoftDbContext _dbContext;
        private readonly Dictionary<Type, object> _repositories;
        private IDbContextTransaction? _currentTransaction;
        private bool _disposed;

        public UnitOfWork(JempSoftDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _repositories = new Dictionary<Type, object>();
        }

        public bool HasActiveTransaction => _currentTransaction != null;

        public IRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);
            
            if (_repositories.TryGetValue(type, out var existingRepo))
            {
                return (IRepository<T>)existingRepo;
            }

            var repository = new BaseRepository<T>(_dbContext);
            _repositories.Add(type, repository);
            return repository;
        }

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public void Rollback()
        {
            foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                    case EntityState.Deleted:
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                }
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress. Commit or rollback the current transaction before starting a new one.");
            }

            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            return _currentTransaction;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No transaction is currently in progress.");
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                return;
            }

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        private async Task DisposeTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        [Obsolete("Use CommitAsync instead")]
        public async Task<int> Commit()
        {
            return await CommitAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _currentTransaction?.Dispose();
                _dbContext.Dispose();
                _repositories.Clear();
            }
            _disposed = true;
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
            }
            await _dbContext.DisposeAsync();
            _repositories.Clear();
        }
    }
}
