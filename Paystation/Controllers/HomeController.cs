using System.Web.Mvc;
using System.Xml;
using System.IO;
using Paystation.Helpers;
using Paystation.Models;

namespace Paystation.Controllers
{
    public class HomeController : Controller
    {
        // This is here to make the sample code work. Normally this data would go in the database...
        private Transaction LastTransaction;

        // GET: Home
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(float amount)
        {
            var payment = new Payment();
            payment.Amount = (int)(amount * 100);
            // payment.OtherData = "&var=value&anything=something";
            payment.MerchantReference = "ref01";
            var response = payment.GetResponse();

            if (response.ErrorCode > 0)
            {
                // handle error however you plan to.
            }

            return View(response);
        }

        /// <summary>
        /// Used by the browser to poll transaction status so it knows when to close the iframe and whether the transaction was successful.
        /// </summary>
        /// <returns>JSON reponse</returns>
        [HttpPost]
        public ActionResult Ajax()
        {
            var transactionId = Request["transaction_id"];
            Transaction transaction;

            if (LastTransaction != null && LastTransaction.ErrorCode > -1 && LastTransaction.TransactionID.Equals(transactionId))
            {
                transaction = LastTransaction;
            }
            else
            {
                transaction = Lookup.GetTransaction(transactionId);

                if (LastTransaction == null || transaction.TransactionID.Equals(transactionId))
                {
                    LastTransaction = transaction;
                }
            }

            return Json(new
            {
                transactionId = transaction.TransactionID,
                amount = (float)transaction.Amount / 100,
                transactionTime = transaction.TransactionTime,
                merchantSession = transaction.MerchantSession,
                errorCode = transaction.ErrorCode,
                errorMessage = transaction.ErrorMessage
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// This is for the Paystation POST response received when a transaction is completed.
        /// </summary>
        /// <returns>Empty response</returns>
        [HttpPost]
        public ActionResult Callback()
        {
            var transaction = new Transaction();
            var xmlStr = new StreamReader(Request.InputStream).ReadToEnd();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlStr);

            var xml = xmlDoc.SelectSingleNode("PaystationPaymentVerification");
            transaction.TransactionID = xml.SelectSingleNode("ti").InnerXml;
            int amount;
            int.TryParse(xml.SelectSingleNode("PurchaseAmount").InnerXml, out amount); // amount is in cents
            transaction.Amount = amount;
            transaction.TransactionTime = xml.SelectSingleNode("TransactionTime").InnerXml;
            transaction.MerchantSession = xml.SelectSingleNode("MerchantSession").InnerXml;
            transaction.ErrorMessage = xml.SelectSingleNode("em").InnerXml;
            var errorCodeStr = xml.SelectSingleNode("ec").InnerXml;
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

            LastTransaction = transaction;

            return new EmptyResult();
        }
    }
}