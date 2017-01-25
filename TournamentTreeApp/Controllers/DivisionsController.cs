using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TournamentModels;
using System.Data.Entity;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using TournamentsTreeApp.Models;

namespace TournamentsTreeApp.Controllers
{

    public class DivisionsController : Controller
    {
        private TournamentTreeAppEntities db = new TournamentTreeAppEntities();

        // GET: Divisions
        public ActionResult Index()
        {
            var divisions = db.Divisions.Include(d => d.Tournament);
            return View(divisions.ToList());
        }

        [Authorize]
        public ActionResult Swap(string p1, string p2, string divisionId)
        {

            using (var context = new TournamentModels.TournamentTreeAppEntities())
            {
                var division = context.Divisions.FirstOrDefault(d => d.DivisionId == new Guid(divisionId));
                if (division == null)
                    return null;

                if (!(User.IsInRole("Administrator") || User.IsInRole(division.TournamentId.ToString().ToLower())))
                {
                    return Content("{\"result\": \"access denied!\"}");
                }

                var pdis = division.ParticipantDivisionInts;
                if (pdis.Count == 0) return null;

                //var process = pdis.Select(pdi => pdi.Participant.Name + "|" + pdi.Participant.School == null ? "" : pdi.Participant.School.Name).ToList();
                //var first = process.Where(p => p == p1);


                var partDivInt1 = pdis.FirstOrDefault(pdi => (pdi.Participant.Name + "|" + (pdi.Participant.School == null ? "" : pdi.Participant.School.Name)).Equals(p1, StringComparison.OrdinalIgnoreCase));
                var partDivInt2 = pdis.FirstOrDefault(pdi => (pdi.Participant.Name + "|" + (pdi.Participant.School == null ? "" : pdi.Participant.School.Name)).Equals(p2, StringComparison.OrdinalIgnoreCase));

                if (partDivInt1 == null || partDivInt2 == null)
                    return null;

                var temp = partDivInt1.OrderId;
                partDivInt1.OrderId = partDivInt2.OrderId;
                partDivInt2.OrderId = temp;
                context.SaveChanges();

                return Content("{\"result\": \"swapped!\"}");
            }


        }

        [HttpPost]
        [Authorize]
        public ActionResult SaveResults(string divisionId)
        {
            using (var context = new TournamentModels.TournamentTreeAppEntities())
            {
                var division = context.Divisions.FirstOrDefault(d => d.DivisionId == new Guid(divisionId));
                if (division == null)
                    return null;

                if (!(User.IsInRole("Administrator") || User.IsInRole(division.TournamentId.ToString().ToLower())))
                {
                    return Content("{\"result\": \"access denied!\"}");
                }

                using (StreamReader inputStream = new StreamReader(this.Request.InputStream))
                {
                    Init globalData = JsonConvert.DeserializeObject<Init>(inputStream.ReadToEnd());
                    division.Bracket = JsonConvert.SerializeObject(globalData);
                    context.SaveChanges();
                }

                return Content("{\"result\": \"results saved!\"}");
            }
        }

        [HttpGet]
        [Authorize]
        public ActionResult PlayerTransfer(Guid playerId, Guid divisionId, Guid currentDivisionId)
        {
            var currentDivision = db.Divisions.Find(currentDivisionId);
            var currentParticipantDivisionInts = currentDivision.ParticipantDivisionInts.FirstOrDefault(pdi => pdi.ParticipantId == playerId);
            if (currentParticipantDivisionInts != null)
            {
                var transferDivision = db.Divisions.Find(divisionId);
                if (transferDivision != null)
                {
                    if (!transferDivision.ParticipantDivisionInts.Any(item => item.ParticipantId == playerId))
                    {

                        var maxOrderID = transferDivision.ParticipantDivisionInts.Any() ? transferDivision.ParticipantDivisionInts.Max(pdi => pdi.OrderId) : 0;
                        transferDivision.ParticipantDivisionInts.Add(new ParticipantDivisionInt() { ParticipantDivisionIntId= Guid.NewGuid(), ParticipantId = playerId, DivisionId = divisionId, OrderId = maxOrderID + 1});
                        db.SaveChanges();

                    }
                }
                db.ParticipantDivisionInts.Remove(currentParticipantDivisionInts);
                db.SaveChanges();
            }
            return RedirectToAction("Details", "Divisions", new { id = currentDivisionId });
        }
        // GET: Divisions/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Division division = db.Divisions.Find(id);
            if (division == null)
            {
                return HttpNotFound();
            }

