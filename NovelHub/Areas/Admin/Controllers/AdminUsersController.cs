using NovelHub.App_Start;
using NovelHub.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Areas.Admin.Controllers
{
    public class AdminUsersController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();
        [AdminAuthorize]
        public ActionResult ManagerUsers(int? page)
        {
            if(page == null) page = 1;
            // truy van list
            var AllUsers = db.Users.ToList();
            int pageSize = 10;
            
            ViewBag.pageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.CountUsers = db.Users.Count();

            ViewBag.isError = TempData["isError"];
            ViewBag.Color = TempData["Color"];
            ViewBag.ToastContent = TempData["ToastContent"];

            ViewBag.CurrtentUser = (User)HttpContext.Session["User"];

            #region Đăng ký lỗi
            ViewBag.registerError = TempData["registerError"];
            ViewBag.ErrorUsername = TempData["ErrorUsername"];
            ViewBag.ErrorEmail = TempData["ErrorEmail"];
            ViewBag.ErrorPassword = TempData["ErrorPassword"];
            ViewBag.ErrorRePassword = TempData["ErrorRePassword"];
            ViewBag.userInput = TempData["userInput"];
            #endregion

            ViewBag.Roles = db.Roles.ToList();

            int pageNumber = (page ?? 1);
            return View(AllUsers.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult AddBlackUser([Bind(Include = "BlacklistedUserID,UserID,CreatedAt,Note")] BlacklistedUser blacklistedUser)
        {
            blacklistedUser.CreatedAt = DateTime.Now;
            if (ModelState.IsValid)
            {
                db.BlacklistedUsers.Add(blacklistedUser);
                db.SaveChanges();
                Toast(false, "Đã khóa thành công");
                return RedirectToAction("ManagerUsers");
            }
            return View(blacklistedUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult RemoveBlackUser(int userID)
        {
            var blackUser = db.BlacklistedUsers.SingleOrDefault(b=>b.UserID == userID);
            db.BlacklistedUsers.Remove(blackUser);
            db.SaveChanges();
            Toast(false, "Đã mở khóa thành công");
            return RedirectToAction("ManagerUsers");
        }

        

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult ChangeUserRole(int userID, int Role)
        {
            var updateUser = db.Users.Find(userID);
            updateUser.RoleID = Role;
            db.Entry(updateUser).State = EntityState.Modified;
            db.SaveChanges();
            Toast(false, "Đã thay đổi quyền thành công");
            return RedirectToAction("ManagerUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult DeleteUser(int? id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();
            Toast(false, "Đã xóa thành công");
            return RedirectToAction("ManagerUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult AddNewUser([Bind(Include = "UserID,RoleID,Username,Password,Email,Avatar,Background,CreatedAt")] User user, string rePass)
        {
            // Đặt giá trị mặc định
            user.Avatar = null;
            user.Background = null;
            user.CreatedAt = DateTime.Now;

            // Kiểm tra có trùng email người dùng khác không
            var userdb = db.Users.SingleOrDefault(m => m.Email == user.Email);

            if (ModelState.IsValid && user.Password == rePass && userdb == null)
            {
                db.Users.Add(user);
                db.SaveChanges();
                Toast(false, "Thêm tài khoản thành công");
                return RedirectToAction("ManagerUsers");
            }

            TempData["registerError"] = true;
            Toast(true, "Thêm tài khoản thất bại");

            if(user.Username == null) TempData["ErrorUsername"] = "Vui lòng nhập trường này";
            if(user.Email == null || userdb != null) TempData["ErrorEmail"] = "Email đã tồn tại hoặc không chính xác";
            if(user.Password == null) TempData["ErrorPassword"] = "Vui lòng nhập trường này"; 
            if(rePass == null) TempData["ErrorRePassword"] = "Vui lòng nhập trường này";
            else if(user.Password != rePass) TempData["ErrorRePassword"] = "Mật khẩu xác nhận không chính xác";

            return RedirectToAction("ManagerUsers");
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