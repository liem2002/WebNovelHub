using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NovelHub.App_Start;
using NovelHub.Models;
using PagedList;
using System.Data.Entity;
using System.IO;
using System.Net;

namespace NovelHub.Areas.AreaUser.Controllers
{
    public class AUManagerProfileController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();
        [UserAuthorize]
        public ActionResult UserProfile(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [UserAuthorize]
        public ActionResult UserProfile([Bind(Include = "UserID,RoleID,Username,Password,Email,Avatar,Background,CreatedAt")] User user, HttpPostedFileBase avatar, HttpPostedFileBase background, string NewPassword, string RePassword)
        {
            var CurrentUser = (User)HttpContext.Session["User"];
            // Default value
            user.CreatedAt = CurrentUser.CreatedAt;
            user.RoleID = CurrentUser.RoleID;

            if (ModelState.IsValid && user.Password == CurrentUser.Password)
            {
                #region Xử lý ảnh
                //Lưu avatar
                if (avatar != null && avatar.ContentLength > 0)
                {
                    // Xóa ảnh
                    if (CurrentUser.Avatar != null)
                    {
                        var relativeImagePath = "~/Contents/img/Avatar/" + CurrentUser.Avatar;
                        var serverPath = Server.MapPath(relativeImagePath);
                        System.IO.File.Delete(serverPath);
                    }
                    // Lưu ảnh
                    var fileName = CurrentUser.UserID + "_Avatar_" + Path.GetFileName(avatar.FileName);
                    var imagePath = Path.Combine(Server.MapPath("~/Contents/img/Avatar/"), fileName);
                    avatar.SaveAs(imagePath);
                    user.Avatar = fileName;
                }
                else
                {
                    user.Avatar = CurrentUser.Avatar;
                }


                // Lưu background
                if (background != null && background.ContentLength > 0)
                {
                    // Xóa ảnh
                    if (CurrentUser.Background != null)
                    {
                        var relativeImagePath = "~/Contents/img/Background/" + CurrentUser.Background;
                        var serverPath = Server.MapPath(relativeImagePath);
                        System.IO.File.Delete(serverPath);
                    }
                    // Lưu ảnh
                    var fileName = CurrentUser.UserID + "_Background_" + Path.GetFileName(background.FileName);
                    var imagePath = Path.Combine(Server.MapPath("~/Contents/img/Background/"), fileName);
                    background.SaveAs(imagePath);
                    user.Background = fileName;
                }
                else
                {
                    user.Background = CurrentUser.Background;
                }
                #endregion

                #region Xử lý khi thay đổi mật khẩu
                //Lưu password , xóa session và xóa cookie
                if (NewPassword != "" && RePassword == NewPassword)
                {
                    user.Password = NewPassword;
                    Session.Clear();
                    if (Request.Cookies["LoginCookie"] != null)
                    {
                        var cookie = new HttpCookie("LoginCookie");
                        cookie.Expires = DateTime.Now.AddDays(-1);
                        Response.Cookies.Add(cookie);
                    }
                    // Cập nhật session
                    var UpdateUser = db.Users.SingleOrDefault(u=>u.UserID == user.UserID);
                    Session["User"] = UpdateUser;
                }
                #endregion 
                // Lưu db
                CurrentUser = null;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                Session["User"] = user;
                Toast(false, "Cập nhật thành công");
                return RedirectToAction("UserProfile", new { id = user.UserID });
            }
            else
            {
                #region Xử lý thông báo

                // Xử lý thông báo lỗi về input
                if (RePassword != NewPassword && NewPassword != "")
                {
                    TempData["MessRePass"] = "Mật khẩu xác nhận không chính xác";
                }

                if(RePassword != NewPassword && NewPassword == "")
                {
                    TempData["MessNewPass"] = "Vui lòng nhập trường này";
                }
                if(user.Password != CurrentUser.Password)
                {
                    TempData["MessPass"] = "Vui lòng nhập trường này";
                    TempData["Border"] = "border: solid 2px #dc3545";
                }
                #endregion
                Toast(true, "Cập nhật thất bại");
                return RedirectToAction("UserProfile", new { id = user.UserID });
            }
        }
        
        [UserAuthorize]
        public ActionResult AddFavoriteNovel(int id)
        {
            FavoriteNovel favoriteNovel = new FavoriteNovel();
            var currentUser = (User)HttpContext.Session["User"];
            favoriteNovel.FavoritedAt = DateTime.Now;
            favoriteNovel.UserID = currentUser.UserID;
            favoriteNovel.NovelID = id;

            db.FavoriteNovels.Add(favoriteNovel);
            db.SaveChanges();
            return Json(new { success = true, message = "Theo dõi thành công." }, JsonRequestBehavior.AllowGet);
        }
        
        [UserAuthorize]
        public ActionResult RemoveFavoriteNovel(int id)
        {
            var currentUser = (User)HttpContext.Session["User"];
            var favoriteNovel = db.FavoriteNovels.SingleOrDefault(fn=>fn.NovelID == id && fn.UserID == currentUser.UserID);
            db.FavoriteNovels.Remove(favoriteNovel);
            db.SaveChanges();
            return Json(new { success = true, message = "Đã bỏ theo dõi thành công." }, JsonRequestBehavior.AllowGet);
        }

        [UserAuthorize]
        public ActionResult FollowedNovels(int? page)
        {
            var user = (User)HttpContext.Session["User"];
            var favoriteNovels = db.FavoriteNovels
                        .Where(fn => fn.UserID == user.UserID).OrderByDescending(n=>n.FavoritedAt)
                        .Select(fn => fn.Novel)
                        .ToList();

            if (page == null) page = 1;
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(favoriteNovels.ToPagedList(pageNumber, pageSize));
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