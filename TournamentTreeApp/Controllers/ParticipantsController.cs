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
    public class ParticipantsController : Controller
    {
        private TournamentTreeAppEntities db = new TournamentTreeAppEntities();

        // GET: Participants
        public ActionResult Index()
        {
            var participants = db.Participants.Include(p => p.School).Include(p => p.Tournament);
            return View(participants.ToList());
        }

        // GET: Participants/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Participant participant = db.Participants.Find(id);
            if (participant == null)
            {
                return HttpNotFound();
            }
            return View(participant);
        }


        // GET: Participants/Create
        [Authorize]
        public ActionResult CreateForSchool(Guid tournamentId, Guid schoolId)
        {
            if (!(User.IsInRole("Administrator") || User.IsInRole(tournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }




            var schools = db.Schools.Where(s => s.TournamentId == tournamentId).ToList();
            ViewBag.SchoolId = new SelectList(/*db.Schools*/schools, "SchoolId", "Name", schoolId);
            ViewBag.TournamentId = new SelectList(db.Tournaments.Where(t => t.TournamentId == tournamentId).ToList(), "TournamentId", "Name", tournamentId);
            ViewBag.RetournLinkId = schoolId;
            return View("Create");
        }

        // POST: Participants/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CreateForSchool([Bind(Include = "ParticipantId,Name,TournamentId,SchoolId,Dummy")] Participant participant)
        {
            if (ModelState.IsValid)
            {
                if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                participant.ParticipantId = Guid.NewGuid();
                db.Participants.Add(participant);
                db.SaveChanges();
                return RedirectToAction("Details", "Schools", new { id = participant.SchoolId });
            }

            ViewBag.SchoolId = new SelectList(db.Schools, "SchoolId", "Name", participant.SchoolId);
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", participant.TournamentId);
            return View(participant);
        }


        // GET: Participants/Create
        [Authorize]
        public ActionResult Create()
        {
            ViewBag.SchoolId = new SelectList(db.Schools, "SchoolId", "Name");
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name");
            if (!(User.IsInRole("Administrator") || User.IsInRole(ViewBag.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            return View();
        }

        // POST: Participants/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Create([Bind(Include = "ParticipantId,Name,TournamentId,SchoolId,Dummy")] Participant participant)
        {
            if (ModelState.IsValid)
            {
                if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                participant.ParticipantId = Guid.NewGuid();
                db.Participants.Add(participant);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.SchoolId = new SelectList(db.Schools, "SchoolId", "Name", participant.SchoolId);
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", participant.TournamentId);
            return View(participant);
        }

        // GET: Participants/Edit/5
        [Authorize]
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Participant participant = db.Participants.Find(id);
            if (participant == null)
            {
                return HttpNotFound();
            }
            if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            ViewBag.SchoolId = new SelectList(db.Schools, "SchoolId", "Name", participant.SchoolId);
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", participant.TournamentId);
            return View(participant);
        }

        // POST: Participants/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Edit([Bind(Include = "ParticipantId,Name,TournamentId,SchoolId,Dummy")] Participant participant)
        {
            if (ModelState.IsValid)
            {
                if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                db.Entry(participant).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "Schools", new { id = participant.SchoolId });
            }
            ViewBag.SchoolId = new SelectList(db.Schools, "SchoolId", "Name", participant.SchoolId);
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", participant.TournamentId);
            return View(participant);
        }

        // GET: Participants/Delete/5
        [Authorize]
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Participant participant = db.Participants.Find(id);
            if (participant == null)
            {
                return HttpNotFound();
            }
            if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            return View(participant);
        }

        // POST: Participants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteConfirmed(Guid id)
        {
            Participant participant = db.Participants.Find(id);
            if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            db.Participants.Remove(participant);
            db.SaveChanges();
            return RedirectToAction("Details", "Schools", new { id = participant.SchoolId });
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
