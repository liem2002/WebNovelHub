using NovelHub.App_Start;
using NovelHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Areas.Admin.Controllers
{
    public class AdminCategoriesController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();

        [AdminAuthorize]
        public ActionResult ManagerCategories()
        {
            ViewBag.CountCategories = db.Categories.Count();
            return View();
        }

        [AdminAuthorize]
        public ActionResult RenderAllCategories()
        {
            var AllCategories = db.Categories.ToList();

            var formattedCategoriesList = AllCategories.Select(category => new
            {
                CategoryID = category.CategoryID,
                CategoryName = category.CategoryName
            });;

            return Json(formattedCategoriesList, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult AddNewCategory([Bind(Include = "CategoryID,CategoryName")] Category category)
        {
            var existCategory = db.Categories.SingleOrDefault(c => c.CategoryName == category.CategoryName);
            if (ModelState.IsValid && existCategory == null && category.CategoryName != null)
            {
                db.Categories.Add(category);
                db.SaveChanges();
                var categories = db.Categories.OrderByDescending(c => c.CategoryName).ToList();
                Toast(false, "Thêm thành công");
            return RedirectToAction("ManagerCategories");
            }
            TempData["ErrorAddNewCategory"] = true;
            if(category.CategoryName == null) TempData["ErrorCategoryName"] = "Trường này không được để trống";
            if(existCategory != null) TempData["ErrorCategoryName"] = "Thể Loại vừa nhập đã tồn tại";
            Toast(true, "Thêm thất bại");
            return RedirectToAction("ManagerCategories");
        }
        
        [AdminAuthorize]
        public ActionResult DeleteCategory(int id)
        {
            Category category = db.Categories.Find(id);
            db.Categories.Remove(category);
            db.SaveChanges();
            Toast(false, "Xóa thành công");
            return RedirectToAction("ManagerCategories");
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