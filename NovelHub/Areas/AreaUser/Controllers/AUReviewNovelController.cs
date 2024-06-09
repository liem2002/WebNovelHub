using NovelHub.App_Start;
using NovelHub.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Areas.AreaUser.Controllers
{
    public class AUReviewNovelController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();
        public ActionResult RenderReviewsNovel(int id)
        {
            var reviews = db.Reviews.Where(r=>r.NovelID == id).OrderByDescending(r=>r.CreatedAt).ToList();
            var formattedReviews = reviews.Select(r=> new
            {
                r.NovelID,
                r.UserID,
                r.Rating,
                r.ReviewText,
                CreatedAt = r.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                r.User.Username,
                r.User.Avatar,
            }).ToList();
            return Json(formattedReviews, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public ActionResult CreateNewReview([Bind(Include = "NovelID,UserID,Rating,ReviewText,CreatedAt")] Review review)
        {
            var user = (User)HttpContext.Session["User"];
            review.UserID = user.UserID;
            review.CreatedAt = DateTime.Now;
            if (ModelState.IsValid && user != null)
            {
                db.Reviews.Add(review);
                db.SaveChanges();
                return Json(new {success = true}, JsonRequestBehavior.AllowGet);
            }
            return Json(new {success = false}, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public ActionResult EditReviewed([Bind(Include = "NovelID,UserID,Rating,ReviewText,CreatedAt")] Review review)
        {
            var user = (User)HttpContext.Session["User"];
            review.UserID = user.UserID;
            review.CreatedAt = DateTime.Now;
            if (ModelState.IsValid)
            {
                db.Entry(review).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new {success = true}, JsonRequestBehavior.AllowGet);
            }
            return Json(new {success = false}, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public ActionResult DeleteReview(int NovelID)
        {
            var user = (User)HttpContext.Session["User"];
            Review review = db.Reviews.SingleOrDefault(r=>r.NovelID == NovelID && r.UserID == user.UserID);
            db.Reviews.Remove(review);
            db.SaveChanges();
            return Json(new {success = true}, JsonRequestBehavior.AllowGet);
        }
    }
}