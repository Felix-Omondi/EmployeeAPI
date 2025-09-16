using EmployeeAPI.Data;
using EmployeeAPI.Dtos;
using EmployeeAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeAPI.Repository
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly EmployeeDbContext _context;

        public EmployeeRepository(EmployeeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Employee> CreateEmployeeAsync(CreateEmployeeDto employee)
        {
            ArgumentNullException.ThrowIfNull(employee, nameof(employee));

            var newEmployee = new Employee
            {
                Name = employee.Name.Trim(),
                Email = employee.Email.Trim().ToLowerInvariant(),
                Role = employee.Role,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
            };

            await _context.Employees.AddAsync(newEmployee);
            await _context.SaveChangesAsync();
            return newEmployee;
        }

        public async Task DeleteEmployeeAsync(int id)
        {
            if (id < 1) throw new ArgumentException("Invalid ID");
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            if (id < 1) throw new ArgumentException("Invalid ID");
            return await _context.Employees.FindAsync(id);
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            ArgumentNullException.ThrowIfNull(employee, nameof(employee));
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }
    }
}
