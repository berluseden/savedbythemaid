using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Applications
{
    public class ComboBoxOutPutDto
    {
        public int Id { get; set; }
        
        public string StringId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Price for service types (optional)
        /// </summary>
        public decimal Price { get; set; } = 0;
    }
}
