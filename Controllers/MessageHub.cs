using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Owin;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Newtonsoft.Json.Linq;
using Smart.Data;
using Smart.Repository;
using Smart.Service;
using Smart.ViewModels;
using AuthorizeAttribute = System.Web.Mvc.AuthorizeAttribute;

namespace Smart.Controllers
{

    public class MessageHub : Hub
    {
        private IMessageService _messageService;
        public MessageHub()
        {
            this._messageService = new MessageService(new MessageRepository(new AuthContext()), new MappingService());
        }
        private static Dictionary<string, string> _users_ConnectionIds = new Dictionary<string, string>();
        public override Task OnConnected() => base.OnConnected();






        public override Task OnDisconnected(bool stopCalled)
        {
            _users_ConnectionIds.Remove(Context.User.Identity.Name);
            return base.OnDisconnected(stopCalled);
        }




        private void BroadcastMessage(string message)
        {
            var userName = Context.User.Identity.Name;

            Clients.Group("authenticated").OnMessage(userName, message);
        }

        [Authorize]
        public void SendMessage(MessageViewModel message)
        {
            if (message == null)
                return;
            if (!_users_ConnectionIds.ContainsKey(Context.ConnectionId))
                _users_ConnectionIds.Add(Context.ConnectionId, message.User.UserName);

            MessageViewModel createdMessage = _messageService.Add(message);
            if (createdMessage != null)
            {
                foreach (var item in createdMessage.Chat.Users)
                {
                    var result = _users_ConnectionIds.SingleOrDefault(x => x.Value.Equals(item.UserName)).Key;
                    if (result != null)
                    {
                        Clients.Client(result).OnMessage(createdMessage);
                    }
                }
            }
        }
    }
}