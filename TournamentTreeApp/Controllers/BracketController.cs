using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TournamentsTreeApp.Controllers
{
    public class BracketController : Controller
    {
        // GET: Bracket
        public ActionResult Index(Guid divisionId)
        {
            return this.GetDivisionView(divisionId);
        }

        private ActionResult GetDivisionView(Guid divisionId)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            using (var context = new TournamentModels.TournamentTreeAppEntities())
            {
                var division = context.Divisions.FirstOrDefault(d => d.DivisionId == divisionId);
                var list = division.Tournament.Divisions.OrderBy(d => d.OrderId).ToList();

                int index = list.IndexOf(division); // find the index of the given number

                // find the index of next and the previous number
                // by taking into account that 
                // the given number might be the first or the last number in the list
                int prev = index > 0 ? index - 1 : -1;

                int next = index < list.Count - 1 ? index + 1 : -1;

                return View(new MinimalDivision()
                {
                    Id = divisionId,
                    Name = division.Name,
                    TournamentName = division.Tournament.Name,
                    PreviousDivision = prev != -1 ? list[prev] : null,
                    NextDivision = next != -1 ? list[next] : null,
                    PreviousAsBracket = prev != -1 ? list[prev].DrawBracket && list[prev].ParticipantDivisionInts.Any() : false,
                    NextAsBracket = next != -1 ? list[next].DrawBracket && list[next].ParticipantDivisionInts.Any() : false,
                    Title = division.Title,
                    TournamentId = division.TournamentId
                });
            }
        }

        public ActionResult Draw(Guid divisionId)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            using (var context = new TournamentModels.TournamentTreeAppEntities())
            {
                var division = context.Divisions.FirstOrDefault(d => d.DivisionId == divisionId);
                return View("Index", new MinimalDivision() { Id = divisionId, Name = division.Name, TournamentName = division.Tournament.Name, Title = division.Title, TournamentId = division.TournamentId });
            }
        }

        public ActionResult Print(Guid divisionId)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            using (var context = new TournamentModels.TournamentTreeAppEntities())
            {
                var division = context.Divisions.FirstOrDefault(d => d.DivisionId == divisionId);
                return View(new MinimalDivision() { Id = divisionId, Name = division.Name, TournamentName = division.Tournament.Name, Title = division.Title, TournamentId = division.TournamentId });
            }
        }


        public ActionResult FlatList(Guid divisionId)
        {
            return this.GetDivisionView(divisionId);
        }

    }

    public class MinimalDivision
    {
        public string TournamentName { set; get; }
        public string Name { set; get; }
        public string Title { set; get; }
        public Guid Id { set; get; }
        public Guid TournamentId { set; get; }
        public bool PreviousAsBracket { set; get; }
        public TournamentModels.Division PreviousDivision { get; set; }
        public bool NextAsBracket { set; get; }
        public TournamentModels.Division NextDivision { get; set; }
    }
}