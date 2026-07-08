using ProductionControl.Domain.Auth;
using ProductionControl.Domain.Production;
using Microsoft.EntityFrameworkCore;

namespace ProductionControl.Persistence.Context;

public class ProductionControlDbContext : DbContext
{
    public ProductionControlDbContext(DbContextOptions<ProductionControlDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<PicCard> PicCards => Set<PicCard>();
    public DbSet<ShiftMaster> ShiftMasters => Set<ShiftMaster>();
    public DbSet<CuttingList> CuttingLists => Set<CuttingList>();
    public DbSet<ProductionWorkOrder> ProductionWorkOrders => Set<ProductionWorkOrder>();
    public DbSet<ProductionWorkOrderOperator> ProductionWorkOrderOperators => Set<ProductionWorkOrderOperator>();
    public DbSet<ProductionActivityLog> ProductionActivityLogs => Set<ProductionActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(80).IsRequired();
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(150);
            entity.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
            entity.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordSalt).HasColumnName("password_salt").HasMaxLength(255).IsRequired();
            entity.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<PicCard>(entity =>
        {
            entity.ToTable("pic_cards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CardUid).HasColumnName("card_uid").HasMaxLength(80).IsRequired();
            entity.Property(x => x.EmployeeNo).HasColumnName("employee_no").HasMaxLength(50).IsRequired();
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Department).HasColumnName("department").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Shift).HasColumnName("shift").HasMaxLength(30).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.LastScannedAt).HasColumnName("last_scanned_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.CardUid).IsUnique();
            entity.HasIndex(x => x.EmployeeNo).IsUnique();
        });

        modelBuilder.Entity<ShiftMaster>(entity =>
        {
            entity.ToTable("shift_masters");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ShiftCode).HasColumnName("shift_code").HasMaxLength(50).IsRequired();
            entity.Property(x => x.ShiftName).HasColumnName("shift_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.ShiftCode).IsUnique();
            entity.HasIndex(x => x.SortOrder);
        });

        modelBuilder.Entity<CuttingList>(entity =>
        {
            entity.ToTable("cutting_lists");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CuttingListNo).HasColumnName("cutting_list_no").HasMaxLength(80).IsRequired();
            entity.Property(x => x.ProductCode).HasColumnName("product_code").HasMaxLength(80).IsRequired();
            entity.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.LineCode).HasColumnName("line_code").HasMaxLength(50).IsRequired();
            entity.Property(x => x.PlannedQty).HasColumnName("planned_qty");
            entity.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(20).IsRequired();
            entity.Property(x => x.PlanDate).HasColumnName("plan_date");
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.CuttingListNo).IsUnique();
            entity.HasIndex(x => new { x.PlanDate, x.LineCode });
        });

        modelBuilder.Entity<ProductionWorkOrder>(entity =>
        {
            entity.ToTable("production_work_orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WoNumber).HasColumnName("wo_number").HasMaxLength(80).IsRequired();
            entity.Property(x => x.CuttingListId).HasColumnName("cutting_list_id");
            entity.Property(x => x.PicCardId).HasColumnName("pic_card_id");
            entity.Property(x => x.LineCode).HasColumnName("line_code").HasMaxLength(50).IsRequired();
            entity.Property(x => x.TargetQty).HasColumnName("target_qty");
            entity.Property(x => x.ActualQty).HasColumnName("actual_qty");
            entity.Property(x => x.RejectQty).HasColumnName("reject_qty");
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.StartedAt).HasColumnName("started_at");
            entity.Property(x => x.CompletedAt).HasColumnName("completed_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(x => x.CuttingList).WithMany().HasForeignKey(x => x.CuttingListId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PicCard).WithMany().HasForeignKey(x => x.PicCardId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => x.WoNumber).IsUnique();
            entity.HasIndex(x => new { x.Status, x.LineCode });
        });

        modelBuilder.Entity<ProductionWorkOrderOperator>(entity =>
        {
            entity.ToTable("production_work_order_operators");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductionWorkOrderId).HasColumnName("production_work_order_id");
            entity.Property(x => x.PicCardId).HasColumnName("pic_card_id");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.ScannedAt).HasColumnName("scanned_at");
            entity.Property(x => x.RemovedAt).HasColumnName("removed_at");
            entity.HasOne(x => x.ProductionWorkOrder).WithMany(x => x.Operators).HasForeignKey(x => x.ProductionWorkOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PicCard).WithMany().HasForeignKey(x => x.PicCardId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ProductionWorkOrderId, x.IsActive });
            entity.HasIndex(x => x.PicCardId);
        });

        modelBuilder.Entity<ProductionActivityLog>(entity =>
        {
            entity.ToTable("production_activity_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductionWorkOrderId).HasColumnName("production_work_order_id");
            entity.Property(x => x.PicCardId).HasColumnName("pic_card_id");
            entity.Property(x => x.ActivityType).HasColumnName("activity_type").HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Remarks).HasColumnName("remarks").HasColumnType("text");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.ProductionWorkOrder).WithMany(x => x.ActivityLogs).HasForeignKey(x => x.ProductionWorkOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PicCard).WithMany().HasForeignKey(x => x.PicCardId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.ProductionWorkOrderId, x.CreatedAt });
        });
    }
}
