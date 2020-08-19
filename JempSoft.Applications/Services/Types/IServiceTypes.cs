using JempSoft.Applications;
using JempSoft.Core.Models;
using JempSoft.Core.POCOs;
using System.Collections.Generic;

namespace netcore.Services.ServiceTypes
{
    public interface IServiceTypeServices
    {
        ServiceType Get(int? id);

        JsonResultMessage Save(ServiceTypeInputDto input);

        JsonResultMessage Update(ServiceTypeInputDto input);

        JsonResultMessage Delete(int? id);

        List<ServiceType> GetAll();

    }
}
