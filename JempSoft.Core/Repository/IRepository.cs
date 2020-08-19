using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Core.Repository
{
    public interface IRepository<T> where T: class
    {

        List<T> GetAll();

        T GetById(object id);

        T Save(T entity);

        void Update(T entity);

        void Delete(T entity);

        void Delete(object id);

        void SoDelete(T entity);
    }
}
