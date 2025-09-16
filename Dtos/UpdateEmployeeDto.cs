using EmployeeAPI.Enumerations.Roles;
using System.ComponentModel.DataAnnotations;

namespace EmployeeAPI.Dtos
{
    public class UpdateEmployeeDto
    {
        [Required]
        public int Id { get; set; }

        [Range(3, 120, ErrorMessage = "Name must be between 3 and 120 characters")]
        public string? Name { get; set; }

        [EmailAddress]
        [Range(3, 120, ErrorMessage = "Email must be between 3 and 120 characters")]
        public string? Email { get; set; }

        public Role? Role { get; set; }
    }
}
