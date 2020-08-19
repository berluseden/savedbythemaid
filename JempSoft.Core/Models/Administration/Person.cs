using System.ComponentModel.DataAnnotations;

namespace JempSoft.Core.Models
{
    public class Person
    {

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string FullName
        {
            get
            {
                return string.Join(" ", FirstName, LastName);
            }
        }

        [Required]
        public string Identification { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string ContactNumber { get; set; }

        [Required]
        public string EmailAddress { get; set; }

    }
}
