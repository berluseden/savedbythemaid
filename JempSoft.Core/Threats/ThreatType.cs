using JempSoft.Core.Api.Database.Entity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JempSoft.Core.Api.Database.Entity.Threats
{
    public class ThreatType : BaseEntity
    {
        public ThreatType()
        {

        }

        [Required]
        [StringLength(50)]
        public string Name
        {
            get;
            set;
        }

        public virtual ICollection<Threat> Threats { get; set; }
    }
}