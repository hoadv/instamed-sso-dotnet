using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace TestInstamed
{
    class Program
    {
        static void Main(string[] args)
        {
            var queryparams1 = new NameValueCollection
            {
                ["id"] = "INSTAMED.DEV.TEST",
                ["lightWeight"] = "true",
                ["responseActionType"] = "header",
                ["userID"] = "testuser",
                ["userName"] = "testuser",
                ["consumerID"] = "f7d6589e-e5a3-4178-89aa-5d9fbfa2b243",
                ["consumerFirstName"] = "john",
                ["consumerLastName"] = "smith",
                ["accountID"] = "Instamed.dev@instamed.net",
                ["ssoAlias"] = "PTPAY.DEV",
                ["securityKey"] = "A+8CE4xSnuH5nAJJ",
                ["amount"] = "10.00",
                ["RelayState"] = "https://pay.instamed.com/Form/Payments/New",
                ["isReadOnly"] = "true",
                ["populatedFieldsReadOnlyMode"] = "CustomAmount"
            };

            var tk = GetToken(queryparams1);
            Console.WriteLine(tk.RelayState);

            Console.ReadLine();
        }

        private static SSOResults GetToken(NameValueCollection queryParams)
        {
            var AcsSaml = "https://pay.instamed.com/Forms/SSO/ACS_SAML2.aspx";
            var sb = new StringBuilder();
            var first = true;
            foreach (var key in queryParams.AllKeys)
            {
                if (!first)
                {
                    sb.Append("&");
                }
                sb.Append(key + "=" + HttpUtility.UrlEncode(queryParams[key]));
                first = false;
            }

            var send = sb.ToString();

            var request = WebRequest.Create(AcsSaml);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = send.Length;
            var requestStream = request.GetRequestStream();
            var stOut = new StreamWriter(requestStream, Encoding.ASCII);
            stOut.Write(send);
            stOut.Flush();
            stOut.Close();
            var response = (HttpWebResponse)request.GetResponse();

            var results = new SSOResults
            {
                RelayState = response.Headers["relayState"]
            };

            if (!string.IsNullOrEmpty(results.RelayState))
            {
                var uri = new Uri(results.RelayState);
                var parts = HttpUtility.ParseQueryString(uri.Query);
                results.Token = parts["token"];
                results.ResponseCode = parts["respCode"];
                results.ResponseMessage = parts["respMessage"];
                results.ErrorCode = parts["errorCode"];
                results.ErrorMessage = parts["errorMessage"];

                if (results.Token == null)
                {
                    throw new Exception("Error: InstaMed token is NULL and the ResponseCode is " +
                                        results.ResponseCode + ". Verify the credentials in your Merchant Account.");
                }
            }

            return results;
        }

       
    }

    public class SSOResults
    {
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string RelayState { get; set; }
        public string Token { get; set; }
    }

    public class InstaMedResponse
    {
        public string InstaMedTransactionId { get; set; }
        public Guid TransactionId { get; set; }
        public string AuthorizationCode { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string TransactionStatus { get; set; } // in instamed guide, check once we actually get data
        public string CvvResponse { get; set; }
        public CreditCardResponseCode CalculatedResponseCode { get; set; }
        public string PartiallyApprovedAmount { get; set; }
    }

    public enum CreditCardResponseCode : int
    {
        Undefined = 0,
        Error = 1,
        Authorized = 2,
        Declined = 3,
        NotUsed = 4,
        Duplicate = 5,
        P2PeCardError = 6,
        Cancelled = 7,
        Timeout = 8
    }
}
