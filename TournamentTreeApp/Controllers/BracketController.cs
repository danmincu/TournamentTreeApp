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
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            using (var context = new TournamentModels.TournamentTreeAppEntities())
            {
                var division = context.Divisions.FirstOrDefault(d => d.DivisionId == divisionId);
                return View(new MinimalDivision() { Id = divisionId, Name = division.Name, TournamentName = division.Tournament.Name, Title = division.Title, TournamentId = division.TournamentId });
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
            using (var context = new TournamentModels.TournamentTreeAppEntities())
            {
                var division = context.Divisions.FirstOrDefault(d => d.DivisionId == divisionId);
                return View(new MinimalDivision() { Id = divisionId, Name = division.Name, TournamentName = division.Tournament.Name, Title = division.Title, TournamentId = division.TournamentId });
            }
        }

    }

    public class MinimalDivision
    {
        public string TournamentName { set; get; }
        public string Name { set; get; }
        public string Title { set; get; }
        public Guid Id { set; get; }
        public Guid TournamentId { set; get; }
    }
}