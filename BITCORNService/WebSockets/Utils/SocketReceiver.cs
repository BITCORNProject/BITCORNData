using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BITCORNService.WebSockets.Utils
{
    public class SocketReceiver
    {
        protected BitcornContext _dbContext = null;
        public void InitDb(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        public virtual async Task Send(WebSocket socket, string message)
        {
            var serverMsg = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(serverMsg), WebSocketMessageType.Text, true, CancellationToken.None);

        }
        public virtual void Start(WebSocket socket, string args)
        {



        }

        public virtual async Task PostStart(WebSocket socket)
        {

        }
        public virtual async Task Process(WebSocket socket, string data)
        {

        }

        public virtual void OnClose()
        {
        }
    }

    public class BattlegroundsSocketReceiver : SocketReceiver
    {
        WebSocket _socket = null;

        public override async Task PostStart(WebSocket socket)
        {
            
            /*
            while (!socket.CloseStatus.HasValue)
            {
                await Task.Delay(2000);
                await Send(socket, JsonConvert.SerializeObject(new
                {
                    type = "testing",
                    payload = new { 
                        stuff = "123"
                    }
                }));
            }
            */
            
        }
        string _authId = null;
        public override void Start(WebSocket socket, string args)
        {
            _socket = socket;
            _authId = args;
            //lock (WebSocketsController.BitcornhubWebsocket)
            if(!string.IsNullOrEmpty(args))
            {
                lock (WebSocketsController.BattlegroundsWebsocket)
                {
                    if(!WebSocketsController.BattlegroundsWebsocket.TryGetValue(args,out var list))
                    {
                        list = new List<WebSocket>();
                        WebSocketsController.BattlegroundsWebsocket.Add(args,list);
                    }

                    list.Add(socket);
                    //WebSocketsController.BattlegroundsWebsocket.Add(args, socket);
                }
            }

        }
        public override void OnClose()
        {

            if (!string.IsNullOrEmpty(_authId))
            {
                lock (WebSocketsController.BattlegroundsWebsocket)
                {
                    WebSocketsController.BattlegroundsWebsocket.Remove(_authId);
                    _socket.Dispose();
                    //WebSocketsController.BitcornhubWebsocket = null;
                }
            }
        }
    }


    public class BitcornfarmsSocketReceiver : SocketReceiver
    {
        public override async Task PostStart(WebSocket socket)
        {
            /*
            while (!socket.CloseStatus.HasValue)
            {
                await Task.Delay(6000);
                var serverMsg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "stuff", payload = "shit" }));
                await socket.SendAsync(new ArraySegment<byte>(serverMsg), WebSocketMessageType.Text, true, CancellationToken.None);

            }*/
        }
        public override void Start(WebSocket socket, string args)
        {

            //lock (WebSocketsController.BitcornhubWebsocket)
            {
                lock (WebSocketsController.BitcornFarmsWebSocket)
                {
                    WebSocketsController.BitcornFarmsWebSocket.Add(socket);
                }
            }

        }
        public override void OnClose()
        {

            lock (WebSocketsController.BitcornFarmsWebSocket)
            {
                WebSocketsController.BitcornFarmsWebSocket.Clear();
                //WebSocketsController.BitcornhubWebsocket = null;
            }
        }

        public override async Task Process(WebSocket socket, string data)
        {

        }
    }
    public class BitcornhubSocketReceiver : SocketReceiver
    {

        public override void Start(WebSocket socket, string args)
        {
            //lock (WebSocketsController.BitcornhubWebsocket)

            {
                lock (WebSocketsController.BitcornhubWebsocket)
                {
                    WebSocketsController.BitcornhubWebsocket.Add(socket);
                }
                if (!string.IsNullOrEmpty(args))
                {
                    var split = args.Split(" ");
                    if (split.Length == 1 && split[0] == "all")
                    {
                        WebSocketsController.BitcornhubSocketArgs = new string[] { "*" };

                    }
                    else
                    {
                        WebSocketsController.BitcornhubSocketArgs = split;
                    }
                }
                else
                {
                    WebSocketsController.BitcornhubSocketArgs = new string[0];

                }
            }

        }

        public override async Task PostStart(WebSocket socket)
        {
            try
            {
                var settings = await LivestreamUtils.GetLivestreamSettings(_dbContext, WebSocketsController.BitcornhubSocketArgs);
                await Send(socket, JsonConvert.SerializeObject(new
                {
                    type = "initial-settings",
                    payload = settings
                }));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public override void OnClose()
        {

            lock (WebSocketsController.BitcornhubWebsocket)
            {
                WebSocketsController.BitcornhubWebsocket.Clear();
                //WebSocketsController.BitcornhubWebsocket = null;
            }
        }
        public override async Task Process(WebSocket socket, string data)
        {
            /*
            */
        }
    }
}
