using System;
using System.Collections.Generic;
using System.Linq;
using JempSoft.Applications;
using JempSoft.Core.Data;
using JempSoft.Core.Models;
using JempSoft.Core.POCOs;
using netcore.Services.ServiceTypes;

namespace netcore.Services.Services.Types
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

        public ServiceType Get(int? id)
        {
            try
            {
                var serviceType = _context.ServiceTypes.FirstOrDefault(s => s.ServiceTypeId == id);

                return new ServiceType
                {
                    ServiceTypeId = serviceType.ServiceTypeId,
                    Title = serviceType.Title,
                    Cost = serviceType.Cost,
                    Price = serviceType.Price,
                    CleaningPlaceRooms = serviceType.CleaningPlaceRooms
                };
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<ServiceType> GetAll()
        {
            throw new NotImplementedException();
        }

        public JsonResultMessage Save(ServiceTypeInputDto input)
        {
            throw new NotImplementedException();
        }

        public JsonResultMessage Update(ServiceTypeInputDto input)
        {
            throw new NotImplementedException();
        }
    }
}
