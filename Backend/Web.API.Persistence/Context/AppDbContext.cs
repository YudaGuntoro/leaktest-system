using Microsoft.EntityFrameworkCore;
using Web.API.Domain.Auth;
using Web.API.Domain.Production;

namespace Web.API.Persistence.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<EngineModel> EngineModels => Set<EngineModel>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<LeakTestWorkRecord> LeakTestWorkRecords => Set<LeakTestWorkRecord>();
    public DbSet<ReworkEngineRecord> ReworkEngineRecords => Set<ReworkEngineRecord>();

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
            entity.Property(x => x.RolesId).HasColumnName("roles_id");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordSalt).HasColumnName("password_salt").HasMaxLength(255).IsRequired();
            entity.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RolesId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.RolesId);
        });

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("role_name").HasMaxLength(30).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(120);
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<EngineModel>(entity =>
        {
            entity.ToTable("engine_models");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ModelName).HasColumnName("engine_model").HasMaxLength(45).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(45);
            entity.Property(x => x.Note).HasColumnName("note").HasMaxLength(45);
            entity.Property(x => x.IsDeleted).HasColumnName("is_deleted");
            entity.HasIndex(x => x.ModelName).IsUnique();
        });

        modelBuilder.Entity<Operator>(entity =>
        {
            entity.ToTable("operators");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OperatorCode).HasColumnName("operator_code").HasMaxLength(50).IsRequired();
            entity.Property(x => x.OperatorName).HasColumnName("operator_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Department).HasColumnName("department").HasMaxLength(80);
            entity.Property(x => x.Note).HasColumnName("note").HasMaxLength(150);
            entity.Property(x => x.IsDeleted).HasColumnName("is_deleted");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.OperatorCode).IsUnique();
            entity.HasIndex(x => x.OperatorName);
        });

        modelBuilder.Entity<LeakTestWorkRecord>(entity =>
        {
            entity.ToTable("leak_test_work_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EngineModelId).HasColumnName("engine_model_id");
            entity.Property(x => x.EngineNumber).HasColumnName("engine_number").HasMaxLength(120).IsRequired();
            entity.Property(x => x.CheckDate).HasColumnName("check_date").HasColumnType("date");
            entity.Property(x => x.CheckTime).HasColumnName("check_time").HasMaxLength(8).IsRequired();
            entity.Property(x => x.MachineName).HasColumnName("machine_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.OperatorId).HasColumnName("operator_id");
            entity.Property(x => x.ParameterPressure).HasColumnName("parameter_pressure").HasPrecision(8, 2);
            entity.Property(x => x.PressureInput).HasColumnName("pressure_input").HasPrecision(8, 2);
            entity.Property(x => x.CycleTimeLeakTestMinutes).HasColumnName("cycle_time_leak_test_minutes").HasPrecision(8, 2);
            entity.Property(x => x.Result).HasColumnName("result").HasMaxLength(10).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(x => x.EngineModel).WithMany().HasForeignKey(x => x.EngineModelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Operator).WithMany().HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.CheckDate, x.EngineNumber });
            entity.HasIndex(x => x.EngineModelId);
            entity.HasIndex(x => x.OperatorId);
        });

        modelBuilder.Entity<ReworkEngineRecord>(entity =>
        {
            entity.ToTable("rework_engine_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EngineModelId).HasColumnName("engine_model_id");
            entity.Property(x => x.EngineModelText).HasColumnName("engine_model_text").HasMaxLength(80);
            entity.Property(x => x.EngineNumber).HasColumnName("engine_number").HasMaxLength(120).IsRequired();
            entity.Property(x => x.BarcodeScan).HasColumnName("barcode_scan").HasMaxLength(180).IsRequired();
            entity.Property(x => x.ReworkDate).HasColumnName("rework_date").HasColumnType("date");
            entity.Property(x => x.ReworkTime).HasColumnName("rework_time").HasMaxLength(8).IsRequired();
            entity.Property(x => x.OperatorId).HasColumnName("operator_id");
            entity.Property(x => x.ParameterPressure).HasColumnName("parameter_pressure").HasPrecision(8, 2);
            entity.Property(x => x.PressureInput).HasColumnName("pressure_input").HasPrecision(8, 2);
            entity.Property(x => x.Result).HasColumnName("result").HasMaxLength(10).IsRequired();
            entity.Property(x => x.Note).HasColumnName("note").HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(x => x.EngineModel).WithMany().HasForeignKey(x => x.EngineModelId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Operator).WithMany().HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.ReworkDate, x.EngineNumber });
            entity.HasIndex(x => x.EngineModelId);
            entity.HasIndex(x => x.OperatorId);
        });
    }
}
