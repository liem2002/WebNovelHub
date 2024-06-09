using NovelHub.App_Start;
using NovelHub.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Areas.Author.Controllers
{
    public class AuthorNovelsController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();
        [AuthorAuthorize]
        public ActionResult PublishNovel()
        {
            ViewBag.NovelStatuses = db.NovelStatuses.ToList();
            ViewBag.Categories = db.Categories.ToList();
            return View();
        }

        [HttpPost]
        [AuthorAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult PublishNovel([Bind(Include = "NovelID,UserID,Title,Author,Description,Poster,CreatedAt,StatusID,LastUpdate")] Novel novel, List<int> CategoriesList, HttpPostedFileBase Poster)
        {
            ViewBag.NovelStatuses = db.NovelStatuses.ToList();
            ViewBag.Categories = db.Categories.ToList();
            var CurrentUser = (User)HttpContext.Session["User"];
            
            #region Default value
            novel.UserID = CurrentUser.UserID;
            novel.CreatedAt = DateTime.Now;
            novel.LastUpdate = DateTime.Now;
            #endregion

            if (ModelState.IsValid)
            {

                #region Handle poster
                if (Poster != null && Poster.ContentLength > 0)
                {
                    // Lưu ảnh
                    string formattedDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var fileName = formattedDate  + "_Poster_" + CurrentUser.UserID + "_" + Path.GetFileName(Poster.FileName);
                    var imagePath = Path.Combine(Server.MapPath("~/Areas/Author/Contents/img/Poster/"), fileName);
                    Poster.SaveAs(imagePath);
                    novel.Poster = fileName;
                }
                #endregion

                #region Add NovelCategories
                if (CategoriesList != null)
                {
                    foreach (var item in CategoriesList)
                    {
                        NovelCategory novelCategory = new NovelCategory();
                        novelCategory.NovelID = novel.NovelID;
                        novelCategory.CategoryID = item;
                        novelCategory.Note = "";
                        db.NovelCategories.Add(novelCategory);
                    }
                }

                #endregion

                #region Default Add novel to BlacklistedNovel
                BlacklistedNovel blackNovel = new BlacklistedNovel();
                blackNovel.NovelID = novel.NovelID;
                blackNovel.Note = "Truyện mới";
                blackNovel.CreatedAt = DateTime.Now;
                db.BlacklistedNovels.Add(blackNovel);
                #endregion

                db.Novels.Add(novel);
                db.SaveChanges();
                return RedirectToAction("PublishedNovel");
            }
            return View();
        }
        
        [AuthorAuthorize]
        public ActionResult PublishedNovel(int? page)
        {
            ViewBag.Categories = db.Categories.ToList();
            var currentUser = (User)HttpContext.Session["User"];
            int getUserID = currentUser.UserID;
            if(page == null) page = 1;
            // truy van list
            var novelList = db.Novels.Where(n=>n.UserID == getUserID).ToList();
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            ViewBag.pageSize = pageSize;
            ViewBag.CurrentPage = page;

            ViewBag.CountCreatedNovel = novelList.Count();
            return View(novelList.ToPagedList(pageNumber, pageSize));
        }

        [AuthorAuthorize]
        public ActionResult NovelUpdate(int? id)
        {
            ViewBag.Categories = db.Categories.ToList();
            ViewBag.NovelStatuses = db.NovelStatuses.ToList();

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var novel = db.Novels.Find(id);
            if (novel == null)
            {
                return HttpNotFound();
            }
            // Toast
            ViewBag.isError = TempData["isError"];
            ViewBag.Color = TempData["Color"];
            ViewBag.ToastContent = TempData["ToastContent"];

            // Error Edit form
            ViewBag.ErrorBookTitle = TempData["ErrorBookTitle"];

            return View(novel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorAuthorize]
        public ActionResult NovelUpdate([Bind(Include = "NovelID,UserID,Title,Author,Description,Poster,CreatedAt,StatusID, LastUpdate")] Novel novel, HttpPostedFileBase Poster, string originPoster, DateTime? time, int[] CategoriesList)
        {
            #region Default value
            novel.CreatedAt = time;
            novel.LastUpdate = DateTime.Now;
            #endregion

            if (ModelState.IsValid)
            {
                #region Xử lý ảnh
                if (Poster != null && Poster.ContentLength > 0)
                {
                    // Xóa ảnh
                    if (originPoster != "")
                    {
                        var relativeImagePath = "~/Areas/Author/Contents/img/Poster/" + originPoster;
                        var serverPath = Server.MapPath(relativeImagePath);
                        System.IO.File.Delete(serverPath);
                    }
                    // Lưu ảnh
                    string formattedDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var fileName = formattedDate  + "_Poster_" + novel.NovelID + "_" + Path.GetFileName(Poster.FileName);
                    var imagePath = Path.Combine(Server.MapPath("~/Areas/Author/Contents/img/Poster/"), fileName);
                    Poster.SaveAs(imagePath);
                    novel.Poster = fileName;
                }
                else
                {
                    novel.Poster = originPoster;
                }
                #endregion

                #region category handle
                var cIDs = db.NovelCategories.Where(nc=>nc.NovelID == novel.NovelID).OrderBy(nc=>nc.CategoryID).Select(c=>c.CategoryID).ToList();
                if(cIDs.SequenceEqual(CategoriesList.OrderBy(x => x)) == false)
                {
                    var ncs = db.NovelCategories.Where(nc=>nc.NovelID == novel.NovelID).ToList();
                    db.NovelCategories.RemoveRange(ncs);
                    db.SaveChanges();

                    foreach(var item in CategoriesList)
                    {
                        var novelCategory = new NovelCategory{ 
                                NovelID = novel.NovelID,
                                CategoryID = item,
                                Note = ""
                            };
                        db.NovelCategories.Add(novelCategory);
                    }
                    db.SaveChanges();
                }
                #endregion

                db.Entry(novel).State = EntityState.Modified;
                db.SaveChanges();
                Toast(false, "Chỉnh sửa thành công");
                return RedirectToAction("PublishedNovel");
            }
            if(novel.Title == null)
            {
                TempData["ErrorBookTitle"] = "Vui lòng nhập vào trường này";
            }
            Toast(true, "Chỉnh sửa thất bại");
            return RedirectToAction("NovelUpdate", new { id = novel.NovelID});
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