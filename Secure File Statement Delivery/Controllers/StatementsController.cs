using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secure_File_Statement_Delivery.Data;
using Secure_File_Statement_Delivery.Models;
using Secure_File_Statement_Delivery.Services;

namespace Secure_File_Statement_Delivery.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatementsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly FileStorageService _fileStorage;
    private readonly SecureLinkService _secureLink;

    public StatementsController(
        ApplicationDbContext db,
        FileStorageService fileStorage,
        SecureLinkService secureLink)
    {
        _db = db;
        _fileStorage = fileStorage;
        _secureLink = secureLink;
    }

    // =========================================================
    // GET ALL STATEMENTS
    // =========================================================

    // GET: api/statements
    [HttpGet]
    public async Task<IActionResult> GetStatements()
    {
        var statements = await _db.Statements
            .Include(x => x.Customer)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.StatementPeriod,
                x.FileName,
                x.CreatedAt,

                Customer = new
                {
                    x.Customer!.Id,
                    x.Customer.AccountNumber,
                    x.Customer.FullName,
                    x.Customer.Email
                }
            })
            .ToListAsync();

        return Ok(statements);
    }

    // =========================================================
    // GET ONE STATEMENT
    // =========================================================

    // GET: api/statements/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStatement(int id)
    {
        var statement = await _db.Statements
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (statement == null)
        {
            return NotFound(new
            {
                message = "Statement not found."
            });
        }

        return Ok(new
        {
            statement.Id,
            statement.StatementPeriod,
            statement.FileName,
            statement.CreatedAt,
            Customer = new
            {
                statement.Customer!.Id,
                statement.Customer.AccountNumber,
                statement.Customer.FullName,
                statement.Customer.Email
            }
        });
    }

    // =========================================================
    // UPLOAD STATEMENT
    // =========================================================

    // POST: api/statements
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadStatement(
        [FromForm] int customerId,
        [FromForm] string statementPeriod,
        [FromForm] IFormFile file)
    {
        if (customerId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid customer ID is required."
            });
        }

        if (string.IsNullOrWhiteSpace(statementPeriod))
        {
            return BadRequest(new
            {
                message = "Statement period is required."
            });
        }

        var customer = await _db.Customers
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please select a PDF file."
            });
        }

        try
        {
            var result =
                await _fileStorage.SavePdfAsync(file);

            var statement = new Statement
            {
                CustomerId = customerId,
                StatementPeriod = statementPeriod.Trim(),
                FileName = Path.GetFileName(file.FileName),
                StoragePath = result.FilePath,
                FileHash = result.Hash
            };

            _db.Statements.Add(statement);

            await _db.SaveChangesAsync();

            await CreateAuditLog(
                statement.Id,
                "STATEMENT_UPLOADED");

            return CreatedAtAction(
                nameof(GetStatement),
                new { id = statement.Id },
                new
                {
                    statement.Id,
                    statement.StatementPeriod,
                    statement.FileName,
                    statement.CreatedAt
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================
    // GENERATE SECURE DOWNLOAD LINK
    // =========================================================

    // POST: api/statements/1/download-link
    [HttpPost("{id:int}/download-link")]
    public async Task<IActionResult> GenerateDownloadLink(
        int id)
    {
        var statement = await _db.Statements
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (statement == null)
        {
            return NotFound(new
            {
                message = "Statement not found."
            });
        }

        // Expire any old active links
        var oldTokens = await _db.DownloadTokens
            .Where(x =>
                x.StatementId == id &&
                x.RevokedAt == null &&
                x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var oldToken in oldTokens)
        {
            oldToken.RevokedAt = DateTime.UtcNow;
        }

        // Generate new secure token
        var rawToken = _secureLink.GenerateToken();

        var tokenHash =
            _secureLink.HashToken(rawToken);

        var downloadToken = new DownloadToken
        {
            StatementId = id,
            TokenHash = tokenHash,

            // Link expires after 30 minutes
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        _db.DownloadTokens.Add(downloadToken);

        await _db.SaveChangesAsync();

        await CreateAuditLog(
            statement.Id,
            "DOWNLOAD_LINK_CREATED");

        var downloadUrl =
            $"{Request.Scheme}://{Request.Host}" +
            $"/api/statements/download/{rawToken}";

        return Ok(new
        {
            message = "Secure download link created.",
            customer = statement.Customer!.FullName,
            statementId = statement.Id,
            expiresAt = downloadToken.ExpiresAt,
            downloadUrl
        });
    }

    // =========================================================
    // DOWNLOAD STATEMENT
    // =========================================================

    // GET: api/statements/download/{token}
    [AllowAnonymous]
    [HttpGet("download/{token}")]
    public async Task<IActionResult> DownloadStatement(
        string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new
            {
                message = "Invalid download token."
            });
        }

        var tokenHash =
            _secureLink.HashToken(token);

        var downloadToken =
            await _db.DownloadTokens
                .Include(x => x.Statement)
                .ThenInclude(x => x!.Customer)
                .FirstOrDefaultAsync(x =>
                    x.TokenHash == tokenHash);

        if (downloadToken == null)
        {
            return Unauthorized(new
            {
                message = "Invalid download link."
            });
        }

        if (downloadToken.RevokedAt != null)
        {
            return Unauthorized(new
            {
                message = "This download link has been revoked."
            });
        }

        if (downloadToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                message = "This download link has expired."
            });
        }

        if (downloadToken.UsedAt != null)
        {
            return Unauthorized(new
            {
                message = "This download link has already been used."
            });
        }

        var statement = downloadToken.Statement;

        if (statement == null)
        {
            return NotFound(new
            {
                message = "Statement not found."
            });
        }

        if (!System.IO.File.Exists(statement.StoragePath))
        {
            return NotFound(new
            {
                message = "Statement PDF could not be found."
            });
        }

        // Mark link as used
        downloadToken.UsedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await CreateAuditLog(
            statement.Id,
            "STATEMENT_DOWNLOADED");

        var fileBytes =
            await System.IO.File.ReadAllBytesAsync(
                statement.StoragePath);

        return File(
            fileBytes,
            "application/pdf",
            statement.FileName);
    }

    // =========================================================
    // REVOKE DOWNLOAD LINK
    // =========================================================

    // POST: api/statements/1/revoke-link
    [HttpPost("{id:int}/revoke-link")]
    public async Task<IActionResult> RevokeDownloadLink(
        int id)
    {
        var tokens = await _db.DownloadTokens
            .Where(x =>
                x.StatementId == id &&
                x.RevokedAt == null)
            .ToListAsync();

        if (tokens.Count == 0)
        {
            return NotFound(new
            {
                message = "No active download links found."
            });
        }

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await CreateAuditLog(
            id,
            "DOWNLOAD_LINK_REVOKED");

        return Ok(new
        {
            message = "Download link(s) revoked."
        });
    }

    // =========================================================
    // AUDIT LOG
    // =========================================================

    // GET: api/statements/1/audit
    [HttpGet("{id:int}/audit")]
    public async Task<IActionResult> GetAuditLogs(
        int id)
    {
        var statementExists =
            await _db.Statements
                .AnyAsync(x => x.Id == id);

        if (!statementExists)
        {
            return NotFound(new
            {
                message = "Statement not found."
            });
        }

        var logs = await _db.AuditLogs
            .Where(x => x.StatementId == id)
            .OrderByDescending(x => x.TimeStamp)
            .Select(x => new
            {
                x.Id,
                x.Action,
                x.TimeStamp,
                x.IpAddress,
                x.UserAgent
            })
            .ToListAsync();

        return Ok(logs);
    }

    // =========================================================
    // CREATE AUDIT LOG
    // =========================================================

    private async Task CreateAuditLog(
        int statementId,
        string action)
    {
        var log = new AuditLog
        {
            StatementId = statementId,
            Action = action,
            IpAddress =
                HttpContext.Connection.RemoteIpAddress?
                    .ToString(),
            UserAgent =
                Request.Headers.UserAgent.ToString()
        };

        _db.AuditLogs.Add(log);

        await _db.SaveChangesAsync();
    }
}