using JempSoft.Core.Models;
using JempSoft.Core.POCOs;
using netcore.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JempSoft.Applications.Services
{
    public interface ICleaningPlaceRoomServices
    {
        CleaningPlaceRoom Get(int? id);

        JsonResultMessage Save(CleaningPlaceRoomInputDto input);

        JsonResultMessage Update(CleaningPlaceRoomInputDto input);

        JsonResultMessage Delete(int? id);

        List<CleaningPlaceRoom> GetAll();

        List<ComboBoxOutPutDto> GetComboBoxOutPut();

        List<ComboBoxOutPutDto> GetComboBoxByCleaningPlaceId(int? id);

    }
}
