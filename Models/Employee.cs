using EmployeeAPI.Enumerations.Roles;
using System.ComponentModel.DataAnnotations;

namespace EmployeeAPI.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Length(3, 100,ErrorMessage ="Name must be between 3 and 100 characters")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Length(3, 120, ErrorMessage = "Email must be between 3 and 120 characters")]
        public string Email { get; set; }

        public Role Role { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastModifiedDate { get; set; }
    }
}
