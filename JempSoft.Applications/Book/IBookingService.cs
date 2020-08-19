using JempSoft.Applications.Book.Dto;
using JempSoft.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JempSoft.Applications
{
    public interface IBookingService
    {
        Task<CartItemOutPutDto> Book(BookInputDto input);

        OrderToCheckOutDto ProcessToCheckOut(int id);

        AvaliableMaidOutputDto GetAvaliableMaids(DateTime day);

        AvaliableMaidMonthOutputDto GetAvaliableMaidThisMonth(DateTime day);

        int GetAvaliablesMaidsByDay(DateTime day);

        bool AddToCart(ServiceOrderInputDto serviceOrder, List<int> aditionalServices, ServiceContactInfoInputDto contactInfo, out long serviceOrderId);
        ServiceItemsOnCartDto GetCartServiceByUserName(string userName);

        ServiceOrder RemoveItemOnCart(long id, out string resultMessage);
    }
}