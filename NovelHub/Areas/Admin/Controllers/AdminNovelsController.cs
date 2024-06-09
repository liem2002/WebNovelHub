using NovelHub.App_Start;
using NovelHub.Models;
using NovelHub.Services;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Areas.Admin.Controllers
{
    public class AdminNovelsController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();

        [AdminAuthorize]
        public ActionResult ManagerNovels(int? page)
        {
            if(page == null) page = 1;
            // truy van list
            var AllNovels = db.Novels.OrderByDescending(e=>e.NovelID).ToList();
            int pageSize = 10;
            
            ViewBag.pageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.CountNovels = AllNovels.Count();

            int pageNumber = (page ?? 1);
            return View(AllNovels.ToPagedList(pageNumber, pageSize));
        }
        [AdminAuthorize]
        public ActionResult ViewNovel(int? id)
        {
            if (id == null)
            {
                return Redirect("~/Notification/NotFound");
            }

            var user = (User)HttpContext.Session["User"];
            if (user != null) ViewBag.HasRate = db.Reviews.SingleOrDefault(r => r.NovelID == id && r.UserID == user.UserID);

            var novel = db.Novels.SingleOrDefault(b => b.NovelID == id);
            if (novel.Chapters.Count != 0)
            {
                ViewBag.FirstChapter = novel.Chapters.OrderBy(c => c.ChapterNumber).FirstOrDefault().ChapterID;
            }

            if (novel.Reviews != null && novel.Reviews.Any())
            {
                ViewBag.AverageRating = novel.Reviews.Average(r => r.Rating);
            }
            else
            {
                ViewBag.AverageRating = 0;
            }

            ViewBag.SumView = novel.Chapters.Sum(c => c.Views);
            ViewBag.CountCmt = novel.Chapters.Sum(chapter => chapter.Comments.Count());

            ViewBag.novel = novel;
            ViewBag.Categories = db.Categories.ToList();
            var author = db.Users.SingleOrDefault(u => u.UserID == novel.UserID);
            ViewBag.author = author;

            var currentUser = (User)HttpContext.Session["User"];
            if (currentUser != null)
            {
                if (db.FavoriteNovels.Any(fn => fn.UserID == currentUser.UserID && fn.NovelID == novel.NovelID)) ViewBag.Following = true;
            }
            // Sử dụng đề xuất gợi ý

            var similarNovels = new List<Novel>();
            var recommendation = new RecommendationSystem();

            var res = recommendation.FindSimilarNovels(novel, 10);
            foreach (var novelId in res)
            {
                var findNovel = db.Novels.FirstOrDefault(n => n.NovelID == novelId);
                if (findNovel != null)
                {
                    // Kiểm tra xem cuốn sách đã tồn tại trong danh sách result chưa
                    if (!similarNovels.Contains(findNovel))
                    {
                        similarNovels.Add(findNovel);
                    }
                }
            }
            ViewData["SimilarNovels"] = similarNovels;
            return View();
        }

        [AdminAuthorize]
        public ActionResult ReadingNovel(int? id)
        {
            var chapter = db.Chapters.SingleOrDefault(c => c.ChapterID == id);
            if (id == null)
            {
                return Redirect("~/Notification/NotFound");
            }

            // Lấy danh sách các chương từ cơ sở dữ liệu và sắp xếp theo số chương
            var chapters = db.Chapters.Where(c => c.NovelID == chapter.NovelID).OrderBy(c => c.ChapterNumber).ToList();

            // Tìm index của chương hiện tại trong danh sách
            int currentIndex = chapters.FindIndex(c => c.ChapterID == chapter.ChapterID);
            if (currentIndex == 0)
            {
                ViewBag.startChapter = true;
            }
            else if (currentIndex == chapters.Count - 1)
            {
                ViewBag.endChapter = true;
            }

            // Tìm chương trước (nếu có)
            Chapter prevChapter = null;
            if (currentIndex > 0)
            {
                prevChapter = chapters[currentIndex - 1];
            }

            // Tìm chương sau (nếu có)
            Chapter nextChapter = null;
            if (currentIndex < chapters.Count - 1)
            {
                nextChapter = chapters[currentIndex + 1];
            }

            ViewBag.prevChapter = (prevChapter != null) ? prevChapter.ChapterID : chapters[0].ChapterID;
            ViewBag.nextChapter = (nextChapter != null) ? nextChapter.ChapterID : chapters[chapters.Count - 1].ChapterID;

            ViewBag.Chapter = chapter;
            ViewBag.ContentChapter = db.Chapters.SingleOrDefault(c => c.ChapterID == id).Content;

            ViewBag.Chapters = chapters;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult AddBackListNovel([Bind(Include = "BlacklistedNovelID,NovelID,CreatedAt,Note")] BlacklistedNovel blacklistedNovel)
        {
            blacklistedNovel.CreatedAt = DateTime.Now;
            if (ModelState.IsValid)
            {
                db.BlacklistedNovels.Add(blacklistedNovel);
                db.SaveChanges();
                Toast(true, "Truyện đã được khóa");
                return RedirectToAction("ManagerNovels");
            }
            return View(blacklistedNovel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult RemoveBackListNovel(int novelID)
        {
            var blackNovel = db.BlacklistedNovels.SingleOrDefault(n=>n.NovelID == novelID);
            db.BlacklistedNovels.Remove(blackNovel);
            db.SaveChanges();
            Toast(false, "Truyện đã được duyệt");
            return RedirectToAction("ManagerNovels");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult DeleteNovel(int? id)
        {
            var deleteNovel = db.Novels.Find(id);
            if (deleteNovel.Poster != null)
            {
                var relativeImagePath = "~/Areas/Author/Contents/img/Poster/" + deleteNovel.Poster;
                var serverPath = Server.MapPath(relativeImagePath);
                System.IO.File.Delete(serverPath);
            }
            db.Novels.Remove(deleteNovel);
            db.SaveChanges();
            Toast(false, "Đã xóa thành công");
            return RedirectToAction("ManagerNovels");
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