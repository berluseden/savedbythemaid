using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netcore.POCOs
{
    public class JsonResultMessage
    {
        public string Title { get; set; }

        public string Detail { get; set; }

        public bool IsSuccess { get; set; }
    }
}
