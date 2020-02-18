﻿using System;
using System.Collections.Generic;


namespace BITCORNService.Utils.Stats
{
    public class TxRecordOutput
    {
        public decimal Amount { get; set; }
        public List<string> Recipients { get; set; } = new List<string>();
        public DateTime Time { get; set; }
        public string Platform { get; set; }
        public string TxType { get; set; }
        public string BlockchainTxId { get; set; }
        public string CornAddy { get; set; }
        public string Action { get; set; }
    }
}