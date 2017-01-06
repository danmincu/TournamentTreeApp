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
    
    public partial class Participant
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Participant()
        {
            this.ParticipantDivisionInts = new HashSet<ParticipantDivisionInt>();
        }
    
        public System.Guid ParticipantId { get; set; }
        public string Name { get; set; }
        public System.Guid TournamentId { get; set; }
        public System.Guid SchoolId { get; set; }
        public bool Dummy { get; set; }
    
        public virtual School School { get; set; }
        public virtual Tournament Tournament { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ParticipantDivisionInt> ParticipantDivisionInts { get; set; }
    }
}
