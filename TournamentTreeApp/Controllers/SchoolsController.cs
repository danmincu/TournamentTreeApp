using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TournamentModels;

namespace TournamentsTreeApp.Controllers
{
    public class SchoolsController : Controller
    {
        private TournamentTreeAppEntities db = new TournamentTreeAppEntities();

        // GET: Schools
        public ActionResult Index()
        {
            var schools = db.Schools.Include(s => s.Tournament);
            return View(schools.ToList());
        }

        // GET: Schools/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            School school = db.Schools.Find(id);
            if (school == null)
            {
                return HttpNotFound();
            }
            return View(school);
        }

        // GET: Schools/Create
        [Authorize]
        public ActionResult Create()
        {
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name");
            if (!(User.IsInRole("Administrator") || User.IsInRole(ViewBag.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            return View();
        }


        // GET: Schools/Create
        [Authorize]
        public ActionResult CreateForTournament(Guid tournamentId)
        {   
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", tournamentId);
            if (!(User.IsInRole("Administrator") || User.IsInRole(tournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            ViewBag.RetournLinkId = tournamentId;
            return View("Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CreateForTournament([Bind(Include = "SchoolId,Name,TournamentId")] School school)
        {
            if (ModelState.IsValid)
            {
                if (!(User.IsInRole("Administrator") || User.IsInRole(school.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }

                school.SchoolId = Guid.NewGuid();
                db.Schools.Add(school);
                db.SaveChanges();
                return RedirectToAction("Details", "Tournaments", new { id = school.TournamentId });
            }

            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", school.TournamentId);
            return View(school);
        }

        // POST: Schools/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Create([Bind(Include = "SchoolId,Name,TournamentId")] School school)
        {
            if (ModelState.IsValid)
            {
                if (!(User.IsInRole("Administrator") || User.IsInRole(school.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                school.SchoolId = Guid.NewGuid();
                db.Schools.Add(school);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", school.TournamentId);
            return View(school);
        }

        // GET: Schools/Edit/5
        [Authorize]
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            School school = db.Schools.Find(id);
            if (school == null)
            {
                return HttpNotFound();
            }
            if (!(User.IsInRole("Administrator") || User.IsInRole(school.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", school.TournamentId);
            return View(school);
        }

        // POST: Schools/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Edit([Bind(Include = "SchoolId,Name,TournamentId")] School school)
        {
            if (ModelState.IsValid)
            {
                if (!(User.IsInRole("Administrator") || User.IsInRole(school.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                db.Entry(school).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "Tournaments", new { id = school.TournamentId });
            }
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", school.TournamentId);
            return View(school);
        }

        // GET: Schools/Delete/5
        [Authorize]
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            School school = db.Schools.Find(id);
            if (school == null)
            {
                return HttpNotFound();
            }
            if (!(User.IsInRole("Administrator") || User.IsInRole(school.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            return View(school);
        }

        // POST: Schools/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteConfirmed(Guid id)
        {
            School school = db.Schools.Find(id);
            if (!(User.IsInRole("Administrator") || User.IsInRole(school.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            db.Schools.Remove(school);
            db.SaveChanges();
            return RedirectToAction("Details", "Tournaments", new { id = school.TournamentId });
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
