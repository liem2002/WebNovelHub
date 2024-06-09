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
    public class AUCommentController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();
        public ActionResult GetAllCommentsNovel(int id)
        {
            var comments = db.Comments.Where(c=>c.Chapter.NovelID == id).OrderByDescending(c=>c.CreatedAt).ToList();
            var formattedComment = comments.Select(c=> new
            {
                c.CommentID,
                c.UserID,
                c.User.Username,
                c.User.Avatar,
                c.CommentText,
                CreatedAt = c.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                c.ChapterID,
                c.Chapter.ChapterNumber,
                c.Chapter.NovelID,

            }).ToList();

            return Json(formattedComment, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAllCommentsChapter(int id)
        {
            var comments = db.Comments.Where(c=>c.ChapterID == id).OrderByDescending(c=>c.CreatedAt).ToList();
            var formattedComment = comments.Select(c=> new
            {
                c.CommentID,
                c.UserID,
                c.User.Username,
                c.User.Avatar,
                c.CommentText,
                CreatedAt = c.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                c.ChapterID,
                c.Chapter.ChapterNumber,
                c.Chapter.NovelID,

            }).ToList();

            return Json(formattedComment, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public ActionResult CreateNewComment([Bind(Include = "CommentID,ChapterID,UserID,CommentText,CreatedAt")] Comment comment)
        {
            var user = (User)HttpContext.Session["User"];
            comment.UserID = user.UserID;
            comment.CreatedAt = DateTime.Now;
            if (ModelState.IsValid && user != null)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                comment.User = db.Users.Find(user.UserID);
                comment.Chapter = db.Chapters.Find(comment.ChapterID);
                var cmt = new
                {
                    comment.CommentID,
                    comment.UserID,
                    comment.User.Username,
                    comment.User.Avatar,
                    comment.CommentText,
                    CreatedAt = comment.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                    comment.ChapterID,
                    comment.Chapter.ChapterNumber,
                    comment.Chapter.NovelID,
                };

                return Json(new {success = true, type="onCreate", comment = cmt }, JsonRequestBehavior.AllowGet);
            }
            return Json(new {success = false}, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public ActionResult EditCommented([Bind(Include = "CommentID,ChapterID,UserID,CommentText,CreatedAt")] Comment comment, string Time)
        {
            comment.CreatedAt = DateTime.Parse(Time);

            if (ModelState.IsValid)
            {
                db.Entry(comment).State = EntityState.Modified;
                db.SaveChanges();
                comment.User = db.Users.Find(comment.UserID);
                comment.Chapter = db.Chapters.Find(comment.ChapterID);
                var cmt = new
                {
                    comment.CommentID,
                    comment.UserID,
                    comment.User.Username,
                    comment.User.Avatar,
                    comment.CommentText,
                    CreatedAt = comment.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                    comment.ChapterID,
                    comment.Chapter.ChapterNumber,
                    comment.Chapter.NovelID,
                };
                return Json(new {success = true, type="onEdit", comment = cmt }, JsonRequestBehavior.AllowGet);
            }
            return Json(new {success = false}, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public ActionResult DeleteComment(int id)
        {
            var comment = db.Comments.Find(id);
            db.Comments.Remove(comment);
            db.SaveChanges();
            return Json(new {success = true, type="ondelete"}, JsonRequestBehavior.AllowGet);
        }

        [UserAuthorize]
        public ActionResult EditComment(int id)
        {
            var comments = db.Comments.ToList();
            var comment = comments.Select(c => new { 
                    c.CommentID,
                    c.UserID,
                    c.User.Username,
                    c.User.Avatar,
                    c.CommentText,
                    CreatedAt = c.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                    c.ChapterID,
                    c.Chapter.ChapterNumber,
                    c.Chapter.NovelID,
                })
                .ToList()
                .SingleOrDefault(c=>c.CommentID == id);
            return Json(comment, JsonRequestBehavior.AllowGet);
        }
    }
}