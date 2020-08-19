using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Applications.Book.Dto
{
    public class AvaliableMaidOutputDto
    {
        public DateTime Day { get; set; }
        
        public int QtyAvaliable { get; set; }

        public string Hour { get; set; }
    }

    public class AvaliableMaidMonthOutputDto
    {
        public AvaliableMaidMonthOutputDto()
        {
            AvaliableByDay = new List<AvaliableMaidOutputDto>();
        }

        public List<AvaliableMaidOutputDto> AvaliableByDay { get; set; }
    }
}
