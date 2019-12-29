using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Models
{
    public struct ColumnValuePair
    {
        public string Column { get; set; }
        public object Value { get; set; }
        public ColumnValuePair(string column,object value)
        {
            this.Column = column;
            this.Value = value;
        }
    }
}
