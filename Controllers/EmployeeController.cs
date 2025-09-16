using EmployeeAPI.Dtos;
using EmployeeAPI.Models;
using EmployeeAPI.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EmployeeAPI.Controllers
{
    [Route("api/employees")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmployeeRepository _repository;
        private readonly IMemoryCache _memoryCache;
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private const string EMPLOYEECACHEKEY = "EMPLOYEESCACHEKEY";

        public EmployeeController(IEmployeeRepository repository, ILogger<EmployeeController> logger, IMemoryCache memoryCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        // GET: api/employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> Get()
        {
            _logger.LogInformation("Retrieving all employee records");

            if (_memoryCache.TryGetValue(EMPLOYEECACHEKEY, out IEnumerable<Employee>? cachedEmployees))
            {
                _logger.LogInformation("Successfully retrieved employees from cache");
                return Ok(cachedEmployees);
            }

            try
            {
                await semaphore.WaitAsync();

                if (_memoryCache.TryGetValue(EMPLOYEECACHEKEY, out cachedEmployees))
                {
                    _logger.LogInformation("Successfully retrieved employees from cache after semaphore");
                    return Ok(cachedEmployees);
                }

                var employees = await _repository.GetAllEmployeesAsync();
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(60)).SetAbsoluteExpiration(TimeSpan.FromHours(1)).SetPriority(CacheItemPriority.Normal);
                _memoryCache.Set(EMPLOYEECACHEKEY, employees, cacheEntryOptions);
                _logger.LogInformation("Successfully retrieved employees from database and cached");
                return Ok(employees);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees");
                return StatusCode(500, new { error = "An error occurred while retrieving the employees" });
            }
            finally
            {
                semaphore.Release();
            }
        }

        // GET api/employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetById(int id)
        {
            _logger.LogInformation("Retrieving employee with ID {id}", id);

            string cacheKey = $"Employee_{id}";

            if (_memoryCache.TryGetValue(cacheKey, out Employee? cachedEmployee))
            {
                _logger.LogInformation("Successfully retrieved employee with ID {id} from cache after semaphore", id);
                return Ok(cachedEmployee);
            }

            try
            {
                await semaphore.WaitAsync();

                if (_memoryCache.TryGetValue(cacheKey, out cachedEmployee))
                {
                    _logger.LogInformation("Successfully retrieved employee with ID {id} from cache", id);
                    return Ok(cachedEmployee);
                }

                var employee = await _repository.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {id} not found", id);
                    return NotFound(new { error = "Employee not found" });
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(60)).SetAbsoluteExpiration(TimeSpan.FromHours(1)).SetPriority(CacheItemPriority.Normal);
                _memoryCache.Set(cacheKey, employee, cacheEntryOptions);

                _logger.LogInformation("Successfully retrieved employee with ID {id} from database and cached", id);
                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with ID {id}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the employee" });
            }
            finally
            {
                semaphore.Release();
            }
        }

        // POST api/employees
        [HttpPost]
        public async Task<ActionResult<Employee>> Post([FromBody] CreateEmployeeDto employee)
        {
            _logger.LogInformation("Creating new employee");

            if (employee == null || !ModelState.IsValid)
            {
                _logger.LogError("Invalid employee data");
                return BadRequest(new { error = "Invalid employee data" });
            }

            try
            {
                var newEmployee = await _repository.CreateEmployeeAsync(employee);
                //await _repository.CreateEmployeeAsync(newEmployee);
                _logger.LogInformation("Successfully created new employee");
                _memoryCache.Remove(EMPLOYEECACHEKEY);
                return CreatedAtAction(nameof(GetById), new { id = newEmployee.Id }, newEmployee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create employee");
                return StatusCode(500, new { error = "An error occured while creating the employee" });
            }

        }

        // PUT api/employees/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateEmployeeDto employee)
        {
            string cacheKey = $"Employee_{id}";
            // string allEmployeesCacheKey = "EMPLOYEESCACHEKEY";

            if (id < 1) throw new ArgumentException("Invalid ID");

            _logger.LogInformation("Updating employee with ID { id}", id);
            if (employee == null || !ModelState.IsValid || employee.Id != id)
            {
                _logger.LogError("Invalid employee data");
                return BadRequest(new { error = "Invalid employee data" });
            }

            var existingEmployee = await _repository.GetEmployeeByIdAsync(id);
            if (existingEmployee == null)
            {
                _logger.LogWarning("Employee with ID {id} not found", id);
                return NotFound(new { message = "Employee not found" });
            }

            existingEmployee.Name = employee.Name?.Trim() ?? existingEmployee.Name;
            existingEmployee.Email = employee.Email?.Trim().ToLowerInvariant() ?? existingEmployee.Email;
            existingEmployee.Role = employee.Role ?? existingEmployee.Role;
            existingEmployee.LastModifiedDate = DateTime.UtcNow;

            try
            {
                await _repository.UpdateEmployeeAsync(existingEmployee);
                _logger.LogInformation("Successfully updated details of employee with ID {id}", id);
                _memoryCache.Remove(cacheKey);
                _memoryCache.Remove(EMPLOYEECACHEKEY);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update employee with ID {id}", id);
                return StatusCode(500, new { error = "An error occurred while updating the employee" });
            }
        }

        // DELETE api/employees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            string cacheKey = $"Employee_{id}";
            _logger.LogInformation("Deleting employee with ID {id}", id);

            if (id < 1) throw new ArgumentException("Invalid ID");

            var existingEmployee = await _repository.GetEmployeeByIdAsync(id);
            if (existingEmployee == null)
            {
                _logger.LogWarning("Employee with ID {id} not found", id);
                return NotFound(new { message = "Employee not found" });
            }

            try
            {
                await _repository.DeleteEmployeeAsync(id);
                _logger.LogInformation("Successfully deleted employee with ID {id}", id);
                _memoryCache.Remove(cacheKey);
                _memoryCache.Remove(EMPLOYEECACHEKEY);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete employee with ID {id}", id);
                return StatusCode(500, new { error = "An error occured while deleting the employee" });
            }

        }
    }
}
