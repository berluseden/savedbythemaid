using netcore.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JempSoft.Core.Models;
using JempSoft.Core.POCOs;

namespace JempSoft.Applications.Services
{ 
    public interface IServiceTypeServices
    {
        ServiceTypeOutputDto Get(int? id);

        List<ComboBoxOutPutDto> GetComboBoxByCleaningPlaceRoomId(int? id);


        JsonResultMessage Save(ServiceTypeInputDto input);

        JsonResultMessage Update(ServiceTypeInputDto input);

        JsonResultMessage Delete(int? id);

        List<ServiceType> GetAll();

    }
}
