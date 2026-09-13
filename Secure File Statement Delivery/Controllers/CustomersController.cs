using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secure_File_Statement_Delivery.Data;
using Secure_File_Statement_Delivery.Models;
using Microsoft.AspNetCore.Authorization;

namespace Secure_File_Statement_Delivery.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CustomersController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/customers
    [HttpGet]
    public async Task<IActionResult> GetCustomers()
    {
        var customers = await _db.Customers
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
                x.AccountNumber,
                x.FullName,
                x.Email,
                x.PhoneNumber,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(customers);
    }

    // GET: api/customers/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var customer = await _db.Customers
            .Include(x => x.Statements)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        return Ok(new
        {
            customer.Id,
            customer.AccountNumber,
            customer.FullName,
            customer.Email,
            customer.PhoneNumber,
            customer.CreatedAt,
            Statements = customer.Statements.Select(s => new
            {
                s.Id,
                s.StatementPeriod,
                s.FileName,
                s.CreatedAt
            })
        });
    }

    // POST: api/customers
    [HttpPost]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.AccountNumber))
        {
            return BadRequest(new
            {
                message = "Account number is required."
            });
        }

        if (string.IsNullOrWhiteSpace(customer.FullName))
        {
            return BadRequest(new
            {
                message = "Full name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        var accountExists = await _db.Customers
            .AnyAsync(x =>
                x.AccountNumber == customer.AccountNumber);

        if (accountExists)
        {
            return Conflict(new
            {
                message = "A customer with this account number already exists."
            });
        }

        var newCustomer = new Customer
        {
            AccountNumber = customer.AccountNumber.Trim(),
            FullName = customer.FullName.Trim(),
            Email = customer.Email.Trim(),
            PhoneNumber = customer.PhoneNumber?.Trim()
        };

        _db.Customers.Add(newCustomer);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = newCustomer.Id },
            newCustomer);
    }
}