using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TournamentsTreeApp.Models
{
    public class FastParticipant
    {
        public string ParticipantName { set; get; }
        public Guid SchoolId { set; get; }
        public string SchoolName { set; get; }
        public Guid DivisionId { set; get; }
        public Guid TournamentId { set; get; }
    }
}