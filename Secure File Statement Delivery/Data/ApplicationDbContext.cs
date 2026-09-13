using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Secure_File_Statement_Delivery.Models;

namespace Secure_File_Statement_Delivery.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers =>
        Set<Customer>();

    public DbSet<Statement> Statements =>
        Set<Statement>();

    public DbSet<DownloadToken> DownloadTokens =>
        Set<DownloadToken>();

    public DbSet<AuditLog> AuditLogs =>
        Set<AuditLog>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>()
            .HasIndex(x => x.AccountNumber)
            .IsUnique();

        modelBuilder.Entity<Statement>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Statements)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DownloadToken>()
            .HasOne(x => x.Statement)
            .WithMany(x => x.DownloadTokens)
            .HasForeignKey(x => x.StatementId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuditLog>()
            .HasOne(x => x.Statement)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.StatementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}