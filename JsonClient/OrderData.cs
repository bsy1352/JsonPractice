using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonClient
{
    public class OrderData
    {
        public int OrderNum { get; set; }

        public string OrderDetail { get; set; }

        public string OrderState { get; set; }

        public string OrderDate { get; set; }
    }
}
