using JempSoft.Core.Data;
using JempSoft.Core.Models;
using JempSoft.Core.POCOs;
using netcore.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JempSoft.Applications.Services
{
    public class CleaningPlaceRoomServices : ICleaningPlaceRoomServices
    {

        private readonly JempSoftDbContext _context;

        public CleaningPlaceRoomServices(JempSoftDbContext context)
        {
            _context = context;
        }


        public JsonResultMessage Delete(int? id)
        {
            throw new NotImplementedException();
        }

        public CleaningPlaceRoom Get(int? id)
        {
            try
            {
                var cleaningPlaceRoom = _context.CleaningPlaceRooms.FirstOrDefault(c => c.CleaningPlaceRoomId == id);

                return new CleaningPlaceRoom
                {
                    CleaningPlaceRoomId = cleaningPlaceRoom.CleaningPlaceRoomId,
                    Title = cleaningPlaceRoom.Title,
                    IsActive = cleaningPlaceRoom.IsActive,
                    CreationDate = cleaningPlaceRoom.CreationDate,
                    CreatorUserId = cleaningPlaceRoom.CreatorUserId
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public List<CleaningPlaceRoom> GetAll()
        {
            throw new NotImplementedException();
        }

        public List<ComboBoxOutPutDto> GetComboBoxOutPut()
        {
            try
            {
                var cleaningPlaceRooms = _context.CleaningPlaceRooms.ToList().Where(c => c.IsActive == true);
                var comboBoxItems = new List<ComboBoxOutPutDto>();

                foreach (var item in cleaningPlaceRooms)
                {
                    var data = new ComboBoxOutPutDto
                    {
                        Id = item.CleaningPlaceRoomId,
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

        public List<ComboBoxOutPutDto> GetComboBoxByCleaningPlaceId(int? id)
        {
            try
            {
                if (!id.HasValue)
                {
                    return null;
                }

                var cleaningPlaceRooms = new List<CleaningPlaceRoom>();

                var cleaningPlacePlaceRooms = _context.CleaningPlaceCleaningPlaceRooms.ToList().Where(c => c.CleaningPlaceId == id);

                var comboBoxItems = new List<ComboBoxOutPutDto>();

                foreach (var item in cleaningPlacePlaceRooms)
                {
                    var cleaningPlaceRoom = _context.CleaningPlaceRooms.FirstOrDefault(c => c.CleaningPlaceRoomId == item.CleaningPlaceRoomId);

                    var data = new ComboBoxOutPutDto
                    {
                        Id = cleaningPlaceRoom.CleaningPlaceRoomId,
                        Title = cleaningPlaceRoom.Title
                    };
                    comboBoxItems.Add(data);
                }

                var result = comboBoxItems.OrderBy(c => c.Title).ToList();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public JsonResultMessage Save(CleaningPlaceRoomInputDto input)
        {
            var creaningPlaceRoom = new CleaningPlaceRoom
            {
                Title = input.Title,
                CreationDate = DateTime.UtcNow,
                IsActive = input.IsActive,
                CreatorUserId = input.CreateUserId
            };

            try
            {
                _context.Add(creaningPlaceRoom);
                _context.SaveChanges();

                return new JsonResultMessage
                {
                    Title = "Operacion exitosa",
                    Detail = $"La dimension del inmueble no. {creaningPlaceRoom.CleaningPlaceRoomId} se ha agragado satisfactoriamente.",
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new JsonResultMessage
                {
                    Title = "Operacion fallida",
                    Detail = $"Error guardando la dimension del inmueble: {ex.InnerException.Message}",
                    IsSuccess = false
                };
            }
        }

        public JsonResultMessage Update(CleaningPlaceRoomInputDto input)
        {
            throw new NotImplementedException();
        }

    }
}
