using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TournamentsTreeApp.Models;

namespace TournamentsTreeApp.Controllers
{
    public class DivisionController : ApiController
    {
        // GET: api/Division
        public Rootobject Get()
        {
            using (var context = new TournamentModels.TournamentTreeAppEntities())
            {
                //return context.Divisions.Select(d=>d.Name);
                return new Rootobject();
            }
        }

        // GET: api/Division/5
        public Rootobject Get(Guid divisionId)
        {
            using (var context = new TournamentModels.TournamentTreeAppEntities())
            {
                //this greatly improves perfomance by preloading divisions schools and participants for a given tournament
                var tournament = context.Tournaments.Where(t => t.Divisions.Any(d => d.DivisionId == divisionId)).Include(t => t.Divisions).Include(t => t.Schools).Include(t => t.Participants).FirstOrDefault();

                var division = tournament.Divisions
                    //    .Include(d => d.ParticipantDivisionInts)                    
                    .FirstOrDefault(d => d.DivisionId == divisionId);
                if (division.DrawBracket && division.ParticipantDivisionInts.Count > 1)
                {
                    var bracketList = division.BracketParticipants(context).ToArray();

                    var teams = new string[(int)(bracketList.Length / 2)][];
                    for (int i = 0; i < (int)(bracketList.Length / 2); i++)
                    {
                        var element1 = bracketList[2 * i];
                        var element2 = bracketList[2 * i + 1];
                        var name1 = element1.School == null ? element1.Name : element1.Name + "|" + element1.School.Name;
                        var name2 = element2.School == null ? element2.Name : element2.Name + "|" + element2.School.Name;
                        teams[i] = new string[] { name1, name2 };
                    }
                    var init = new Init() { /*results = new int?[1][][][],*/ teams = teams, divisionId = division.DivisionId.ToString(), drawBracket = true };

                    //read the results from the database
                    if (division.Bracket != null)
                    {
                        try
                        {
                            init.results = JsonConvert.DeserializeObject<Init>(division.Bracket).results;
                        }
                        catch
                        {
                            //bury the error bad data in the database?
                        }
                    }

                    //if the results have not been initialized from the database
                    if (init.results == null)
                    {
                        init.results = new int?[1][][][];
                        init.results[0] = new int?[1][][];
                        init.results[0][0] = Enumerable.Range(0, teams.Length).Select(i => teams[i][1].Contains("==>") ? new int?[2] { 1, 0 } : new int?[2]).ToArray();
                    }

                    var bracket = new Bracket() { dir = "lr", skipConsolationRound = !(division.ConsolidationRound) || division.ParticipantDivisionInts.Count <= 3, skipGrandFinalComeback = false, skipSecondaryFinal = false, init = init };
                    var ro = new Rootobject() { id = division.Tournament.TournamentId.ToString(), data = new Data() { name = division.Tournament.Name, bracket = bracket } };
                    return ro;
                }
                //no bracket is to be drawn
                else
                {
                    var teams = new string[1][];
                    teams[0] = division.OrderedParticipants.Select(p => p.School == null ? p.Name : p.Name + "|" + p.School.Name).ToArray();
                    var init = new Init() { results = new int?[1][][][], teams = teams, drawBracket = false };
                    var bracket = new Bracket() { init = init };
                    var ro = new Rootobject() { id = division.Tournament.TournamentId.ToString(), data = new Data() { name = division.Tournament.Name, bracket = bracket } };

                    return ro;
                }
            }
        }

        // POST: api/Division
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/Division/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Division/5
        public void Delete(int id)
        {
        }
    }
}
