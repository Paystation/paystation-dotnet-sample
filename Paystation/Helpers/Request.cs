using System;
using System.Security.Cryptography;
using System.Web.Configuration;
using System.Net;
using System.IO;
using System.Text;

namespace Paystation.Helpers
{
    public class Request
    {
        /// <summary>
        /// Shared secret. Necessary for dynamic URLs and means you don't have to whitelist your server IP.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="timestamp"></param>
        /// <param name="hmacKey"></param>
        /// <returns></returns>
        private static string GenerateHMACParameters(string content)
        {
            var timestamp = ((int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
            var webserviceName = "paystation";
            var hmacKey = WebConfigurationManager.AppSettings["hmacKey"];

            var hmacsha = new HMACSHA512(Encoding.UTF8.GetBytes(hmacKey));
            var messageBytes = hmacsha.ComputeHash(Encoding.UTF8.GetBytes(timestamp + webserviceName + content));

            var hmacStr = "";
            for (int i = 0; i < messageBytes.Length; i++)
            {
                hmacStr += messageBytes[i].ToString("X2"); // hex format
            }

            return "pstn_HMACTimestamp=" + timestamp + "&pstn_HMAC=" + hmacStr;
        }

        public static string Post(string url, string body)
        {
            var hmacModeEnabled = WebConfigurationManager.AppSettings["hmacModeEnabled"].ToLower().Equals("true");
            var testModeEnabled = WebConfigurationManager.AppSettings["testModeEnabled"].ToLower().Equals("true");

            if (testModeEnabled)
            {
                url += "?pstn_tm=t";
            }

            if (hmacModeEnabled)
            {
                url += testModeEnabled ? '&' : '?';
                url += GenerateHMACParameters(body);
            }

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = body.Length;

            StreamWriter stOut = new StreamWriter(req.GetRequestStream(), System.Text.Encoding.ASCII);
            stOut.Write(body);
            stOut.Close();

            StreamReader stIn = new StreamReader(req.GetResponse().GetResponseStream());
            var strResponse = stIn.ReadToEnd();
            stIn.Close();

            return strResponse;
        }
    }
}