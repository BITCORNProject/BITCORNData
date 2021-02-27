using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BITCORNService.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BITCORNService.WebSockets.Utils
{
    public class SocketReceiver
    {
        public virtual void Start(WebSocket socket)
        {



        }
        public virtual async Task Process(WebSocket socket, string data)
        {

        }

        public virtual void OnClose()
        {
        }
    }

    public class BitcornhubSocketReceiver : SocketReceiver
    {
        public override void Start(WebSocket socket)
        {
            //lock (WebSocketsController.BitcornhubWebsocket)
            {
                WebSocketsController.BitcornhubWebsocket = socket;
            }
        }
        public override void OnClose()
        {

            //lock (WebSocketsController.BitcornhubWebsocket)
            {
                WebSocketsController.BitcornhubWebsocket = null;
            }
        }
        public override async Task Process(WebSocket socket, string data)
        {
            
            var serverMsg = Encoding.UTF8.GetBytes("stuff");
            await socket.SendAsync(new ArraySegment<byte>(serverMsg), WebSocketMessageType.Text, true, CancellationToken.None);

        }
    }
}
