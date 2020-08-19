using System;
using System.Collections.Generic;
using System.Linq;
using netcore.Dto;
using JempSoft.Core.POCOs;
using JempSoft.Core.Models;
using JempSoft.Core.Data;

namespace JempSoft.Applications.Services
{
    public class CleaningPlaceServices : ICleaningPlaceServices
    {

        private readonly JempSoftDbContext _context;

        public CleaningPlaceServices(JempSoftDbContext context)
        {
            _context = context;
        }


        public JsonResultMessage Delete(int? id)
        {
            throw new NotImplementedException();
        }

        public CleaningPlace Get(int? id)
        {
            try
            {
                var cleaningPlace = _context.CleaningPlaces.FirstOrDefault(c => c.CleaningPlaceId == id);

                return new CleaningPlace
                {
                    CleaningPlaceId = cleaningPlace.CleaningPlaceId,
                    Title = cleaningPlace.Title
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public List<CleaningPlace> GetAll()
        {

            var cleaningPlace = _context.CleaningPlaces.ToList().Where(c => c.IsActive == true);

            return cleaningPlace.ToList();
        }

        public List<ComboBoxOutPutDto> GetComboBoxOutPut()
        {
            try
            {
                var cleaningPlaces = _context.CleaningPlaces.ToList().Where(c => c.IsActive == true);
                var comboBoxItems = new List<ComboBoxOutPutDto>();

                foreach (var item in cleaningPlaces)
                {
                    var data = new ComboBoxOutPutDto
                    {
                        Id = item.CleaningPlaceId,
                        Title = item.Title
                    };
                    comboBoxItems.Add(data);
                }

                return comboBoxItems.ToList();
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }            
        }

        public JsonResultMessage Save(CleaningPlaceInputDto input)
        {
            var creaningPlace = new CleaningPlace
            {
                Title = input.Title,
                CreationDate = DateTime.UtcNow,
                IsActive = input.IsActive,
                CreatorUserId = input.CreateUserId
            };

            try
            {
                _context.Add(creaningPlace);
                _context.SaveChanges();

                return new JsonResultMessage
                {
                    Title = "Operacion exitosa",
                    Detail = $"El inmueble no. {creaningPlace.CleaningPlaceId} se ha agragado satisfactoriamente.",
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new JsonResultMessage
                {
                    Title = "Operacion fallida",
                    Detail = $"Error guardando el inmueble: {ex.InnerException.Message}",
                    IsSuccess = false
                };
            }
        }

        public JsonResultMessage Update(CleaningPlaceInputDto input)
        {
            throw new NotImplementedException();
        }
    }
}
