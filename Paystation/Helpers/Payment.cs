using System;
using System.Web.Configuration;
using System.Xml;
using System.IO;
using Paystation.Models;
using System.Net;

namespace Paystation.Helpers
{
    /// <summary>
    /// Used for creating a new transaction.
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// The payment amount in cents.
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// Merchant Reference is visible from Paystation Admin. Can be up to 64 char. Optional.
        /// Must be URL safe (URL encode if necessary).
        /// </summary>
        public string MerchantReference { get; set; }

        /// <summary>
        /// Any additional querystring variables set will be returned with the response from Paystation.
        /// Must be URL safe (URL encode the values if necessary).
        /// </summary>
        public string OtherData { get; set; }

        /// <summary>
        /// Generates the Merchant Session ID for this transaction.
        /// The Merchant Session ID consists of the "site" key from the Web.config, a string representing the current 
        /// date and time to the nearest millisecond and lastly the session ID.
        /// </summary>
        /// <returns>The Merchant Session ID</returns>
        private string GetMerchantSessionID()
        {
            var now = DateTime.Now;
            var dateTime = now.Year.ToString() + now.Month.ToString() + now.Day.ToString() + now.Hour.ToString()
                + now.Minute.ToString() + now.Second.ToString() + now.Millisecond.ToString();
            return WebConfigurationManager.AppSettings["site"] + dateTime + Guid.NewGuid().ToString().Substring(0, 8);
        }

        /// <summary>
        /// Sends an Initiation request to the Paystation payment URL, reads the XML response and redirects to the URL specified in the response.
        /// </summary>
        /// <returns>Response data from the XML response</returns>
        public Transaction GetResponse()
        {
            var xmlDocument = new XmlDocument();
            var response = new Transaction();

            try
            {
                string paystationID = WebConfigurationManager.AppSettings["paystationID"];
                string gatewayID = WebConfigurationManager.AppSettings["gatewayID"];
                var testModeEnabled = WebConfigurationManager.AppSettings["testModeEnabled"].ToLower().Equals("true");

                var queryString = @"paystation=_empty" +
                    "&pstn_am=" + Amount + "&pstn_pi=" + paystationID +
                    "&pstn_gi=" + gatewayID + "&pstn_ms=" + GetMerchantSessionID() +
                    "&pstn_nr=t&pstn_mr=" + MerchantReference + OtherData;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                xmlDocument.Load(new StringReader(Request.Post("https://www.paystation.co.nz/direct/paystation.dll", queryString)));
                
                if (xmlDocument.SelectSingleNode("InitiationRequestResponse/DigitalOrder") != null)
                {
                    var psResponse = xmlDocument.SelectSingleNode("InitiationRequestResponse");
                    response.DigitalOrderURL = psResponse.SelectSingleNode("DigitalOrder").InnerXml;
                    response.TransactionID = psResponse.SelectSingleNode("PaystationTransactionID").InnerXml;
                }
                else if (xmlDocument.SelectSingleNode("response/PaystationErrorCode") != null)
                {
                    var psResponse = xmlDocument.SelectSingleNode("response");
                    int errorCode;
                    int.TryParse(psResponse.SelectSingleNode("PaystationErrorCode").InnerXml, out errorCode);
                    response.ErrorCode = errorCode;
                    response.ErrorMessage = "(" + errorCode + ") Failed to create new transaction.";

                    if (testModeEnabled)
                    {
                        response.ErrorMessage = "Cannot create transaction. Please check that the correct Paystation ID is set in your config. Error Message: "
                            + psResponse.SelectSingleNode("PaystationErrorMessage").InnerXml;
                    }
                }
                else
                {
                    response.ErrorCode = 1;
                    response.ErrorMessage = "Unknown error occurred while creating a new transaction.";
                }
            }
            catch (System.Net.WebException e)
            {
                Console.WriteLine($"The file was not found: '{e}'");
                response.ErrorMessage = "Unable to connect to Paystation.";
                response.ErrorCode = 1;
            }
            catch (System.Xml.XmlException)
            {
                response.ErrorMessage = "The XML file is not well formed.";
                response.ErrorCode = 1;
            }

            return response;
        }
    }
}