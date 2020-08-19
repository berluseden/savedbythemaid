using JempSoft.Core.Models;
using JempSoft.Core.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JempSoft.Core.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : class;

        Task<int> Commit();

        void Rollback();
    }
}
