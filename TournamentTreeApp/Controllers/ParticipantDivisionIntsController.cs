using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TournamentModels;
using TournamentsTreeApp.Models;

namespace TournamentsTreeApp.Controllers
{
    public class ParticipantDivisionIntsController : Controller
    {
        private TournamentTreeAppEntities db = new TournamentTreeAppEntities();

        // GET: ParticipantDivisionInts
        public ActionResult Index()
        {
            var participantDivisionInts = db.ParticipantDivisionInts.Include(p => p.Division).Include(p => p.Participant);
            return View(participantDivisionInts.ToList());
        }

        // GET: ParticipantDivisionInts/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ParticipantDivisionInt participantDivisionInt = db.ParticipantDivisionInts.Find(id);
            if (participantDivisionInt == null)
            {
                return HttpNotFound();
            }
            return View(participantDivisionInt);
        }


        // GET
        [Authorize]
        public ActionResult CreateForDivisionFast(Guid divisionId, Guid tournamentId)
        {
            if (!(User.IsInRole("Administrator") || User.IsInRole(tournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            
            var schools = db.Schools.Where(s => s.TournamentId == tournamentId).ToList();
            schools.Insert(0, new School() { SchoolId = Guid.Empty, Name = "Select a school or type new name..." });
            ViewBag.SchoolId = new SelectList(schools, "SchoolId", "Name");
            ViewBag.RetournLinkId = divisionId;
            return View();
        }

        // POST
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CreateForDivisionFast([Bind(Include = "SchoolId,SchoolName,ParticipantName,DivisionId,TournamentId")] FastParticipant fastParticipant)
        {
            if (string.IsNullOrEmpty(fastParticipant.ParticipantName) || (fastParticipant.SchoolId == Guid.Empty && string.IsNullOrEmpty(fastParticipant.SchoolName)))
                return RedirectToAction("Details", "Divisions", new { id = fastParticipant.DivisionId });

            var tournament = db.Tournaments.Include(t => t.Schools).Include(t => t.Participants).Where(t => t.TournamentId == fastParticipant.TournamentId).FirstOrDefault();
            School school = null;
            if (fastParticipant.SchoolId == Guid.Empty)
            {
                school = tournament.Schools.FirstOrDefault(s => s.Name.Equals(fastParticipant.SchoolName, StringComparison.OrdinalIgnoreCase));
                if (school == null)
                {
                    school = new School
                    {
                        Name = fastParticipant.SchoolName,
                        SchoolId = Guid.NewGuid(),
                        Tournament = tournament,
                        TournamentId = fastParticipant.TournamentId
                    };
                    tournament.Schools.Add(school);
                };
            }
            else
            {
                school = db.Schools.FirstOrDefault(s => s.SchoolId == fastParticipant.SchoolId);
            }

            var participant = new Participant
            {
                Name = fastParticipant.ParticipantName,
                School = school,
                SchoolId = school.SchoolId,
                Tournament = tournament,
                TournamentId = fastParticipant.TournamentId,
                Dummy = false,
                ParticipantId = Guid.NewGuid()
            };


            school.Participants.Add(participant);
            tournament.Participants.Add(participant);
            db.SaveChanges();

            var transferDivision = db.Divisions.Find(fastParticipant.DivisionId);
            if (transferDivision != null)
            {
                var maxOrderID = transferDivision.ParticipantDivisionInts.Any() ? transferDivision.ParticipantDivisionInts.Max(pdi => pdi.OrderId) : 0;
                transferDivision.ParticipantDivisionInts.Add(new ParticipantDivisionInt()
                {
                    ParticipantDivisionIntId = Guid.NewGuid(),
                    ParticipantId = participant.ParticipantId,
                    DivisionId = fastParticipant.DivisionId,
                    OrderId = maxOrderID + 1
                });
                db.SaveChanges();
            }

            return RedirectToAction("Details", "Divisions", new { id = fastParticipant.DivisionId });
        }



        // GET: ParticipantDivisionInts/Create
        [Authorize]
        public ActionResult CreateForDivision(Guid divisionId, Guid tournamentId)
        {
            if (!(User.IsInRole("Administrator") || User.IsInRole(tournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            ViewBag.DivisionId = new SelectList(db.Divisions, "DivisionId", "Id", divisionId);
            var division = db.Divisions.Where(d => d.DivisionId == divisionId).FirstOrDefault();
            var participants = db.Participants.Where(p => p.TournamentId == tournamentId).OrderBy(p => p.Name);
            var list = new List<Participant>();
            if (division != null)
            {
                foreach (var participant in participants)
                {
                    if (!division.ParticipantDivisionInts.Any(pdi => pdi.ParticipantId == participant.ParticipantId))
                        list.Add(participant);
                }

                ViewBag.ParticipantId = new SelectList(list, "ParticipantId", "Name");
            }
            else
                ViewBag.ParticipantId = new SelectList(participants, "ParticipantId", "Name");
            ViewBag.RetournLinkId = divisionId;
            return View("Create");
        }

        // POST: ParticipantDivisionInts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CreateForDivision([Bind(Include = "ParticipantDivisionIntId,ParticipantId,DivisionId,OrderId")] ParticipantDivisionInt participantDivisionInt)
        {

            if (participantDivisionInt.ParticipantId == Guid.Empty)
                return RedirectToAction("Details", "Divisions", new { id = participantDivisionInt.DivisionId });
            if (ModelState.IsValid)
            {
                Participant participant = db.Participants.Find(participantDivisionInt.ParticipantId);
                if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                participantDivisionInt.ParticipantDivisionIntId = Guid.NewGuid();
                db.ParticipantDivisionInts.Add(participantDivisionInt);
                db.Divisions.Where(d => d.DivisionId == participantDivisionInt.DivisionId).First().Bracket = null;
                db.SaveChanges();
                return RedirectToAction("Details", "Divisions", new { id = participantDivisionInt.DivisionId });
            }

            ViewBag.DivisionId = new SelectList(db.Divisions, "DivisionId", "Id", participantDivisionInt.DivisionId);
            ViewBag.ParticipantId = new SelectList(db.Participants, "ParticipantId", "Name", participantDivisionInt.ParticipantId);
            return View(participantDivisionInt);
        }


        // GET: ParticipantDivisionInts/Create
        [Authorize]
        public ActionResult Create()
        {
            ViewBag.DivisionId = new SelectList(db.Divisions, "DivisionId", "Id");
            ViewBag.ParticipantId = new SelectList(db.Participants, "ParticipantId", "Name");
            return View();
        }

        // POST: ParticipantDivisionInts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Create([Bind(Include = "ParticipantDivisionIntId,ParticipantId,DivisionId,OrderId")] ParticipantDivisionInt participantDivisionInt)
        {
            if (ModelState.IsValid)
            {
                Participant participant = db.Participants.Find(participantDivisionInt.ParticipantId);
                if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                participantDivisionInt.ParticipantDivisionIntId = Guid.NewGuid();
                db.ParticipantDivisionInts.Add(participantDivisionInt);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.DivisionId = new SelectList(db.Divisions, "DivisionId", "Id", participantDivisionInt.DivisionId);
            ViewBag.ParticipantId = new SelectList(db.Participants, "ParticipantId", "Name", participantDivisionInt.ParticipantId);
            return View(participantDivisionInt);
        }

        // GET: ParticipantDivisionInts/Edit/5
        [Authorize]
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ParticipantDivisionInt participantDivisionInt = db.ParticipantDivisionInts.Find(id);
            if (participantDivisionInt == null)
            {
                return HttpNotFound();
            }
            Participant participant = db.Participants.Find(participantDivisionInt.ParticipantId);
            if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            ViewBag.DivisionId = new SelectList(db.Divisions, "DivisionId", "Id", participantDivisionInt.DivisionId);
            ViewBag.ParticipantId = new SelectList(db.Participants, "ParticipantId", "Name", participantDivisionInt.ParticipantId);
            return View(participantDivisionInt);
        }

        // POST: ParticipantDivisionInts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Edit([Bind(Include = "ParticipantDivisionIntId,ParticipantId,DivisionId,OrderId")] ParticipantDivisionInt participantDivisionInt)
        {
            if (ModelState.IsValid)
            {
                Participant participant = db.Participants.Find(participantDivisionInt.ParticipantId);
                if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                db.Entry(participantDivisionInt).State = EntityState.Modified;
                db.Divisions.Where(d => d.DivisionId == participantDivisionInt.DivisionId).First().Bracket = null;
                db.SaveChanges();
                return RedirectToAction("Details", "Divisions", new { id = participantDivisionInt.DivisionId });
            }
            ViewBag.DivisionId = new SelectList(db.Divisions, "DivisionId", "Id", participantDivisionInt.DivisionId);
            ViewBag.ParticipantId = new SelectList(db.Participants, "ParticipantId", "Name", participantDivisionInt.ParticipantId);
            return View(participantDivisionInt);
        }

        // GET: ParticipantDivisionInts/Delete/5
        [Authorize]
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ParticipantDivisionInt participantDivisionInt = db.ParticipantDivisionInts.Find(id);
            if (participantDivisionInt == null)
            {
                return HttpNotFound();
            }
            Participant participant = db.Participants.Find(participantDivisionInt.ParticipantId);
            if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            return View(participantDivisionInt);
        }

        // POST: ParticipantDivisionInts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteConfirmed(Guid id)
        {
            ParticipantDivisionInt participantDivisionInt = db.ParticipantDivisionInts.Find(id);
            Participant participant = db.Participants.Find(participantDivisionInt.ParticipantId);
            if (!(User.IsInRole("Administrator") || User.IsInRole(participant.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            db.ParticipantDivisionInts.Remove(participantDivisionInt);
            db.Divisions.Where(d => d.DivisionId == participantDivisionInt.DivisionId).First().Bracket = null;
            db.SaveChanges();
            return RedirectToAction("Details", "Divisions", new { id = participantDivisionInt.DivisionId });
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
