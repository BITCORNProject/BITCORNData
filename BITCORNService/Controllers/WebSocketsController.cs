using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.WebSockets.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BITCORNService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WebSocketsController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        private readonly IConfiguration _configuration;
        public WebSocketsController(BitcornContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _configuration = config;
        }
        public enum SocketBroadcastResult
        {
            Success,
            NoConnections,
            NoValidSockets,
            InvalidSocketArray,
            Failed,
        }
        public static async Task<SocketBroadcastResult> TryBroadcast(List<WebSocket> inputSockets, BitcornContext dbContext, string type, object data)
        {
            try
            {
                if (inputSockets != null)
                {
                    var sockets = inputSockets.Where(x => x != null && !x.CloseStatus.HasValue).ToArray();
                    if (sockets != null && sockets.Length > 0)
                    {

                        var serverMsg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                        {
                            type,
                            payload = data
                        }));
                        List<WebSocket> needsClosing = new List<WebSocket>();
                        int sentCount = 0;
                        foreach (var socket in sockets)
                        {
                            if (socket.State == WebSocketState.Open)
                            {
                                await socket.SendAsync(new ArraySegment<byte>(serverMsg),
                                WebSocketMessageType.Text,
                                true, CancellationToken.None);
                                sentCount++;
                            }
                            else
                            {
                                needsClosing.Add(socket);
                            }

                        }
                        if (needsClosing.Count > 0)
                        {
                            lock (inputSockets)
                            {
                                for (int i = 0; i < needsClosing.Count; i++)
                                {
                                    var socket = needsClosing[i];
                                    inputSockets.Remove(socket);
                                }
                            }
                        }

                        for (int i = 0; i < needsClosing.Count; i++)
                        {
                            var socket = needsClosing[i];
                            await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "socket not ready", CancellationToken.None);
                        }
                        if (sentCount > 0)
                        {
                            return SocketBroadcastResult.Success;
                        }
                        else
                        {
                            return SocketBroadcastResult.NoConnections;
                        }
                    }
                    else
                    {
                        return SocketBroadcastResult.NoValidSockets;
                    }
                }
                else
                {
                    return SocketBroadcastResult.InvalidSocketArray;
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(dbContext, e, "error sending to bitcornhub socket");
            }

            return SocketBroadcastResult.Failed;
        }

        public static async Task TryBroadcastToBitcornhub(BitcornContext dbContext, string type, object data)
        {
            await TryBroadcast(BitcornhubWebsocket, dbContext, type, data);
        }

        public static async Task<SocketBroadcastResult> TryBroadcastToBitcornfarms(BitcornContext dbContext, string type, object data)
        {
            return await TryBroadcast(BitcornFarmsWebSocket, dbContext, type, data);
        }


        public static void GetSocketArgs<T>(string v)
        {

        }

        public static List<WebSocket> BitcornFarmsWebSocket { get; set; } = new List<WebSocket>();
        public static List<WebSocket> BitcornhubWebsocket { get; set; } = new List<WebSocket>();
        public static string[] BitcornhubSocketArgs { get; set; }

        [Authorize(Policy = AuthScopes.SendTransaction)]

        [HttpGet("/bitcornhub")]
        public async Task GetBitcornhub([FromQuery] string settingsColumns)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {

                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await SocketReceiverHandler<BitcornhubSocketReceiver>(webSocket, settingsColumns);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        [Authorize(Policy = AuthScopes.SendTransaction)]

        [HttpGet("/bitcornfarms")]
        public async Task GetBitcornfarms()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {

                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await SocketReceiverHandler<BitcornfarmsSocketReceiver>(webSocket, null);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        private async Task SocketReceiverHandler<T>(WebSocket webSocket, string args) where T : SocketReceiver, new()
        {

            var buffer = new byte[1024 * 4];
            //var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            var receiver = new T();
            receiver.InitDb(_dbContext);
            receiver.Start(webSocket, args);
            await receiver.PostStart(webSocket);
            while (!webSocket.CloseStatus.HasValue)
            {
                try
                {


                    var receiveArr = new ArraySegment<byte>(buffer);
                    //var serverMsg = Encoding.UTF8.GetBytes($"Server: Hello. You said: {Encoding.UTF8.GetString(buffer)}");
                    //await webSocket.SendAsync(new ArraySegment<byte>(serverMsg), result.MessageType, result.EndOfMessage, CancellationToken.None);


                    var result = await webSocket.ReceiveAsync(receiveArr, CancellationToken.None);
                    if (result.Count != 0 || result.CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        string message = Encoding.UTF8.GetString(receiveArr.Array,
                             receiveArr.Offset, result.Count);


                        await receiver.Process(webSocket, message);
                    }
                }
                catch(WebSocketException ex)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await BITCORNLogger.LogError(_dbContext, ex, "web socket error ");
                }

            }
            try
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                await BITCORNLogger.LogError(_dbContext, ex, "failed to close socket");

            }

            receiver.OnClose();
        }

    }


}

