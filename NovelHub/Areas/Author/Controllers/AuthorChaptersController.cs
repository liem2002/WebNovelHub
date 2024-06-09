using NovelHub.App_Start;
using NovelHub.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Areas.Author.Controllers
{
    public class AuthorChaptersController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();
        [AuthorAuthorize]
        public ActionResult ManagerChapters(int? novelID)
        {
            var CurrtentUser = (User)HttpContext.Session["User"];
            ViewBag.Categories = db.Categories.ToList();

            if(novelID != null)
            {
                ViewBag.CurrentNovel = db.Novels.SingleOrDefault(n=>n.NovelID == novelID);
                var CountChapters = db.Chapters.Where(c=>c.NovelID == novelID).Count();
                ViewBag.CountChapters = CountChapters;

                if(CountChapters == 0)
                {
                    ViewBag.NewChapterNumber = 1;
                }
                else
                {
                    ViewBag.NewChapterNumber = db.Chapters
                        .Where(c => c.NovelID == novelID)
                        .OrderByDescending(c => c.ChapterNumber)
                        .FirstOrDefault().ChapterNumber + 1;
                }
                return View();
            }
            else
            {
                var firstNovel = db.Novels.Where(n=>n.UserID == CurrtentUser.UserID).OrderBy(n=>n.NovelID).FirstOrDefault();
                ViewBag.CurrentNovel = firstNovel;
                var CountChapters = db.Chapters.Where(c=>c.NovelID == firstNovel.NovelID).Count();
                ViewBag.CountChapters = CountChapters;

                if(CountChapters == 0)
                {
                    ViewBag.NewChapterNumber = 1;
                }
                else
                {
                    ViewBag.NewChapterNumber = db.Chapters
                        .Where(c => c.NovelID == firstNovel.NovelID)
                        .OrderByDescending(c => c.ChapterNumber)
                        .FirstOrDefault().ChapterNumber + 1;
                }
                return View();
            }
        }
        [AuthorAuthorize]
        public ActionResult RenderChapters(int novelID)
        {
            var Chapters = db.Chapters.Where(c=>c.NovelID == novelID).OrderBy(c=>c.ChapterNumber).ToList();
            var formatChapters = Chapters.Select(chapter => new { 
                    ChapterID = chapter.ChapterID,
                    NovelID = chapter.NovelID,
                    ChapterTitle = chapter.ChapterTitle,
                    ChapterNumber = chapter.ChapterNumber,
                    CreatedAt = chapter.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Views = chapter.Views
                });
            return Json(formatChapters, JsonRequestBehavior.AllowGet);
        }

        [AuthorAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PublishChapter([Bind(Include = "ChapterID,NovelID,ChapterNumber,ChapterTitle,Content,Views,CreatedAt")] Chapter chapter)
        {
            chapter.Views = 0;
            chapter.CreatedAt = DateTime.Now;
            //chapter.chapter_content = chapter.chapter_content.Trim();
            if (ModelState.IsValid && chapter.ChapterNumber != null && chapter.ChapterTitle != null && chapter.ChapterNumber >= 0)
            {
                db.Chapters.Add(chapter);
                db.SaveChanges();

                var novel = db.Novels.Find(chapter.NovelID);
                novel.LastUpdate = DateTime.Now;
                db.Entry(novel).State = EntityState.Modified;

                db.SaveChanges();

                Toast(false, "Đã thêm chương thành công");
                return RedirectToAction("ManagerChapters", new { novelID = chapter.NovelID});
            }

            TempData["ErrorCreate"] = true;
            Toast(true, "Thêm chương mới thất bại");
            if(chapter.ChapterNumber == null)
            {
                TempData["BorderDangerForChapterNumber"] = "border-danger";
                TempData["ChapterNumber"] = "Trường này không được để trống";
            } else if(chapter.ChapterNumber < 0) {
                TempData["BorderDangerForChapterNumber"] = "border-danger";
                TempData["ChapterNumber"] = "Vui lòng nhập số chương lớn hơn hoặc bằng 0";
            }
            if(chapter.ChapterTitle == null)
            {
                TempData["BorderDangerForChapterTitle"] = "border-danger";
                TempData["ChapterTitle"] = "Trường này không được để trống";
            }
            return RedirectToAction("ManagerChapters", new { novelID = chapter.NovelID});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorAuthorize]
        public ActionResult DeleteChapter(int id)
        {
            Chapter chapter = db.Chapters.Find(id);
            var novelID = chapter.NovelID;
            db.Chapters.Remove(chapter);
            db.SaveChanges();
            Toast(false, "Đã xóa thành công");
            return RedirectToAction("ManagerChapters", new { novelID = novelID});
        }
        
        [AuthorAuthorize]
        public ActionResult UpdateChapter(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Chapter chapter = db.Chapters.Find(id);
            if (chapter == null)
            {
                return HttpNotFound();
            }
            return View(chapter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorAuthorize]
        public ActionResult UpdateChapter([Bind(Include = "ChapterID,NovelID,ChapterNumber,ChapterTitle,Content,Views,CreatedAt")] Chapter chapter)
        {
            if (ModelState.IsValid && chapter.ChapterNumber != null && chapter.ChapterTitle != null && chapter.ChapterNumber >= 0)
            {
                db.Entry(chapter).State = EntityState.Modified;
                db.SaveChanges();
                Toast(false, "Cập nhật thành công");
                return RedirectToAction("ManagerChapters", new { novelID = chapter.NovelID});
            }
            TempData["ErrorEdit"] = true;
            Toast(true, "Cập nhật thất bại");
            if(chapter.ChapterNumber == null)
            {
                TempData["BorderDangerForChapterNumber"] = "border-danger";
                TempData["ChapterNumber"] = "Trường này không được để trống";
            } else if(chapter.ChapterNumber < 0) {
                TempData["BorderDangerForChapterNumber"] = "border-danger";
                TempData["ChapterNumber"] = "Vui lòng nhập số chương lớn hơn hoặc bằng 0";
            }
            if(chapter.ChapterTitle == null)
            {
                TempData["BorderDangerForChapterTitle"] = "border-danger";
                TempData["ChapterTitle"] = "Trường này không được để trống";
            }
            return RedirectToAction("UpdateChapter", new { id = chapter.ChapterID});
            //return View(chapter);
        }

        public void Toast(bool isError, string contentToast){
            TempData["ActiveToast"] = true;
            if(isError)
            {
                TempData["isError"] = true;
                TempData["Color"] = "#dc3545";
                TempData["ToastContent"] = contentToast;
            }
            else
            {
                TempData["isError"] = false;
                TempData["Color"] = "#00dc82";
                TempData["ToastContent"] = contentToast;
            }
        }
    }
}