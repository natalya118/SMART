using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Smart.Service;
using Smart.ViewModels;

namespace Smart.Controllers
{
    public class MessagesController : ApiController
    {
        private IMessageService _messageService;
        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }


        /// <summary>
        /// Get all messages for chat
        /// </summary>
        /// <param name="id">Chat ID</param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage Get(string id)
        {
            try
            {
                List<MessageViewModel> messages = _messageService.GetAllMessagesFromChat(id);
                return Request.CreateResponse(HttpStatusCode.OK, messages);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }



        }
    }
}
