using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Smart.Data;
using Smart.Models.Entities;
using Smart.Repository;
using Smart.Service;
using Smart.ViewModels;

namespace Smart.Controllers
{
    //[RequireHttps]
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            //List<ChatViewModel> allChats = chats.GetAllChats(); 
            return View();
        }
    }
}