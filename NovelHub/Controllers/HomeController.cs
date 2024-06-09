using NovelHub.App_Start;
using NovelHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Controllers
{
    public class HomeController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();
        [CheckLoginAuthorize]
        public ActionResult Index()
        {
            //#region Check cookie
            //if (Request.Cookies["LoginCookie"] != null)
            //{
            //    string email = Request.Cookies["LoginCookie"]["Email"];
            //    string password = Request.Cookies["LoginCookie"]["Password"];

            //    var user = db.Users.SingleOrDefault(m => m.Email == email && m.Password == password);

            //    if (user != null)
            //    {
            //        Session["User"] = user;
            //    }
            //}
            //#endregion

            var favoriteNovels = db.Novels.Where(n => n.BlacklistedNovels.Count == 0).OrderByDescending(n => n.FavoriteNovels.Count).Take(8).ToList();

            ViewBag.FavoriteNovels = favoriteNovels;

            return View();
        }
    }
}