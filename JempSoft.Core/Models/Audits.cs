using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JempSoft.Core.Models
{
    public class Audits
    {
        public bool IsActive { get; set; } = true;
        
        public int CreatorUserId { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public int UpdateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int DeleteUserId { get; set; }
        public DateTime? DeleteDate { get; set; }
        public bool? IsDeleted { get; set; } = false;

        [ForeignKey("CreatorUserId")]
        public virtual User User { get; set; }
    }

    public class User
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
    }
}
