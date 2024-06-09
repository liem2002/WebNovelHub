using NovelHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Controllers
{
    public class AccountController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register([Bind(Include = "UserID,RoleID,Username,Password,Email,Avatar,Background,CreatedAt")] User user, string rePass)
        {
            // Đặt giá trị mặc định
            user.RoleID = 3;
            user.Avatar = null;
            user.Background = null;
            user.CreatedAt = DateTime.Now;

            // Kiểm tra có trùng email người dùng khác không
            var userdb = db.Users.SingleOrDefault(m => m.Email == user.Email);

            if (ModelState.IsValid && user.Password == rePass && userdb == null)
            {
                db.Users.Add(user);
                db.SaveChanges();
                Toast(false, "Đăng ký thành công");
                return Redirect("~/Home/Index");
            }
            TempData["registerError"] = true;
            if(user.Username == null) TempData["UsernameErrorMessage"] = "Trường này không được để trống";
            if(user.Email == null || userdb != null) TempData["EmailErrorMessage"] = "Email đã tồn tại hoặc không chính xác";
            if(user.Password == null) TempData["PasswordErrorMessage"] = "Trường này không được để trống";
            if(user.Password != rePass) TempData["PasswordErrorMessage"] = "Mật khẩu nhập lại không chính xác";
            Toast(true, "Đăng ký không thành công");
            return Redirect("~/Home/Index");
        }

        [ValidateAntiForgeryToken]
        public ActionResult Login(string InputEmail, string inputPassword, int? RememberMe)
        {
            var user = db.Users.SingleOrDefault(m => m.Email == InputEmail && m.Password == inputPassword);
            if (user != null)
            {
                Session["User"] = user;

                if (RememberMe == 1)
                {
                    HttpCookie cookie = new HttpCookie("LoginCookie");
                    cookie.Values["Email"] = InputEmail;
                    cookie.Values["Password"] = inputPassword;
                    //cookie.Expires = DateTime.Now.AddMinutes(120);
                    cookie.Expires = DateTime.Now.AddDays(30);
                    Response.Cookies.Add(cookie);
                }
                Toast(false, "Đăng nhập thành công");
                return Redirect("~/Home/Index");
            }
            else
            {
                TempData["loginError"] = true;
                TempData["LoginErrorMessage"] = "Email hoặc mật khẩu không chính xác";
                Toast(true, "Đăng nhập không thành công");
                return Redirect("~/Home/Index");
            }
        }

        public ActionResult LogOut()
        {
            // Xóa Session
            Session.Clear();

            // Xóa cookie
            if (Request.Cookies["LoginCookie"] != null)
            {
                var cookie = new HttpCookie("LoginCookie");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }
            return Redirect("~/Home/Index");
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