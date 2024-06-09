using NovelHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NovelHub.Controllers
{
    public class CategoryController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();
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
    }
}