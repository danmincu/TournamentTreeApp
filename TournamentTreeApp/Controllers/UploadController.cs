using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using TournamentModels;
using TournamentsTreeApp.Common;
using TournamentTreeApp.Models;

namespace TournamentsTreeApp.Controllers
{
    [RoutePrefix("api/upload")]
    public class UploadController : ApiController
    {
        class FileResult : IHttpActionResult
        {
            private readonly string fileContent;
            private readonly string _contentType;
            private readonly string _charSet;

            public FileResult(string fileContent, string charSet = null, string contentType = null)
            {
                if (string.IsNullOrEmpty(fileContent)) throw new ArgumentNullException("fileContent");

                this.fileContent = fileContent;
                _charSet = charSet;
                _contentType = contentType;

            }

            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {

                    Content = new StreamContent(StringToStream(fileContent))
                };

                var contentType = _contentType ?? MimeMapping.GetMimeMapping(Path.GetExtension("t.txt"));
                if (_charSet != null)
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType) { CharSet = _charSet };
                else
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                return Task.FromResult(response);
            }

            public static string StreamToString(Stream stream)
            {
                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }

            public static Stream StringToStream(string src)
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(src);
                return new MemoryStream(byteArray);
            }

        }

        [Route("tournamentcsvfiles")]
        [HttpPost]
        [ValidateMimeMultipartContentFilter]
        [Authorize]
        public async /*Task<FileResult>*/Task<IHttpActionResult> UploadCsvFilePair()
        {
            if (!(User.IsInRole("Administrator") || User.IsInRole("Creator")))
            {
                return Content(HttpStatusCode.Forbidden, "You don't have permission for this operation");
            }

            var streamProvider = new MultipartMemoryStreamProvider();// CustomMultipartFormDataStreamProvider(/*ServerUploadFolder*/);

            await Request.Content.ReadAsMultipartAsync(streamProvider);
            var tournamentNameContent = streamProvider.Contents.Where(c => c.Headers.ContentDisposition.Name.Equals("\"tournament\"")).FirstOrDefault();
            if (tournamentNameContent == null)
                return Content(HttpStatusCode.ExpectationFailed, "Null tournament name");

            var tournamentDefaultConsolidationRoundContent = streamProvider.Contents.Where(c => c.Headers.ContentDisposition.Name.Equals("\"defConsolidation\"")).FirstOrDefault();
            var defConsolidation = tournamentDefaultConsolidationRoundContent == null ? "off" : tournamentDefaultConsolidationRoundContent.ReadAsStringAsync().Result;
            var boolDefConsolidation = defConsolidation != null && defConsolidation.Equals("on", StringComparison.OrdinalIgnoreCase);
            var tournamentName = tournamentNameContent.ReadAsStringAsync().Result;
            if (string.IsNullOrEmpty(tournamentName))
                return Content(HttpStatusCode.ExpectationFailed, "Empty tournament name");

            var encodingContent = streamProvider.Contents.Where(c => c.Headers.ContentDisposition.Name.Equals("\"encoding\"")).FirstOrDefault();
            if (encodingContent == null)
                return Content(HttpStatusCode.ExpectationFailed, "Null encoding type");

            var encodingyType = encodingContent.ReadAsStringAsync().Result;
            if (string.IsNullOrEmpty(encodingyType))
                return Content(HttpStatusCode.ExpectationFailed, "Empty encodingyType");
            Encoding enc;
            if (encodingyType.Equals("UTF-8", StringComparison.OrdinalIgnoreCase))
                enc = Encoding.UTF8;
            else
                enc = Encoding.GetEncoding(1252);
            var dict = streamProvider.Contents.Where(c => !string.IsNullOrEmpty(c.Headers.ContentDisposition.FileName)).Select(c =>
             new KeyValuePair<string, string>(c.Headers.ContentDisposition.FileName, enc.GetString(c.ReadAsByteArrayAsync().Result))).ToDictionary(kv => kv.Key, kv => kv.Value);

            if (dict.Count != 2)
                return Content(HttpStatusCode.ExpectationFailed, "You need to upload exactly 2 csv files simultaneously");
            try
            {   

                //var datacsv = dict.Keys.Where(k => k.IndexOf("data", StringComparison.OrdinalIgnoreCase) > 0).FirstOrDefault();
                //var divisionscsv = dict.Keys.Where(k => k.IndexOf("division", StringComparison.OrdinalIgnoreCase) > 0).FirstOrDefault();
                //improved detection of the what file is what
                var datacsv = dict.Keys.Where(k => Regex.IsMatch(k,@"(Data.*)\.csv", RegexOptions.Multiline)).FirstOrDefault();
                var divisionscsv = dict.Keys.Where(k => Regex.IsMatch(k, @"(Divisions.*)\.csv", RegexOptions.Multiline)).FirstOrDefault();
                var tournament = TournamentModels.Tournament.LoadFromImportedCsv(tournamentName, dict[divisionscsv], dict[datacsv]);
                foreach (var division in tournament.Divisions)
                {
                    division.ConsolidationRound = boolDefConsolidation;
                }
                tournament.ConsolidationRound = boolDefConsolidation;

                try
                {
                    SaveTournament(tournament);
                }
                catch (Exception ex1)
                {
                   return Content(HttpStatusCode.ExpectationFailed,
                     string.Format("Error saving the tournament to the database! Exception {0}", ex1.Message));                    
                }


                //return new FileResult(TournamentOboslete.DebugReport(tournament).ToString(), "UTF-8");
                //return new FileResult(Tournament.DebugReport(tournament).ToString(), "UTF-8");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.ExpectationFailed,
                    string.Format("Parsing data failed. Make sure the csv files are respecting the name and content format! Exception {0}", ex.Message));
            }

            return RedirectToRoute("Default", null);

        }


        private void setRole(string roleName)
        {
            using (var rm = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext())))            
            {
                var role = rm.FindByName(roleName);
                if (role == null)
                {
                    rm.Create(new IdentityRole(roleName));
                }
            }         
        }

        private void SaveTournament(Tournament tournament)
        {
            using (var context = new TournamentTreeAppEntities())
            {
                context.Tournaments.Add(tournament);
                context.SaveChanges();
                setRole(tournament.TournamentId.ToString().ToLower());
            }

        }
    }

}
