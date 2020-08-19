using netcore.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JempSoft.Core.Models;
using JempSoft.Core.POCOs;

namespace JempSoft.Applications.Services
{
    public interface ICleaningPlaceServices
    {
        CleaningPlace Get(int? id);

        JsonResultMessage Save(CleaningPlaceInputDto input);

        JsonResultMessage Update(CleaningPlaceInputDto input);

        JsonResultMessage Delete(int? id);

        List<CleaningPlace> GetAll();

        List<ComboBoxOutPutDto> GetComboBoxOutPut();

    }
}
