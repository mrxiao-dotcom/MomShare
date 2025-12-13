using Microsoft.EntityFrameworkCore;
using MomShares.Core.Entities;

namespace MomShares.Infrastructure.Data;

/// <summary>
/// 应用程序数据库上下文
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Holder> Holders { get; set; }
    public DbSet<ProductNetValue> ProductNetValues { get; set; }
    public DbSet<HolderShare> HolderShares { get; set; }
    public DbSet<ShareTransaction> ShareTransactions { get; set; }
    public DbSet<Dividend> Dividends { get; set; }
    public DbSet<DividendDetail> DividendDetails { get; set; }
    public DbSet<DividendDistribution> DividendDistributions { get; set; }
    public DbSet<CapitalIncrease> CapitalIncreases { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<OperationLog> OperationLogs { get; set; }
    public DbSet<DistributionPlan> DistributionPlans { get; set; }
    public DbSet<Advisor> Advisors { get; set; }
    public DbSet<Manager> Managers { get; set; }
    public DbSet<DailyTotalEquity> DailyTotalEquities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CurrentNetValue).HasColumnType("decimal(18,4)");
            entity.Property(e => e.TotalShares).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.InitialAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.DistributionPlan)
                .WithOne(d => d.Product)
                .HasForeignKey<Product>(e => e.DistributionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Advisor)
                .WithMany(a => a.Products)
                .HasForeignKey(e => e.AdvisorId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Manager)
                .WithMany(m => m.Products)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
            entity.HasIndex(e => e.DistributionPlanId).IsUnique();
        });

        // 配置Holder
        modelBuilder.Entity<Holder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.BankAccount).HasMaxLength(50);
            entity.Property(e => e.AccountName).HasMaxLength(100);
            entity.HasIndex(e => e.Phone).IsUnique();
        });

        // 配置ProductNetValue
        modelBuilder.Entity<ProductNetValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NetValue).HasColumnType("decimal(18,4)");
            entity.HasOne(e => e.Product)
                .WithMany(p => p.NetValues)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.NetValueDate);
            entity.HasIndex(e => new { e.ProductId, e.NetValueDate });
        });

        // 配置DailyTotalEquity
        modelBuilder.Entity<DailyTotalEquity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Remarks).HasMaxLength(500);
            entity.HasIndex(e => e.RecordDate).IsUnique();
        });

        // 配置HolderShare
        modelBuilder.Entity<HolderShare>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShareAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.InvestmentAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ShareType).HasMaxLength(20).HasDefaultValue("Subordinate");
            entity.HasOne(e => e.Holder)
                .WithMany(h => h.HolderShares)
                .HasForeignKey(e => e.HolderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.HolderShares)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.HolderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.ShareType);
            entity.HasIndex(e => new { e.HolderId, e.ProductId, e.ShareType }).IsUnique();
        });

        // 配置ShareTransaction
        modelBuilder.Entity<ShareTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShareChange).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TransactionPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NetValueAtTime).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Remarks).HasMaxLength(500);
            entity.HasOne(e => e.Holder)
                .WithMany(h => h.ShareTransactions)
                .HasForeignKey(e => e.HolderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.ShareTransactions)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Counterparty)
                .WithMany(h => h.CounterpartyTransactions)
                .HasForeignKey(e => e.CounterpartyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.HolderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.TransactionType);
        });

        // 配置Dividend
        modelBuilder.Entity<Dividend>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Dividends)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.DividendDate);
        });

        // 配置DividendDetail
        modelBuilder.Entity<DividendDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ShareRatio).HasColumnType("decimal(18,6)");
            entity.HasOne(e => e.Dividend)
                .WithMany(d => d.DividendDetails)
                .HasForeignKey(e => e.DividendId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Holder)
                .WithMany(h => h.DividendDetails)
                .HasForeignKey(e => e.HolderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.DividendId);
            entity.HasIndex(e => e.HolderId);
        });

        // 配置CapitalIncrease
        modelBuilder.Entity<CapitalIncrease>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AmountBefore).HasColumnType("decimal(18,2)");
            entity.Property(e => e.IncreaseAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AmountAfter).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Product)
                .WithMany(p => p.CapitalIncreases)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.IncreaseDate);
        });

        // 配置Admin
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // 配置OperationLog
        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OperationDetails).HasColumnType("text");
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.HasIndex(e => e.OperationTime);
            entity.HasIndex(e => e.OperatorType);
            entity.HasIndex(e => e.OperatorId);
            entity.HasIndex(e => e.OperationType);
        });

        // 配置DistributionPlan
        modelBuilder.Entity<DistributionPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriorityRatio).HasColumnType("decimal(5,2)");
            entity.Property(e => e.SubordinateRatio).HasColumnType("decimal(5,2)");
            entity.Property(e => e.ManagerRatio).HasColumnType("decimal(5,2)");
            entity.Property(e => e.AdvisorRatio).HasColumnType("decimal(5,2)");
            entity.HasOne(e => e.Product)
                .WithOne(p => p.DistributionPlan)
                .HasForeignKey<DistributionPlan>(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ProductId).IsUnique();
        });

        // 配置Advisor
        modelBuilder.Entity<Advisor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(500);
        });

        // 配置Manager
        modelBuilder.Entity<Manager>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(500);
        });

        // 配置DividendDistribution
        modelBuilder.Entity<DividendDistribution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DistributionType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Ratio).HasColumnType("decimal(5,2)");
            entity.HasOne(e => e.Dividend)
                .WithMany()
                .HasForeignKey(e => e.DividendId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Manager)
                .WithMany(m => m.DividendDistributions)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Advisor)
                .WithMany(a => a.DividendDistributions)
                .HasForeignKey(e => e.AdvisorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.DividendId);
            entity.HasIndex(e => e.ManagerId);
            entity.HasIndex(e => e.AdvisorId);
        });
    }
}