            Dictionary<Guid, List<SelectListItem>> transferLinks = new Dictionary<Guid, List<SelectListItem>>();
            foreach (var participant in division.OrderedParticipants)
            {
                List<SelectListItem> items = new List<SelectListItem>();
                items.Add(new SelectListItem { Text = "Select division for transfer...", Value = "" });
                foreach (var div in division.Tournament.Divisions.Where(div => div.DivisionId != division.DivisionId).OrderBy(div => div.Name))
                {
                    items.Add(new SelectListItem
                    {
                        Text = div.Name,
                        Value = "/Divisions/PlayerTransfer?playerId=" + participant.ParticipantId.ToString() + "&divisionId=" + div.DivisionId.ToString() + "&currentDivisionId=" + id.ToString()
                    });
                }

                transferLinks.Add(participant.ParticipantId, items);
            }

            division.LinkHelper = transferLinks;
            return View(division);
        }


        [Authorize]
        // GET: Divisions/Create
        public ActionResult CreateForTournament(Guid tournamentId)
        {
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", tournamentId);
            if (!(User.IsInRole("Administrator") || User.IsInRole(tournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }

            //var tournament = db.Tournaments.Where(t => t.TournamentId == tournamentId);
            //var s = tournament.Select(t => t.ConsolidationRound).ToList().FirstOrDefault();
            //var m = (bool?)s;

            ViewBag.DefaultConsolidationRound = (bool?)db.Tournaments.Where(t => t.TournamentId == tournamentId).Select(t => t.ConsolidationRound).ToList().FirstOrDefault();
            ViewBag.RetournLinkId = tournamentId;
            return View("Create");
        }

        // POST: Divisions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CreateForTournament([Bind(Include = "DivisionId,Id,Name,Title,TournamentId,DrawBracket,ConsolidationRound,Bracket,OrderId")] Division division)
        {
            if (ModelState.IsValid)
            {
                if (!(User.IsInRole("Administrator") || User.IsInRole(division.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                division.DivisionId = Guid.NewGuid();
                var maxId = db.Divisions.Where(d => d.TournamentId == division.TournamentId).Select(d => d.OrderId).Max();
                //var maxId = db.Divisions.Where(d => d.TournamentId == division.TournamentId).Max(d => TryParse(d.Id));
                division.Id = division.DivisionId.ToString();
                division.OrderId = (maxId ?? 0) + 1;
                db.Divisions.Add(division);
                db.SaveChanges();
                return RedirectToAction("Details", "Tournaments", new { id = division.TournamentId });
            }

            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", division.TournamentId);
            return View(division);
        }

        private int TryParse(string id)
        {
            int outval = -1;
            int.TryParse(id, out outval);
            return outval;
        }

        [Authorize]
        // GET: Divisions/Create
        public ActionResult Create()
        {
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name");

            if (!(User.IsInRole("Administrator") || User.IsInRole(ViewBag.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }

            return View();
        }

        // POST: Divisions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Create([Bind(Include = "DivisionId,Id,Name,Title,TournamentId,DrawBracket,ConsolidationRound,Bracket,OrderId")] Division division)
        {
            if (ModelState.IsValid)
            {
                if (!(User.IsInRole("Administrator") || User.IsInRole(division.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }

                division.DivisionId = Guid.NewGuid();
                db.Divisions.Add(division);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", division.TournamentId);
            return View(division);
        }

        // GET: Divisions/Edit/5
        [Authorize]
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Division division = db.Divisions.Find(id);
            if (division == null)
            {
                return HttpNotFound();
            }

            if (!(User.IsInRole("Administrator") || User.IsInRole(division.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }

            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", division.TournamentId);
            return View(division);
        }

        // POST: Divisions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Edit([Bind(Include = "DivisionId,Id,Name,Title,TournamentId,DrawBracket,ConsolidationRound,Bracket,OrderId")] Division division)
        {
            if (ModelState.IsValid)
            {

                if (!(User.IsInRole("Administrator") || User.IsInRole(division.TournamentId.ToString().ToLower())))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }

                db.Entry(division).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "Tournaments", new { id = division.TournamentId });
            }
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", division.TournamentId);
            return View(division);
        }

        // GET: Divisions/Delete/5
        [Authorize]
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Division division = db.Divisions.Find(id);
            if (division == null)
            {
                return HttpNotFound();
            }

            if (!(User.IsInRole("Administrator") || User.IsInRole(division.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }

            return View(division);
        }

        // POST: Divisions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteConfirmed(Guid id)
        {
            Division division = db.Divisions.Find(id);
            if (!(User.IsInRole("Administrator") || User.IsInRole(division.TournamentId.ToString().ToLower())))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            db.Divisions.Remove(division);
            db.SaveChanges();
            return RedirectToAction("Details", "Tournaments", new { id = division.TournamentId });
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