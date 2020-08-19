using JempSoft.Applications.Book.Dto;
using JempSoft.Core.Data;
using JempSoft.Core.Models;
using JempSoft.Core.Models.Services;
using JempSoft.Core.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JempSoft.Applications
{
    public class BookingService : IBookingService
    {
        private readonly JempSoftDbContext _context;

        private readonly IUnitOfWork _unitOfWork;

        public BookingService(JempSoftDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<CartItemOutPutDto> Book(BookInputDto input)
        {
            if (input.CleaningPlaceId != 0 && input.CleaningPlaceId != 0 && input.ServiceTypeId != 0)
            {

                var getItem = await this.GetCartItem(input);

                if (getItem == null)
                {
                    try
                    {
                        var cartItem = new CartItem
                        {
                            CleaningPlaceId = input.CleaningPlaceId,
                            CleaningPlaceRoomId = input.CleaningPlaceRoomId,
                            ServiceTypeId = input.ServiceTypeId
                        };

                        await _context.AddAsync(cartItem);
                        await _context.SaveChangesAsync();

                        return await this.GetCartItem(input);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }

                return getItem;
            }

            return null;
        }

        public AvaliableMaidOutputDto GetAvaliableMaids(DateTime day)
        {
            var avaliables = _context.AvaliableMaids.ToList().Where(d => d.DayOfAvaliability.ToShortDateString() == day.ToShortDateString());

            var avaliableMaid = new AvaliableMaidOutputDto
            {
                Day = day,
                QtyAvaliable = avaliables.Count()
            };

            return avaliableMaid;

        }

        public AvaliableMaidMonthOutputDto GetAvaliableMaidThisMonth(DateTime day)
        {
            var avaliables = _context.AvaliableMaids.ToList().Where(d => d.DayOfAvaliability.Month == day.Month);

            var result = new AvaliableMaidMonthOutputDto();


            foreach (var item in avaliables)
            {
                var avaliableMaid = new AvaliableMaidOutputDto
                {
                    Day = item.DayOfAvaliability,
                    QtyAvaliable = item.AvaliableCount,
                    Hour = item.DayOfAvaliability.Hour.ToString()
                };

                result.AvaliableByDay.Add(avaliableMaid);
            }

            return result;
        }

        public int GetAvaliablesMaidsByDay(DateTime day)
        {
            try
            {

                var result = _context.AvaliableMaids.FirstOrDefault(d => d.DayOfAvaliability.Day == day.Day && d.DayOfAvaliability.Month == day.Month && d.DayOfAvaliability.Year == day.Year);
                if (result == null)
                {
                    return 0;
                }
                return (result.AvaliableCount - result.ServiceCount);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<CartItemOutPutDto> GetCartItem(BookInputDto input)
        {
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.CleaningPlaceId.Equals(input.CleaningPlaceId) && c.CleaningPlaceRoomId.Equals(input.CleaningPlaceRoomId) && c.ServiceTypeId.Equals(input.ServiceTypeId));


            if (item != null)
            {

                var placeTitle = _context.CleaningPlaces.FirstOrDefault(c => c.CleaningPlaceId == input.CleaningPlaceId).Title;
                var placeRoomTitle = _context.CleaningPlaceRooms.FirstOrDefault(c => c.CleaningPlaceRoomId == input.CleaningPlaceRoomId).Title;
                var serviceType = _context.ServiceTypes.FirstOrDefault(c => c.ServiceTypeId == input.ServiceTypeId);


                return new CartItemOutPutDto
                {
                    CartItemId = item.CartItemId,
                    CleaningPlaceId = item.CleaningPlaceId,
                    CleaningPlaceTitle = placeTitle,
                    CleaningPlaceRoomId = item.CleaningPlaceRoomId,
                    CleaningPlaceRoomTitle = placeRoomTitle,
                    ServiceTypeId = item.ServiceTypeId,
                    ServiceTypeFullDescription = serviceType.FullDescription,
                    Price = serviceType.Price
                };
            }

            return null;
        }

        public OrderToCheckOutDto ProcessToCheckOut(int id)
        {
            if (id != 0)
            {
                try
                {

                    var cartItem = _context.CartItems.FirstOrDefault(c => c.CartItemId == id);
                    var cleaningPlace = _context.CleaningPlaces.FirstOrDefault(c => c.CleaningPlaceId == cartItem.CleaningPlaceId);
                    var cleaningPlaceRoom = _context.CleaningPlaceRooms.FirstOrDefault(c => c.CleaningPlaceRoomId == cartItem.CleaningPlaceRoomId);
                    var serviceType = _context.ServiceTypes.FirstOrDefault(c => c.ServiceTypeId == cartItem.ServiceTypeId);

                    var additionalServiceList = _context.AdditionalServiceTypes.ToList();

                    var result = new OrderToCheckOutDto
                    {
                        CartItemId = cartItem.CartItemId,
                        CleaningPlace_Title = cleaningPlace.Title,
                        CleaningPlaceRoom_Title = cleaningPlaceRoom.Title,
                        ServiceType_Title = serviceType.Title,
                        ServiceType_Price = Convert.ToDecimal(serviceType.Price)
                    };


                    var additionalServiteTypeOutPut = new List<AddtionalServiceTypeListOutputDto>();

                    foreach (var item in additionalServiceList)
                    {
                        var additionalService = new AddtionalServiceTypeListOutputDto
                        {
                            AdditionalServiceTypeId = item.AdditionalServiceTypeId,
                            Title = item.Title,
                            Price = Convert.ToDecimal(item.Price)
                        };

                        result.AdditionalServiceTypes.Add(additionalService);
                    }


                    return result;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

            }

            return null;
        }

        public bool AddToCart(ServiceOrderInputDto serviceOrder, List<int> aditionalServices, ServiceContactInfoInputDto contactInfo, out long serviceOrderId)
        {
            serviceOrderId = 0;

            if (contactInfo == null || serviceOrder == null)
            {
                return false;
            }

            try
            {
                _context.Database.BeginTransaction();

                var itemAmount = 0.0;
                var cartItem = _context.CartItems.FirstOrDefault(c => c.CartItemId == serviceOrder.CartItemId);

                if (cartItem != null)
                {
                    var serviceOnCartItem = _context.ServiceTypes.FirstOrDefault(c => c.ServiceTypeId == cartItem.ServiceTypeId);
                    if (serviceOnCartItem != null)
                    {
                        itemAmount = serviceOnCartItem.Price;
                    }
                }

                decimal tax = 0;

                serviceOrder.Amount = Convert.ToDecimal(GetAdditionalServicesAmount(aditionalServices) + itemAmount);
                serviceOrder.TotalAmount = serviceOrder.Amount + tax;

                var serviceContactInfo = new ServiceOrderContactInfo
                {
                    Name = contactInfo.Name,
                    Email = contactInfo.Email,
                    Phone = contactInfo.Phone,
                    Address = contactInfo.Address,
                    AdditionalServiceInfo = contactInfo.AdditionalServiceInfo
                };

                _context.Add(serviceContactInfo);
                _context.SaveChanges();

                var service = new ServiceOrder
                {
                    CartItemId = serviceOrder.CartItemId,
                    Day = serviceOrder.Day,
                    Month = serviceOrder.Month,
                    Year = serviceOrder.Year,
                    Hour = serviceOrder.Hour,
                    Minute = serviceOrder.Minute,
                    IsPayed = serviceOrder.IsPayed,
                    IsActive = serviceOrder.IsActive,
                    IsComplete = serviceOrder.IsComplete,
                    Amount = serviceOrder.Amount,
                    Tax = serviceOrder.Tax,
                    TotalAmount = serviceOrder.TotalAmount,
                    ServiceContactInfoId = serviceContactInfo.Id
                };

                _context.Add(service);
                _context.SaveChanges();

                foreach (var additionalServiceId in aditionalServices)
                {

                    var additionalService = new ServiceOrderAdditionalService
                    {
                        AdditionalServiceId = additionalServiceId,
                        ServiceOrderId = service.Id
                    };

                    _context.Add(additionalService);
                    _context.SaveChanges();
                }
                
                _context.Database.CommitTransaction();
                serviceOrderId = service.Id;

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);

            }
        }

        public double GetAdditionalServicesAmount(List<int> additionalServices)
        {
            var amount = 0.0;

            foreach (var additionalServiceId in additionalServices)
            {
                var additionalService = _context.AdditionalServiceTypes.Find(additionalServiceId);
                if (additionalService != null)
                {
                    amount = +additionalService.Price;
                }
            }
            return amount;
        }

        public ServiceItemsOnCartDto GetCartServiceByUserName(string userName)
        {
            var itemsOnCart = new ServiceItemsOnCartDto();
            
            var items = (
                from s in _context.ServiceOrders.Where(s => !s.IsComplete && s.IsActive && !s.IsPayed)
                                                .Include(d => d.AdditionalServices)
                                                .Include(b => b.CartItem)                                                
                                                .Include(a => a.ServiceContactInfo)                                                
                                                .Where(c => c.ServiceContactInfo.Email == userName)
                
                                                join cp in _context.CleaningPlaces on s.CartItem.CleaningPlaceId equals cp.CleaningPlaceId
                                                join cpr in _context.CleaningPlaceRooms on s.CartItem.CleaningPlaceRoomId equals cpr.CleaningPlaceRoomId                                                
                                                join st in _context.ServiceTypes on s.CartItem.ServiceTypeId equals st.ServiceTypeId
                where s.ServiceContactInfo.Email == userName
                select new
                {
                    ServiceId = s.Id,
                    CartItemId = s.CartItemId,
                    CleaningPlace = cp.Title,
                    CleaningPlaceRoom = cpr.Title,
                    ServiceType = st.Title,
                    ServiceTypePrice = st.Price,
                    OrderAmount = s.Amount,
                    OrderTax = s.Tax,
                    OrderTotalAmount = s.TotalAmount,
                    IsActive = s.IsActive
                });


            foreach (var item in items)
            {
                var itemOnCart = new ServicesOnCartDto
                {
                    ServiceId = item.ServiceId,
                    CartItemId = item.CartItemId,
                    CleaningPlace = item.CleaningPlace,
                    CleaningPlaceRoom = item.CleaningPlaceRoom,
                    ServiceType = item.ServiceType,
                    ServiceTypePrice = Convert.ToDecimal(item.ServiceTypePrice),
                    OrderAmount = item.OrderAmount,
                    OrderTax = Convert.ToDouble(item.OrderTax),
                    OrderTotalAmount = item.OrderTotalAmount
                };
                itemsOnCart.Footer.SubTotal += itemOnCart.ServiceTypePrice;

                itemsOnCart.Services.Add(itemOnCart);

                foreach (var additionalService in _context.ServiceOrderAdditionalServices.Where(s => s.ServiceOrderId == item.ServiceId))
                {
                    var additionalServiceData = _context.AdditionalServiceTypes.FirstOrDefault(c => c.AdditionalServiceTypeId == additionalService.AdditionalServiceId);


                    var additional = new AdditionalServiceType
                    {
                        AdditionalServiceTypeId = additionalServiceData.AdditionalServiceTypeId,
                        Title = additionalServiceData.Title,
                        Price = additionalServiceData.Price
                    };

                    itemOnCart.AdditionalServicesOnCart.Add(additional);
                    itemsOnCart.Footer.SubItemsTotal += Convert.ToDecimal(additional.Price);
                }

            }

            itemsOnCart.Footer.SubTotal = itemsOnCart.Footer.SubTotal + itemsOnCart.Footer.SubItemsTotal;
            itemsOnCart.Footer.Total = itemsOnCart.Footer.SubTotal + itemsOnCart.Footer.Tax; 

            return itemsOnCart;

        }

        public ServiceOrder RemoveItemOnCart(long id, out string resultMessage)
        {

            try
            {
                var result = _context.ServiceOrders.FirstOrDefault(c => c.Id == id);

                result.IsActive = false;
                _context.Entry(result).State = EntityState.Modified;
                _context.SaveChanges();
                resultMessage = "";

                return result;
            }
            catch (Exception ex)
            {
                resultMessage = ex.InnerException.Message;
                return null;
            }
        }
    }
}
