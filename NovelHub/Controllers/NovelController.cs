using NovelHub.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Threading.Tasks;
using NovelHub.App_Start;
using NovelHub.Services;

namespace NovelHub.Controllers
{
    public class NovelController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();

        public ActionResult GetMostViewedNovels()
        {
            var getMostViewedNovels = db.Novels
                .Where(n=>n.BlacklistedNovels.Count == 0)
                .OrderByDescending(n => n.Chapters
                .Sum(c=>c.Views))
                .Take(15)
                .ToList();

            var formattedGetMostViewedNovels = getMostViewedNovels.Select(novel => new
            {
                NovelID = novel.NovelID,
                Poster = novel.Poster,
                Title = novel.Title,
                TotalViews = novel.Chapters.Sum(c=>c.Views),
                TotalHearts = novel.FavoriteNovels.Count(n=>n.NovelID == novel.NovelID),
                TotalComments = novel.Chapters.SelectMany(c => c.Comments).Count(),
                LastChapter = (novel.Chapters.Count == 0) ? "" : novel.Chapters.OrderByDescending(c => c.ChapterNumber).FirstOrDefault().ChapterNumber.ToString(),
                LastUpdated = (novel.Chapters.Count == 0) ? novel.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss") : novel.Chapters.OrderByDescending(c => c.ChapterNumber).FirstOrDefault().CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),

            }).ToList();
            return Json(formattedGetMostViewedNovels, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetRecentlyUpdatedNovels()
        {
            var getRecentlyUpdatedNovels = db.Novels
                .Where(n => n.BlacklistedNovels.Count == 0) // .Where(n => n.BlacklistedNovels.Count == 0 && n.Chapters.Any()) 
                .Select(n => new
                {
                    Novel = n,
                    LastChapter = n.Chapters.OrderByDescending(c => c.CreatedAt).FirstOrDefault()
                })
                .OrderByDescending(result => result.LastChapter.CreatedAt) // Sắp xếp theo thời gian tạo của chương cuối cùng
                .Take(15)
                .Select(result => result.Novel)
                .ToList();
            
            var formattedGetRecentlyUpdatedNovels = getRecentlyUpdatedNovels.Select(novel => new
            {
                NovelID = novel.NovelID,
                Poster = novel.Poster,
                Title = novel.Title,
                TotalViews = novel.Chapters.Sum(c=>c.Views),
                TotalHearts = novel.FavoriteNovels.Count(n=>n.NovelID == novel.NovelID),
                TotalComments = novel.Chapters.SelectMany(c => c.Comments).Count(),
                LastChapter = (novel.Chapters.Count == 0) ? "" : novel.Chapters.OrderByDescending(c => c.ChapterNumber).FirstOrDefault().ChapterNumber.ToString(),
                LastUpdated = (novel.Chapters.Count == 0) ? novel.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss") : novel.Chapters.OrderByDescending(c => c.ChapterNumber).FirstOrDefault().CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),

            }).ToList();
            return Json(formattedGetRecentlyUpdatedNovels, JsonRequestBehavior.AllowGet);
        }

        [CheckLoginAuthorize]
        public ActionResult ViewNovel(int? id)
        {
            if(db.BlacklistedNovels.Any(bln =>bln.NovelID == id) || id == null)
            {
                return Redirect("~/Notification/NotFound");
            }
            
            var user = (User)HttpContext.Session["User"];
            if(user != null) ViewBag.HasRate = db.Reviews.SingleOrDefault(r=>r.NovelID == id && r.UserID == user.UserID);

            var novel = db.Novels.SingleOrDefault(b=>b.NovelID == id);
            if(novel.Chapters.Count != 0)
            {
                ViewBag.FirstChapter = novel.Chapters.OrderBy(c=>c.ChapterNumber).FirstOrDefault().ChapterID;
            }

            if(novel.Reviews != null && novel.Reviews.Any())
            {
                ViewBag.AverageRating = novel.Reviews.Average(r=>r.Rating);
            }
            else
            {
                ViewBag.AverageRating = 0;
            }

            ViewBag.SumView = novel.Chapters.Sum(c=>c.Views);
            ViewBag.CountCmt = novel.Chapters.Sum(chapter => chapter.Comments.Count());

            ViewBag.novel = novel;
            ViewBag.Categories = db.Categories.ToList();
            var author = db.Users.SingleOrDefault(u => u.UserID == novel.UserID);
            ViewBag.author = author;

            var currentUser = (User)HttpContext.Session["User"];
            if(currentUser != null)
            {
                if(db.FavoriteNovels.Any(fn=>fn.UserID == currentUser.UserID && fn.NovelID == novel.NovelID)) ViewBag.Following = true;
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

        public ActionResult RenderChapters(int id)
        {
            var chapters = db.Chapters.Where(c=>c.NovelID == id).OrderBy(c=>c.ChapterNumber).ToList();

            var formattedChapters = chapters.Select(chapter => new
            {
                ChapterID = chapter.ChapterID,
                NovelID = chapter.NovelID,
                ChapterNumber = chapter.ChapterNumber,
                ChapterTitle = chapter.ChapterTitle,
                Content = chapter.Content,
                Views = chapter.Views,
                CreatedAt = chapter.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
            });

            return Json(formattedChapters, JsonRequestBehavior.AllowGet);
        }
    
        [CheckLoginAuthorize]
        public ActionResult ReadingNovel(int id)
        {
            var chapter = db.Chapters.SingleOrDefault(c=>c.ChapterID == id);
            if(db.BlacklistedNovels.Any(bln => bln.NovelID == chapter.NovelID))
            {
                return Redirect("~/Notification/NotFound");
            }

            // Lấy danh sách các chương từ cơ sở dữ liệu và sắp xếp theo số chương
            var chapters = db.Chapters.Where(c => c.NovelID == chapter.NovelID).OrderBy(c => c.ChapterNumber).ToList();

            // Tìm index của chương hiện tại trong danh sách
            int currentIndex = chapters.FindIndex(c => c.ChapterID == chapter.ChapterID);
            if(currentIndex == 0)
            {
                ViewBag.startChapter = true;
            }
            else if (currentIndex == chapters.Count-1)
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
            ViewBag.nextChapter = (nextChapter != null) ? nextChapter.ChapterID : chapters[chapters.Count-1].ChapterID;

            ViewBag.Chapter = chapter;
            ViewBag.ContentChapter = db.Chapters.SingleOrDefault(c=>c.ChapterID == id).Content;

            ViewBag.Chapters = chapters;
            return View();
        }

        public string RemoveDiacritics(string input)
        {
            string normalizedString = input.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public List<Novel> SearchMethod(string input)
        {
            var searchString = RemoveDiacritics(input).ToLower(); // Loại bỏ dấu và chuyển thành chữ thường
            var novels = db.Novels.AsEnumerable() // Chuyển đổi thành danh sách bằng AsEnumerable()
                            .Where(novel => !db.BlacklistedNovels.Any(bn => bn.NovelID == novel.NovelID) && // Kiểm tra tiểu thuyết không nằm trong BlacklistedNovel
                                            (RemoveDiacritics(novel.Title).ToLower().Contains(searchString) ||
                                            novel.Chapters.Any(chapter => RemoveDiacritics(chapter.Content).ToLower().Contains(searchString))))
                            .ToList();

            return novels;
        }
        
        [HttpGet]
        public ActionResult FindByCategory(int categoryID)
        {
            var categories = new List<int>{ categoryID };
            var novels = db.Novels.Where(n=>n.NovelCategories.Any(nc=>nc.CategoryID == categoryID)).ToList();
            TempData["ResultFilterNovels"] = novels;
            TempData["CategoriesChk"] = categories;
            return RedirectToAction("FilterNovels");
        }

        [HttpGet]
        public ActionResult Search(string searchString)
        {
            TempData.Remove("FilterNovels");
            var novels = SearchMethod(searchString);
            var contentToast = "Có " + novels.Count() + " kết quả được tìm thấy";
            Toast(false, contentToast);
            TempData["SearchNovels"] = novels;
            TempData.Keep("SearchNovels");

            TempData["SearchString"] = searchString;
            TempData.Keep("SearchString");
            TempData["ResultFilterNovels"] = novels;
            return RedirectToAction("FilterNovels");
        }

        [HttpPost]
        public ActionResult Filter(int? sinceChapter, int? status, List<int> categories, DateTime? dateFrom, DateTime? dateTo)
        {
            TempData.Remove("SearchString");
            TempData["SinceChk"] = sinceChapter;
            TempData["StatusChk"] = status;
            TempData["CategoriesChk"] = categories;
            TempData["DateFromChk"] = dateFrom;
            TempData["DateToChk"] = dateTo;

            var novels = db.Novels.ToList();
            if (sinceChapter != null && sinceChapter != 1)
            {
                switch (sinceChapter)
                {
                    case 2:
                        novels = novels.Where(n=>n.Chapters.Count < 100).ToList();
                        break;
                    case 3:
                        novels = novels.Where(n => n.Chapters.Count >= 101 && n.Chapters.Count <= 200).ToList();
                        break;
                    case 4:
                        novels = novels.Where(n => n.Chapters.Count >= 201 && n.Chapters.Count <= 300).ToList();
                        break;
                    case 5:
                        novels = novels.Where(n => n.Chapters.Count >= 301 && n.Chapters.Count <= 400).ToList();
                        break;
                    case 6:
                        novels = novels.Where(n=>n.Chapters.Count > 400).ToList();
                        break;
                }
            }

            if (status != null && status != 0)
            {
                novels = novels.Where(n=>n.StatusID == status).ToList();
            }

            if (categories != null && categories.Count > 0)
            {
                novels = novels
                    .Where(n => 
                        categories.All(c => 
                            n.NovelCategories.Any(nc => nc.CategoryID == c)
                        )
                    )
                    .ToList();
            }
            
            if(dateFrom != null || dateTo != null)
            {
                if(dateFrom != null && dateTo == null)
                {
                    novels = novels.Where(n => n.CreatedAt >= dateFrom).OrderBy(n=>n.CreatedAt).ToList();
                }
                else if(dateFrom == null && dateTo != null)
                {
                    novels = novels.Where(n => n.CreatedAt <= dateTo).OrderByDescending(n=>n.CreatedAt).ToList();
                }
                else
                {
                    novels = novels.Where(n => n.CreatedAt >= dateFrom && n.CreatedAt <= dateTo).OrderBy(n=>n.CreatedAt).ToList();
                }
            }

            var contentToast = "Có " + novels.Count() + " kết quả được tìm thấy";
            Toast(false, contentToast);
            TempData["FilterNovels"] = novels;
            TempData.Keep("FilterNovels");
            TempData["ResultFilterNovels"] = novels;
            return RedirectToAction("FilterNovels");
        }

        [HttpPost]
        public ActionResult Sort(int sortType)
        {
            var novels = db.Novels.Where(n=>n.BlacklistedNovels.Count == 0).ToList();
            if(TempData["SearchNovels"] != null) novels = TempData["SearchNovels"] as List<Novel>;
            if(TempData["FilterNovels"] != null) novels = TempData["FilterNovels"] as List<Novel>;

            #region Sort
            
            switch (sortType)
            {
                case 1:
                    novels = novels.OrderBy(n => n.Title).ToList();
                    break;
                case 2:
                    novels = novels.OrderByDescending(n => n.Chapters.Count).ToList();
                    break;
                case 3:
                    novels = novels.OrderByDescending(n => n.Chapters.Sum(c => c.Views)).ToList();
                    break;
                case 4:
                    novels = novels.OrderByDescending(n => n.Chapters
                        .OrderByDescending(c => c.ChapterNumber)
                        .Select(c => c.CreatedAt)
                        .FirstOrDefault()).ToList();
                    break;
                case 5:
                    novels = novels.OrderByDescending(n => n.FavoriteNovels.Count).ToList();
                    break;
            }
            #endregion

            TempData["ResultFilterNovels"] = novels;
            TempData["SortTypeChk"] = sortType;
            return RedirectToAction("FilterNovels");
        }

        public class SortItem
        {
            public int Id { get; set; }
            public string Type { get; set; }
        }

        public class SinceChapter
        {
            public int Id { get; set; }
            public string Since { get; set; }
        }

        [CheckLoginAuthorize]
        public ActionResult FilterNovels(int? page)
        {
            #region checked form sort
            if(TempData["SortTypeChk"] != null) ViewBag.SortTypeChk = TempData["SortTypeChk"]; else ViewBag.SortTypeChk = 1;
            if(TempData["SinceChk"] != null) ViewBag.SinceChk = TempData["SinceChk"]; else ViewBag.SinceChk = 1;
            if(TempData["StatusChk"] != null) ViewBag.StatusChk = TempData["StatusChk"]; else ViewBag.StatusChk = 0;
            if(TempData["CategoriesChk"] != null) ViewBag.CategoriesChk = TempData["CategoriesChk"];
            #endregion

            var novelStatuses = db.NovelStatuses.ToList();
            novelStatuses.Add(new NovelStatus{ StatusID = 0, StatusName = "Toàn bộ" });
            ViewBag.NovelStatuses = novelStatuses;
            ViewBag.Categories = db.Categories.ToList();
            
            var sortItems = new List<SortItem>{
                new SortItem { Id = 1, Type = "Tên Truyện" },
                new SortItem { Id = 2, Type = "Số Chương" },
                new SortItem { Id = 3, Type = "Xem nhiều" },
                new SortItem { Id = 4, Type = "Mới Cập Nhật" },
                new SortItem { Id = 5, Type = "Yêu Thích" }
            };
            ViewBag.SortTypes = sortItems;

            var sinceChapter = new List<SinceChapter>{
                new SinceChapter { Id = 2, Since = "Dưới 100" },
                new SinceChapter { Id = 3, Since = "101 - 200" },
                new SinceChapter { Id = 4, Since = "201 - 300" },
                new SinceChapter { Id = 5, Since = "301 - 400" },
                new SinceChapter { Id = 6, Since = "Trên 400" },
                new SinceChapter { Id = 1, Since = "Toàn Bộ" },
            };
            ViewBag.SincesChapter = sinceChapter;

            var resultNovels = db.Novels.Where(n=>n.BlacklistedNovels.Count == 0).OrderBy(n=>n.Title).ToList();
            if (TempData["ResultFilterNovels"] != null) resultNovels = TempData["ResultFilterNovels"] as List<Novel>;

            if (page == null) page = 1;
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            ViewBag.pageSize = pageSize;
            ViewBag.CurrentPage = page;

            return View(resultNovels.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        public ActionResult IncreaseViews(int id)
        {
            var chapter = db.Chapters.Find(id);
            if(chapter != null)
            {
                chapter.Views++;
                db.Entry(chapter).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new {success = true, message = "successfully"}, JsonRequestBehavior.AllowGet);
            }
            return Json(new {success = false, message = "Failed"}, JsonRequestBehavior.AllowGet);
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