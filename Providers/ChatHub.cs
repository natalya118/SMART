using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace Smart.Providers
{
    public class ChatHub : Hub
    {
        public override Task OnConnected()
        {
            AssignToSecurityGroup();
            Greet();

            return base.OnConnected();
        }

        private void AssignToSecurityGroup()
        {
            if (Context.User.Identity.IsAuthenticated)
                Groups.Add(Context.ConnectionId, "authenticated");
            else
                Groups.Add(Context.ConnectionId, "anonymous");
        }

        private void Greet()
        {
            var greetedUserName = Context.User.Identity.IsAuthenticated ?
                Context.User.Identity.Name :
                "anonymous";

            Clients.Client(Context.ConnectionId).OnMessage(
                "[server]", "Welcome to the chat room, " + greetedUserName);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            if (stopCalled)
            {
                // We know that Stop() was called on the client, 
                // and the connection shut down gracefully. 
            }
            else
            {
                // This server hasn't heard from the client in the last ~35 seconds. 
                // If SignalR is behind a load balancer with scaleout configured, 
                // the client may still be connected to another SignalR server. 
            }
            RemoveFromSecurityGroups();
            return base.OnDisconnected(stopCalled);
        }

        private void RemoveFromSecurityGroups()
        {
            Groups.Remove(Context.ConnectionId, "authenticated");
            Groups.Remove(Context.ConnectionId, "anonymous");
        }

        [Authorize]
        public void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            BroadcastMessage(message);
        }

        private void BroadcastMessage(string message)
        {
            var userName = Context.User.Identity.Name;

            Clients.Group("authenticated").OnMessage(userName, message);

            var excerpt = message.Length <= 3 ? message : message.Substring(0, 3) + "...";
            Clients.Group("anonymous").OnMessage("[someone]", excerpt);
        }
    }
}