using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace BITCORNServiceTests.Models
{
    public class TxTestResult
    {
        public decimal Amount { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public decimal FromStartBalance { get; set; }
        public decimal ToStartBalance { get; set; }
        public decimal FromEndBalance { get; set; }
        public decimal ToEndBalance { get; set; }
        public ActionResult<TxReceipt[]> ResponseObject { get; internal set; }
    }
}
