using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Paystation.Models
{
    public class Transaction
    {
        public string TransactionID { get; set; } // Used to poll the transaction status to see when it has completed.
        public string MerchantSession { get; set; }
        public string DigitalOrderURL { get; set; } // The URL for the iframe where the credit card details are entered on the checkout page.
        public int Amount { get; set; } // in cents
        public string TransactionTime { get; set; }
        public int ErrorCode { get; set; } // Negative means unset. 0 means no error. Positive means there is an error.
        public string ErrorMessage { get; set; }

        public Transaction()
        {
            ErrorCode = -1;
        }
    }
}