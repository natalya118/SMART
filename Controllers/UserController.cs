using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Smart.Repository;
using Smart.Service;
using Smart.ViewModels;

namespace Smart.Controllers
{
    public class UserController : ApiController
    {
        private IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public HttpResponseMessage Get(string id)
        {
            UserViewModel user = _userService.GetUserByName(id);
            return Request.CreateResponse(HttpStatusCode.OK, user);
        }

        [HttpPost]
        public HttpResponseMessage Post(AddUserToChatViewModel obj)
        {

            if (ModelState.IsValid)
            {
                _userService.AddUserToChat(obj.UserName, obj.ChatTitle);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            return Request.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
