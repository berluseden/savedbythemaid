using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JempSoft.Core.Data;
using JempSoft.Core.Models;
using JempSoft.Core.Repository;

namespace JempSoft.Core.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private JempSoftDbContext _dbContext;
        private readonly Dictionary<Type, object> _repositories = new Dictionary<Type, object>();

        public Dictionary<Type, object> Repositories
        {
            get { return _repositories; }
            set { Repositories = value; }
        }

        public UnitOfWork(JempSoftDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IRepository<T> Repository<T>() where T : class
        {
            if (Repositories.Keys.Contains(typeof(T)))
            {
                return Repositories[typeof(T)] as IRepository<T>;
            }

            IRepository<T> repo = new BaseRepository<T>(_dbContext);
            Repositories.Add(typeof(T), repo);
            return repo;
        }

        public async Task<int> Commit()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public void Rollback()
        {
            _dbContext.ChangeTracker.Entries().ToList().ForEach(x => x.Reload());
        }

        public void Dispose()
        {
            this._dbContext.Dispose();
        }
    }
}
