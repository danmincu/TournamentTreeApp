using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using TournamentModels;
using TournamentTreeApp.Models;

namespace TournamentsTreeApp.Controllers
{
    public class TournamentsController : Controller
    {
        private TournamentTreeAppEntities db = new TournamentTreeAppEntities();

        // GET: Tournaments
        public ActionResult Index()
        {
            return View(db.Tournaments.ToList());
        }


        // GET: Tournaments/Details/5
        public ActionResult Copy(Guid tournamentId)
        {
            var tournament = db.Tournaments
                .Include(t => t.Schools)
                .Include(t => t.Participants)
                .Include(t => t.Divisions)
                .AsNoTracking()
                .FirstOrDefault(e => e.TournamentId == tournamentId);

            if (tournament == null)
            {
                return HttpNotFound();
            }

            var copiedTournament = new Tournament();

            copiedTournament.Name = String.Format("Copy of ({0})", tournament.Name.Replace("{", "(").Replace("}", ")"));

            copiedTournament.TournamentId = Guid.NewGuid();


            Dictionary<Guid, Participant> exchangeKeysDrawer = new Dictionary<Guid, Participant>();


            foreach (var sch in tournament.Schools)
            {
                var school = new School()
                {
                    Name = sch.Name,
                    SchoolId = Guid.NewGuid(),
                    TournamentId = copiedTournament.TournamentId,
                    Tournament = copiedTournament
                };

                copiedTournament.Schools.Add(school);

                foreach (var part in sch.Participants)
                {
                    var participant = new Participant()
                    {
                        ParticipantId = Guid.NewGuid(),
                        Name = part.Name,
                        SchoolId = school.SchoolId,
                        School = school,
                        TournamentId = copiedTournament.TournamentId,
                        Tournament = copiedTournament
                    };
                    exchangeKeysDrawer.Add(part.ParticipantId, participant);
                    school.Participants.Add(participant);
                }
            }


            foreach (var div in tournament.Divisions)
            {
                var division = new Division()
                {
                    DivisionId = Guid.NewGuid(),
                    Id = div.Id,
                    Name = div.Name,
                    Bracket = div.Bracket,
                    DoubleElimination = div.DoubleElimination,
                    NoComebackFromLooserBracket = div.NoComebackFromLooserBracket,
                    NoSecondaryFinal = div.NoSecondaryFinal,
                    DrawBracket = div.DrawBracket,
                    ConsolidationRound = div.ConsolidationRound,
                    OrderId = div.OrderId,
                    Title = div.Title,
                    RoundRobin = div.RoundRobin,
                    TournamentId = copiedTournament.TournamentId,
                    Tournament = copiedTournament
                };

                copiedTournament.Divisions.Add(division);
                foreach (var pd in div.ParticipantDivisionInts)
                {
                    var participantDiv = new ParticipantDivisionInt()
                    {
                        ParticipantDivisionIntId = Guid.NewGuid(),
                        Division = division,
                        DivisionId = division.DivisionId,
                        Participant = exchangeKeysDrawer[pd.ParticipantId],
                        ParticipantId = exchangeKeysDrawer[pd.ParticipantId].ParticipantId,
                        OrderId = div.OrderId
                    };

                    division.ParticipantDivisionInts.Add(participantDiv);
                }
            }

            db.Tournaments.Add(copiedTournament);
            db.SaveChanges();

            return RedirectToAction("Index");
        }



        // GET: Tournaments/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            return View(tournament);
        }

        public ActionResult Print(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            return View(tournament);
        }

