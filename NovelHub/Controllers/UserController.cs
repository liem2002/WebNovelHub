using NovelHub.App_Start;
using NovelHub.Models;
using PagedList;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Controllers
{
    public class UserController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();
        [CheckLoginAuthorize]
        public ActionResult ViewUserProfile(int? id, int? page)
        {
            var user = db.Users.Find(id);
            var NovelsOfUser = db.Novels.Where(n=>n.UserID == id).ToList();

            if (page == null) page = 1;
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            ViewBag.User = user;
            ViewBag.NovelsOfUserCount = NovelsOfUser.Count;

            return View(NovelsOfUser.ToPagedList(pageNumber, pageSize));
        }

    }
}