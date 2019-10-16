using System.Web.Configuration;
using System.Xml;
using System.IO;
using Paystation.Models;

namespace Paystation.Helpers
{
    /// <summary>
    /// Used to query Paystation's Quick Lookup API.
    /// </summary>
    public class Lookup
    {
        private static string URL = "https://payments.paystation.co.nz/lookup/";

        /// <summary>
        /// This is used to poll paystation to get the status of a transaction. When you implement this, you should ensure
        /// it has access controls on it so that users cannot get information about other people's transactions.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns>transaction with all fields set if the transaction is found. Error code will be -1 if the transaction is incomplete.</returns>
        public static Transaction GetTransaction(string transactionId)
        {
            var xml = new XmlDocument();
            var transaction = new Transaction();

            try
            {
                string paystationId = WebConfigurationManager.AppSettings["paystationID"];
                var testModeEnabled = WebConfigurationManager.AppSettings["testModeEnabled"].ToLower().Equals("true");
                var queryString = "pi=" + paystationId + "&ti=" + transactionId;

                xml.Load(new StringReader(Request.Post(Lookup.URL, queryString)));

                if (xml.SelectSingleNode("PaystationQuickLookup/LookupResponse") != null)
                {
                    var lookupResponse = xml.SelectSingleNode("PaystationQuickLookup/LookupResponse");
                    transaction.TransactionID = lookupResponse.SelectSingleNode("PaystationTransactionID").InnerXml;
                    int amount;
                    int.TryParse(lookupResponse.SelectSingleNode("PurchaseAmount").InnerXml, out amount); // amount is in cents
                    transaction.Amount = amount;
                    transaction.TransactionTime = lookupResponse.SelectSingleNode("TransactionTime").InnerXml;
                    transaction.MerchantSession = lookupResponse.SelectSingleNode("MerchantSession").InnerXml;
                    transaction.ErrorMessage = lookupResponse.SelectSingleNode("PaystationErrorMessage").InnerXml;
                    var errorCodeStr = lookupResponse.SelectSingleNode("PaystationErrorCode").InnerXml;
                    if (errorCodeStr.Equals(""))
                    {
                        transaction.ErrorCode = -1;
                    }
                    else
                    {
                        int errorCode;
                        int.TryParse(errorCodeStr, out errorCode);
                        transaction.ErrorCode = errorCode;
                    }
                }
                else if (xml.SelectSingleNode("PaystationQuickLookup/LookupStatus") != null)
                {
                    var lookupStatus = xml.SelectSingleNode("PaystationQuickLookup/LookupStatus");
                    int errorCode;
                    int.TryParse(lookupStatus.SelectSingleNode("LookupCode").InnerXml, out errorCode);
                    transaction.ErrorCode = errorCode;
                    transaction.ErrorMessage = "(" + errorCode + ") An error occurred while looking up your transaction details.";

                    if (testModeEnabled)
                    {
                        transaction.ErrorMessage = "Cannot query transaction details. Please check that the correct HMAC key is set in your config. Error Message: " 
                            + lookupStatus.SelectSingleNode("LookupMessage").InnerXml;
                    }
                }
                else
                {
                    transaction.ErrorCode = 1;
                    transaction.ErrorMessage = "Unknown error.";
                }
                
            }
            catch (System.Net.WebException)
            {
                transaction.ErrorMessage = "Unable to connect to Paystation.";
            }
            catch (System.Xml.XmlException)
            {
                transaction.ErrorMessage = "The XML file is not well formed.";
            }

            return transaction;
        }
    }
}