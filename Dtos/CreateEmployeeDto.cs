using EmployeeAPI.Enumerations.Roles;
using System.ComponentModel.DataAnnotations;

namespace EmployeeAPI.Dtos
{
    public class CreateEmployeeDto
    {
        [Required]
        [Length(3, 120, ErrorMessage = "Name must be between 3 and 120 characters")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Length(3, 120, ErrorMessage = "Email must be between 3 and 120 characters")]
        public string Email { get; set; }

        [Required]
        public Role Role { get; set; }
    }
}
