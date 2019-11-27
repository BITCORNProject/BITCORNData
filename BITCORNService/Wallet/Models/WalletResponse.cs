using System;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json.Linq;
namespace BITCORNService.Wallet.Models
{
    /// <summary>
    /// Deserialized response from the wallet server
    /// </summary>
    public class WalletResponse
    {
  
        /// <summary>
        /// Raw Data returned by the wallet
        /// </summary>
        public string Result { get; set; }
        /// <summary>
        /// Error returned by the wallet
        /// </summary>
        public WalletError Error { get; set; }

        public int Id { get; set; }
        /// <summary>
        /// response status code from the Http message
        /// </summary>
        public HttpStatusCode StatusCode { get; internal set; }

        public WalletResponse()
        {

        }

        /// <summary>
        /// Parses the wallet server response
        /// </summary>
        public void FromJson(string json)
        {
            try
            {
                /*
                  input Json format:
                  {
                     "result" : "data-object",
                     "error"  : "error-object",
                     "id"     : "id"
                  }
                 */
                dynamic jsonObject = JObject.Parse(json);

                Result = jsonObject.result.ToString();

                string errorJson = jsonObject.error.ToString();
                if (!string.IsNullOrEmpty(errorJson))
                {


                    dynamic errorObject = JObject.Parse(errorJson);

                    Error = new WalletError();
                    Error.Code = errorObject.code;
                    Error.Message = errorObject.message;
                }

                this.Id = jsonObject.id;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

}
