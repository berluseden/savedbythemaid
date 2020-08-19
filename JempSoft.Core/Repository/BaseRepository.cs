using JempSoft.Core.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JempSoft.Core.Repository
{
    public class BaseRepository<T> : IRepository<T> where T : class
    {
        internal readonly JempSoftDbContext _context;
        internal DbSet<T> _dbSet;


        public BaseRepository(JempSoftDbContext context)
        {
            _context = context;
            this._dbSet = _context.Set<T>();
        }

        public virtual void Delete(T entity)
        {
            if(_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }

        public virtual void Delete(object id)
        {
            var entity = _dbSet.Find(id);
            Delete(entity);
        }

        public virtual void SoDelete(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }


        public virtual List<T> GetAll()
        {
            return _dbSet.ToList();
        }
        

        public virtual T GetById(object id)
        {
            return _dbSet.Find(id);
        }

        public virtual T Save(T entity)
        {
            _dbSet.Add(entity);
            return entity;
        }

        public virtual void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
}
