using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JempSoft.Core.Models
{
    public class ServiceOrderContactInfo
    {
        [Key]
        public int Id { get; set; }
        
        public int? UserId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public string AdditionalServiceInfo { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    
    }
}
