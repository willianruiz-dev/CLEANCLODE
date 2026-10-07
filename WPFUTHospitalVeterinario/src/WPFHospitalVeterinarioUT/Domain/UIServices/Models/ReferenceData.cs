using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.UIServices.Models
{
    public class ReferenceData
    {
        public string Reference { get; set; }

        public string ExtraData { get; set; }

        public decimal Value { get; set; }

        public bool IsSelected { get; set; }

    }
}
