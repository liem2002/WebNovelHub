using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Controllers
{
    public class NotificationController : Controller
    {
        // GET: Notification
        public ActionResult Notification()
        {
            return View();
        }

        public ActionResult NotFound()
        {
            return View();
        }
    }
}