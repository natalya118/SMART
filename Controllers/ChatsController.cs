using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Smart.Repository;
using Smart.Service;
using Smart.ViewModels;

namespace Smart.Controllers
{
    [RoutePrefix("api/Chats")]
    public class ChatsController : ApiController
    {
        private IChatsService _chatsService;

        public ChatsController(IChatsService chatsService)
        {
            _chatsService = chatsService;
        }

        [HttpGet]
        public HttpResponseMessage Get(string id)
        {
            //Get all chats by username
            List<ChatViewModel> chats = _chatsService.GetAllChats(id);
            return Request.CreateResponse(HttpStatusCode.OK, chats);
        }



        [HttpPost]
        public HttpResponseMessage Post(ChatViewModel newChat)
        {
            if (ModelState.IsValid)
            {
                ChatViewModel chat = _chatsService.Add(newChat);
                if (chat != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, chat);
                }

            }
            return Request.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
