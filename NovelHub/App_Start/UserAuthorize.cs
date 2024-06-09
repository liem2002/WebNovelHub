using NovelHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.App_Start
{
    public class UserAuthorize: AuthorizeAttribute
    {
        private NovelHubEntities  db = new NovelHubEntities();
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            // 1. Check session:
            var CurrentUser = (User)HttpContext.Current.Session["User"];
            if(CurrentUser != null)
            {
                return;
            }
            else
            {
                if (filterContext.HttpContext.Request.Cookies["LoginCookie"] != null)
                {
                    string email = filterContext.HttpContext.Request.Cookies["LoginCookie"]["Email"];
                    string password = filterContext.HttpContext.Request.Cookies["LoginCookie"]["Password"];

                    var user = db.Users.SingleOrDefault(m => m.Email == email && m.Password == password);

                    if (user != null)
                    {
                        filterContext.HttpContext.Session["User"] = user;
                        return;
                    }
                }

                var returnUrl = filterContext.RequestContext.HttpContext.Request.RawUrl;
                var newUrl = "~/Notification/Notification";
                filterContext.Result = new RedirectResult(newUrl);
            }
        }
    }
}