using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Utils.Wallet.Models;
using Newtonsoft.Json;

namespace BITCORNService.Utils.Wallet
{
    /// <summary>
    /// Handles communication with the wallet server
    /// </summary>
    public class WalletClient : IDisposable
    {
        /// <summary>
        /// internal httpclient
        /// </summary>
        private HttpClient _httpClient = null;
        /// <summary>
        /// api endpoint
        /// </summary>
        private Uri _targetUri = null;

        /// <summary>
        /// Handles communication with the wallet server
        /// </summary>
        /// <param name="targetUri">api endpoint to use</param>
        /// <param name="contentType">request content type</param>
        /// <param name="accessToken">api call access token</param>
        /// <param name="messageHandler">optional message handler to easily extend the httpclient</param>
        public WalletClient(string targetUrl,
            string accessToken,
            HttpMessageHandler messageHandler = null) : this(new Uri(targetUrl), accessToken, messageHandler)
        {

        }

        /// <summary>
        /// Handles communication with the wallet server
        /// </summary>
        /// <param name="targetUri">api endpoint to use</param>
        /// <param name="contentType">request content type</param>
        /// <param name="accessToken">api call access token</param>
        /// <param name="messageHandler">optional message handler to easily extend the httpclient</param>
        public WalletClient(Uri targetUri,
        string accessToken,
        HttpMessageHandler messageHandler = null)
        {

            this._targetUri = targetUri;

            if (messageHandler != null)
            {
                this._httpClient = new HttpClient(messageHandler, true);
            }
            else
            {
                this._httpClient = new HttpClient();
            }

            this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }

        /// <summary>
        /// make direct call to the wallet server
        /// </summary>
        /// <param name="method">wallet method</param>
        /// <param name="parameters">parameters required by the wallet method</param>
        /// <returns>HttpResponseMessage returned by the wallet server</returns>
        protected async Task<HttpResponseMessage> MakeInternalRequestAsync(
            string method,
            object[] parameters)
        {

            if (!string.IsNullOrEmpty(method))
            {
                //this is the format the wallet expects the request body to be in
                var json = JsonConvert.SerializeObject(new
                {
                    method = method,
                    @params = parameters
                });

                var content = new StringContent(json, Encoding.UTF8);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                return await this._httpClient.PostAsync(_targetUri, content);
            }
            else
            {
                //dont make request to the server if method name is not defined.
                var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                response.Content = new StringContent("Method name expected.");

                return response;
            }

        }

        /// <summary>
        /// implement IWalletClient interface
        /// </summary>
        /// <param name="method">wallet method</param>
        /// <param name="parameters">parameters required by the wallet method</param>
        /// <returns>Deserialized wallet response</returns>
        public async Task<WalletResponse> MakeRequestAsync(string method, params object[] parameters)
        {
            var response = await MakeInternalRequestAsync(method, parameters);

            WalletResponse data = new WalletResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {

                string json = await response.Content.ReadAsStringAsync();
                data.FromJson(json);

            }
            else
            {
                var error = CreateHttpErrorResponse(response);
                data.Error = error;
            }

            data.StatusCode = response.StatusCode;
            return data;

        }

        /// <summary>
        /// Creates wallet error object with HTTP_ERROR flag indicating that the error was in the request
        /// </summary>
        /// <returns></returns>
        private WalletError CreateHttpErrorResponse(HttpResponseMessage message)
        {
            var error = new WalletError();
            error.Code = WalletErrorCodes.HTTP_ERROR;
            error.Message = message.StatusCode.ToString();
            return error;
        }

        /// <summary>
        /// dispose internal http client
        /// </summary>
        public void Dispose()
        {
            if (this._httpClient != null)
            {
                this._httpClient.Dispose();
                this._httpClient = null;
            }
        }
    }
}
