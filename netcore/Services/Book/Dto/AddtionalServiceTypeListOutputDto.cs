using netcore.Models;
﻿namespace netcore.Services.Book.Dto
{
    public class AddtionalServiceTypeListOutputDto
    {
        public int AdditionalServiceTypeId { get; set; }

        public string Title { get; set; }

        public decimal Price { get; set; }
    }
}