using JempSoft.Core.Data;
using JempSoft.Core.Models;
using JempSoft.Core.POCOs;
using netcore.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JempSoft.Applications.Services
{
    public class ServiceTypeServices : IServiceTypeServices
    {

        private readonly JempSoftDbContext _context;

        public ServiceTypeServices(JempSoftDbContext context)
        {
            _context = context;
        }


        public JsonResultMessage Delete(int? id)
        {
            throw new NotImplementedException();
        }

        public ServiceTypeOutputDto Get(int? id)
        {
            try
            {
                var serviceType = _context.ServiceTypes.FirstOrDefault(s => s.ServiceTypeId == id);

                return new ServiceTypeOutputDto
                {
                    ServiceTypeId = serviceType.ServiceTypeId,
                    Title = serviceType.Title,
                    FullDescription = serviceType.FullDescription,
                    Cost = serviceType.Cost,
                    Price = serviceType.Price,
                    IsActive = serviceType.IsActive,
                    CreatorUserName = ""
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public List<ServiceType> GetAll()
        {
            var result = _context.ServiceTypes.ToList();
            return result;
        }

        public List<ComboBoxOutPutDto> GetComboBoxByCleaningPlaceRoomId(int? id)
        {
            try
            {
                if (!id.HasValue)
                {
                    return null;
                }

                var serviceTypes = new List<ServiceType>();

                var cleaningPlaceRoomServiceTypes = _context.CleaningPlaceRoomServiceTypes.ToList().Where(c => c.CleaningPlaceRoomId == id);

                var comboBoxItems = new List<ComboBoxOutPutDto>();

                foreach (var item in cleaningPlaceRoomServiceTypes)
                {
                    if(item.CleaningPlaceRoomId == id)
                    {
                        var serviceType = _context.ServiceTypes.FirstOrDefault(c => c.ServiceTypeId == item.ServiceTypeId);

                        var data = new ComboBoxOutPutDto
                        {
                            Id = serviceType.ServiceTypeId,
                            Title = serviceType.FullDescription
                        };
                        comboBoxItems.Add(data);
                    }
                }

                var result = comboBoxItems.OrderBy(c => c.Title).ToList();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public JsonResultMessage Save(ServiceTypeInputDto input)
        {
            var serviceType = new ServiceType
            {
                Title = input.Title,
                Cost = input.Cost,
                Price = input.Price,
                IsActive = input.IsActive,
                CreatorUserId = input.CreatorUserId
            };

            try
            {
                _context.Add(serviceType);
                _context.SaveChanges();

                return new JsonResultMessage
                {
                    Title = "Operacion exitosa",
                    Detail = $"El tipo de servicio no. {serviceType.ServiceTypeId} se ha agragado satisfactoriamente.",
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new JsonResultMessage
                {
                    Title = "Operacion fallida",
                    Detail = $"Error guardando el tipo de servicio: {ex.InnerException.Message}",
                    IsSuccess = false
                };
            }
        }

        public JsonResultMessage Update(ServiceTypeInputDto input)
        {
            throw new NotImplementedException();
        }
    }
}
