//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TournamentModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    public partial class Division
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Division()
        {
            this.ParticipantDivisionInts = new HashSet<ParticipantDivisionInt>();
        }
    
        public System.Guid DivisionId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public System.Guid TournamentId { get; set; }

        [DisplayName("Draw Bracket")]
        public bool DrawBracket { get; set; }
        public string Bracket { get; set; }
        public Nullable<int> OrderId { get; set; }
        public bool RoundRobin { get; set; }
        public bool DoubleElimination { get; set; }
        public bool NoSecondaryFinal { get; set; }
        public bool NoComebackFromLooserBracket { get; set; }
        [DisplayName("Consolidation round")]
        public bool ConsolidationRound { get; set; }
    
        public virtual Tournament Tournament {
            get;
            set;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ParticipantDivisionInt> ParticipantDivisionInts { get; set; }
    }
}
