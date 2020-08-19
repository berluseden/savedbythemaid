using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Applications
{
    public interface IApplicationServices<T> where T: class
    {
        List<T> GetAll();

        T GetById(int? id);

        void Save(T input);

        void Update(T input);

        void Delete(int? id);
    }
}
