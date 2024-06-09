using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NovelHub.Models;

namespace NovelHub.Areas.Admin.Controllers
{
    public class NotificationsController : Controller
    {
        private NovelHubEntities db = new NovelHubEntities();

        // GET: Admin/Notifications
        public ActionResult Index()
        {
            ViewBag.NotificationNotRead = db.Notifications.Where(x => x.ReadStatus == false).ToList().Count;
            
            var notifications = db.Notifications.Include(n => n.User);
            return View(notifications.ToList());
        }

        public ActionResult CountReadStatusFalse()
        {
            var notificationNotRead = db.Notifications.Where(x => x.ReadStatus == false).ToList().Count;
            return Content(notificationNotRead.ToString());
        }

        // GET: Admin/Notifications/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ViewBag.Roles = db.Roles.ToList();
            Notification notification = db.Notifications.Find(id);
            notification.ReadStatus = true;
            
            //Lưu data về sql server
            db.Entry(notification).State = EntityState.Modified;
            db.SaveChanges();

            if (notification == null)
            {
                return HttpNotFound();
            }
            ViewBag.Notification = notification;
            return View(notification);
        }

        //// GET: Admin/Notifications/Create
        //public ActionResult Create()
        //{
        //    ViewBag.UserID = new SelectList(db.Users, "UserID", "Username");
        //    return View();
        //}

        //// POST: Admin/Notifications/Create
        //// To protect from overposting attacks, enable the specific properties you want to bind to, for 
        //// more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create([Bind(Include = "NotificationID,UserID,Content,Timestamp,ReadStatus")] Notification notification)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Notifications.Add(notification);
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }

        //    ViewBag.UserID = new SelectList(db.Users, "UserID", "Username", notification.UserID);
        //    return View(notification);
        //}

        [HttpGet]
        //[ValidateAntiForgeryToken]
        public ActionResult Create(int userID)
        {
            var checkAllow = db.Notifications.Find(userID);
            if(checkAllow == null)
            {
                Notification notification = new Notification();
                notification.UserID = userID;
                notification.Content = "Cấp quyền cho người dùng có id = " + userID;
                notification.Timestamp = DateTime.Now;
                notification.ReadStatus = false;

                if (ModelState.IsValid)
                {
                    db.Notifications.Add(notification);
                    db.SaveChanges();
                    return Redirect("/AreaUser/AUManagerProfile/UserProfile/" + userID);
                }

                ViewBag.UserID = new SelectList(db.Users, "UserID", "Username", notification.UserID);
                return View(notification);
            }
            else
            {
                return Redirect("/AreaUser/AUManagerProfile/UserProfile/" + userID);
            }
            
        }

        // GET: Admin/Notifications/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Notification notification = db.Notifications.Find(id);
            if (notification == null)
            {
                return HttpNotFound();
            }
            ViewBag.UserID = new SelectList(db.Users, "UserID", "Username", notification.UserID);
            return View(notification);
        }

        // POST: Admin/Notifications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "NotificationID,UserID,Content,Timestamp,ReadStatus")] Notification notification)
        {
            if (ModelState.IsValid)
            {
                db.Entry(notification).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UserID = new SelectList(db.Users, "UserID", "Username", notification.UserID);
            return View(notification);
        }

        // GET: Admin/Notifications/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Notification notification = db.Notifications.Find(id);
            if (notification == null)
            {
                return HttpNotFound();
            }
            return View(notification);
        }

        // POST: Admin/Notifications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Notification notification = db.Notifications.Find(id);
            db.Notifications.Remove(notification);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
