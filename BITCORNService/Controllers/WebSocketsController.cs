using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BITCORNService.Utils.Auth;
using BITCORNService.WebSockets.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BITCORNService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WebSocketsController : ControllerBase
    {
        public static async Task TryBroadcastToBitcornhub(string type, object data)
        {
            try
            {
                if (BitcornhubWebsocket != null)
                {
                    if (!BitcornhubWebsocket.CloseStatus.HasValue)
                    {

                        var serverMsg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                        {
                            type,
                            payload = data
                        }));

                        await BitcornhubWebsocket.SendAsync(new ArraySegment<byte>(serverMsg),
                            WebSocketMessageType.Text,
                            true, CancellationToken.None);


                    }
                }
            }
            catch(Exception e)
            {
                
            }
        } 
        public static WebSocket BitcornhubWebsocket { get; set; }

        [Authorize(Policy = AuthScopes.SendTransaction)]

        [HttpGet("/bitcornhub")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
              
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await SocketReceiverHandler<BitcornhubSocketReceiver>(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }
        
        private async Task SocketReceiverHandler<T>(WebSocket webSocket) where T : SocketReceiver, new()
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            var receiver = new T();
            receiver.Start(webSocket);
            while (!result.CloseStatus.HasValue)
            {
                var receiveArr = new ArraySegment<byte>(buffer);
                //var serverMsg = Encoding.UTF8.GetBytes($"Server: Hello. You said: {Encoding.UTF8.GetString(buffer)}");
                //await webSocket.SendAsync(new ArraySegment<byte>(serverMsg), result.MessageType, result.EndOfMessage, CancellationToken.None);
                
                result = await webSocket.ReceiveAsync(receiveArr, CancellationToken.None);
                if (result.Count != 0 || result.CloseStatus == WebSocketCloseStatus.Empty)
                {
                    string message = Encoding.UTF8.GetString(receiveArr.Array,
                         receiveArr.Offset, result.Count);

                    
                    receiver.Process(webSocket, message);
                }

                
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            receiver.OnClose();
        }
    }

    
}