        public ActionResult PrintSubdocument(Guid? id, bool drawBracket, int minSize, int maxSize, int pixHeight = 1500, int maxDivisionCount = 100, int startDivisionPosition = 0)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            return View(new TournamentModel(tournament, drawBracket, minSize, maxSize) { PixHeight = pixHeight, MaxDivisionCount = maxDivisionCount, StartDivisionPosition = startDivisionPosition });
        }

        public ActionResult PrintSingleDocument(Guid? id, Guid? divisionId, int pixHeight = 1500)
        {
            if (id == null && divisionId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            return View(new TournamentSingleDocumentModel(tournament, (Guid)divisionId) { PixHeight = pixHeight });
        }


        public ActionResult PrintSingleDocumentSmallDivision(Guid? id, Guid? divisionId, int pixHeight = 1500)
        {
            if (id == null && divisionId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            return View(new TournamentSingleDocumentModel(tournament, (Guid)divisionId) { PixHeight = pixHeight });
        }



        // GET: Tournaments/Create
        [Authorize]
        public ActionResult Create()
        {
            if (!User.IsInRole("Administrator"))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            return View();
        }

        //http://stackoverflow.com/questions/19543198/adding-role-dynamically-in-new-vs-2013-identity-usermanager
        private void setRole(string roleName)
        {
            using (var rm = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext())))
            //using (var um = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
            {
                var role = rm.FindByName(roleName);
                if (role == null)
                {
                    rm.Create(new IdentityRole(roleName));
                }

            }
            //foreach (var item in userRoles)
            //{
            //    if (!rm.RoleExists(item.Value))
            //    {
            //        var roleResult = rm.Create(new IdentityRole(item.Value));
            //        if (!roleResult.Succeeded)
            //            throw new ApplicationException("Creating role " + item.Value + "failed with error(s): " + roleResult.Errors);
            //    }
            //    var user = um.FindByName(item.Key);
            //    if (!um.IsInRole(user.Id, item.Value))
            //    {
            //        var userResult = um.AddToRole(user.Id, item.Value);
            //        if (!userResult.Succeeded)
            //            throw new ApplicationException("Adding user '" + item.Key + "' to '" + item.Value + "' role failed with error(s): " + userResult.Errors);
            //    }
            //}
        }


        private void removeRole(string roleName)
        {
            using (var rm = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext())))
            {
                var role = rm.FindByName(roleName);
                if (role != null)
                {
                    rm.Delete(role);
                }
            }
        }


        // POST: Tournaments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Create([Bind(Include = "TournamentId,Name,Date,Location,Options,ConsolidationRound")] Tournament tournament)
        {
            if (ModelState.IsValid)
            {
                if (!User.IsInRole("Administrator"))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                tournament.TournamentId = Guid.NewGuid();
                db.Tournaments.Add(tournament);
                db.SaveChanges();
                setRole(tournament.TournamentId.ToString().ToLower());
                return RedirectToAction("Index");
            }
            return View(tournament);
        }

        // GET: Tournaments/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            if (!User.IsInRole("Administrator"))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            return View(tournament);
        }

        public ActionResult TxtReport(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }

            return new FileContentResult(System.Text.Encoding.UTF8.GetBytes(Tournament.DebugReport(tournament).ToString()), "text/plain; charset=UTF-8");
        }

        public ActionResult PdfPrint(Guid? id)
        {
            var printUrl = System.Configuration.ConfigurationManager.AppSettings["pdfPrintUrl"];
            if (string.IsNullOrEmpty(printUrl))
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "pdfPrintUrl must be added to configuration");
            if (!printUrl.Contains("{0}"))
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "pdfPrintUrl must containg {0} tag!");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Tournament ID must be specified!");
            }
            var fullUrl = Url.AbsoluteAction("Print", "Tournaments", new { id = id });
            return Redirect(String.Format(CultureInfo.InvariantCulture, printUrl, fullUrl));
        }

        private bool CheckHasDivisions(Tournament t, bool hasBracket, int minParticipants, int maxParticipant)
        {
            return t.Divisions.Where(d => d.DrawBracket == hasBracket && d.ParticipantDivisionInts.Count >= minParticipants && d.ParticipantDivisionInts.Count <= maxParticipant).Any();
        }


        class PrintTemplate
        {
            public int min;
            public int max;
            public bool hasBracket;
            public string orientation;
            public string pageSize;
            public double marginAll;
            public double pagePixHeight;
            public double zoomFactor;
        }


        //divisionId is null - prints them all
        public ActionResult PdfPrintDivision(Guid? id, Guid? divisionId)
        {
            int divisionNumber = 0;
            var templates = new PrintTemplate[]
                {
                    new PrintTemplate() {min = 1, max = 8, hasBracket = true, orientation = "Landscape", pageSize = "Letter", pagePixHeight = 720, marginAll = 1, zoomFactor = 1.5 },
                    new PrintTemplate() {min = 9, max = 16, hasBracket = true, orientation = "Portrait", pageSize = "Letter", pagePixHeight = 1340, marginAll = 0.5, zoomFactor = .95 },
                    new PrintTemplate() {min = 17, max = 32, hasBracket = true, orientation = "Portrait", pageSize = "Letter", pagePixHeight = 1500, marginAll = 2, zoomFactor = .7 },
                    new PrintTemplate() {min = 33, max = 64, hasBracket = true, orientation = "Portrait", pageSize = "A2", pagePixHeight = 3000, marginAll = 1.5, zoomFactor = .82 },
                    new PrintTemplate() {min = 1, max = 32, hasBracket = false, orientation = "Landscape", pageSize = "Letter", pagePixHeight = 840, marginAll = 1, zoomFactor = .95 },
                    new PrintTemplate() {min = 33, max = 128, hasBracket = false, orientation = "Portrait", pageSize = "Letter", pagePixHeight = 1200, marginAll = 1, zoomFactor = .90}
                };

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);

            var printUrl = System.Configuration.ConfigurationManager.AppSettings["html2pdf"];
            if (string.IsNullOrEmpty(printUrl))
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "html2pdf must be added to configuration");

            var printCommand = new PrintCommand() { Name = tournament.Name };

            foreach (var division in tournament.Divisions.Where(d => (divisionId == null || d.DivisionId == divisionId)).OrderBy(d => d.OrderId))
            {
                if (divisionId != null)
                    divisionNumber = (int)division.OrderId;

                //hack - I treat divisions of 1 as flat therefore I need to apply the right rule here
                if (division.ParticipantDivisionInts.Count == 1)
                {
                    var template = templates[4];//hack to get flat of 1
                    printCommand.Documents.Add(new Subdocument()
                    {
                        Address = Url.AbsoluteAction("PrintSingleDocument", "Tournaments",
                        new { id = id, divisionId = division.DivisionId, pixHeight = template.pagePixHeight }),
                        Orientation = template.orientation,
                        MarginAll = template.marginAll,
                        PageSize = template.pageSize,
                        ZoomFactor = template.zoomFactor
                    });
                }
                else
                    foreach (var template in templates)
                    {


                        if (template.hasBracket == division.DrawBracket && template.min <= division.ParticipantDivisionInts.Count && template.max >= division.ParticipantDivisionInts.Count)
                        {
                            printCommand.Documents.Add(new Subdocument()
                            {
                                Address = division.ParticipantDivisionInts.Count <= 16 ? Url.AbsoluteAction("PrintSingleDocumentSmallDivision", "Tournaments",
                                new { id = id, divisionId = division.DivisionId, pixHeight = template.pagePixHeight }) : Url.AbsoluteAction("PrintSingleDocument", "Tournaments",
                                new { id = id, divisionId = division.DivisionId, pixHeight = template.pagePixHeight }),
                                Orientation = template.orientation,
                                MarginAll = template.marginAll,
                                PageSize = template.pageSize,
                                ZoomFactor = template.zoomFactor
                            });
                            break;
                        }
                    }
            }

            var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string jsonString = javaScriptSerializer.Serialize(printCommand);

            using (var client = new ExtendedTimeout())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                byte[] result = client.UploadData(printUrl, (new ASCIIEncoding()).GetBytes(jsonString));
                return new FileContentResult(result, "application/pdf") { FileDownloadName = MakeValidFileName(tournament.Name) + ((divisionId == null) ? "" : string.Format("_division_{0}", divisionNumber)) + ".pdf" };
            }
        }


        public ActionResult PdfPrintDocOrdered(Guid? id)
        {
            return PdfPrintDivision(id, null);
        }


        public ActionResult PdfPrintDoc(Guid? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);

            var printUrl = System.Configuration.ConfigurationManager.AppSettings["html2pdf"];
            if (string.IsNullOrEmpty(printUrl))
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "html2pdf must be added to configuration");

            var printCommand = new PrintCommand() { Name = tournament.Name };
            if (CheckHasDivisions(tournament, true, 1, 8))
                printCommand.Documents.Add(new Subdocument()
                {
                    Address = Url.AbsoluteAction("PrintSubdocument", "Tournaments",
                    new { id = id, drawBracket = true, minSize = 1, maxSize = 8, pixHeight = 500 }),
                    Orientation = "Landscape",
                    MarginAll = 1,
                    PageSize = "Letter",
                    ZoomFactor = 1.5
                });
            if (CheckHasDivisions(tournament, true, 9, 16))
                printCommand.Documents.Add(new Subdocument()
                {
                    Address = Url.AbsoluteAction("PrintSubdocument", "Tournaments",
                new { id = id, drawBracket = true, minSize = 9, maxSize = 16, pixHeight = 850 }),
                    Orientation = "Landscape",
                    MarginAll = 1,
                    PageSize = "Letter",
                    ZoomFactor = 0.95
                });
            if (CheckHasDivisions(tournament, true, 17, 32))
                printCommand.Documents.Add(new Subdocument()
                {
                    Address = Url.AbsoluteAction("PrintSubdocument", "Tournaments",
                new { id = id, drawBracket = true, minSize = 17, maxSize = 32, pixHeight = 1500 }),
                    Orientation = "Portrait",
                    MarginAll = 2,
                    PageSize = "Letter",
                    ZoomFactor = 0.7
                });
            if (CheckHasDivisions(tournament, true, 33, 64))
                printCommand.Documents.Add(new Subdocument()
                {
                    Address = Url.AbsoluteAction("PrintSubdocument", "Tournaments",
              new { id = id, drawBracket = true, minSize = 33, maxSize = 64, pixHeight = 3000 }),
                    Orientation = "Portrait",
                    MarginAll = 1.5,
                    PageSize = "A2",
                    ZoomFactor = 0.82
                });
            if (CheckHasDivisions(tournament, false, 1, 32))
                printCommand.Documents.Add(new Subdocument()
                {
                    Address = Url.AbsoluteAction("PrintSubdocument", "Tournaments",
               new { id = id, drawBracket = false, minSize = 1, maxSize = 32, pixHeight = 840 }),
                    Orientation = "Landscape",
                    MarginAll = 1,
                    PageSize = "Letter",
                    ZoomFactor = 0.95
                });
            if (CheckHasDivisions(tournament, false, 33, 64))
                printCommand.Documents.Add(new Subdocument()
                {
                    Address = Url.AbsoluteAction("PrintSubdocument", "Tournaments",
               new { id = id, drawBracket = false, minSize = 33, maxSize = 64, pixHeight = 1100 }),
                    Orientation = "Portrait",
                    MarginAll = 2,
                    PageSize = "Letter",
                    ZoomFactor = 0.90
                });

            var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string jsonString = javaScriptSerializer.Serialize(printCommand);

            using (var client = new ExtendedTimeout())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                byte[] result = client.UploadData(printUrl, (new ASCIIEncoding()).GetBytes(jsonString));
                return new FileContentResult(result, "application/pdf") { FileDownloadName = MakeValidFileName(tournament.Name) + ".pdf" };
            }
        }
        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        private class ExtendedTimeout : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 120 * 60 * 1000;
                return w;
            }
        }


        // POST: Tournaments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Edit([Bind(Include = "TournamentId,Name,Date,Location,Options,ConsolidationRound")] Tournament tournament)
        {
            if (ModelState.IsValid)
            {
                if (!User.IsInRole("Administrator"))
                {
                    return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
                }
                db.Entry(tournament).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tournament);
        }

        // GET: Tournaments/Delete/5
        [Authorize]
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            if (!User.IsInRole("Administrator"))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            return View(tournament);
        }

        // POST: Tournaments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteConfirmed(Guid id)
        {
            if (!User.IsInRole("Administrator"))
            {
                return Content("Access denied: this user doesn't have permission to alter the selected tournament!");
            }
            Tournament tournament = db.Tournaments.Find(id);
            db.Tournaments.Remove(tournament);
            db.SaveChanges();
            removeRole(id.ToString().ToLower());
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