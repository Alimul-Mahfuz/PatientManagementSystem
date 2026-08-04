using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PatientManagementSystem.Models.Entities;

namespace PatientManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Condition> Conditions => Set<Condition>();
        public DbSet<PatientCondition> PatientConditions => Set<PatientCondition>();
        public DbSet<Ward> Wards => Set<Ward>();
        public DbSet<Bed> Beds => Set<Bed>();
        public DbSet<BedAssignment> BedAssignments => Set<BedAssignment>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(b =>
            {
                b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
                b.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<Patient>(b =>
            {
                b.ToTable("Patients");
                b.HasKey(p => p.Id);
                b.Property(p => p.Mrn).HasMaxLength(20).IsRequired();
                b.HasIndex(p => p.Mrn).IsUnique();
                b.Property(p => p.FullName).HasMaxLength(150).IsRequired();
                b.Property(p => p.Phone).HasMaxLength(40);
                b.Property(p => p.Email).HasMaxLength(150);
                b.Property(p => p.Address).HasMaxLength(300);
                b.Property(p => p.EmergencyContact).HasMaxLength(150);
                b.Property(p => p.BloodGroup).HasMaxLength(10);
                b.Property(p => p.Allergies).HasMaxLength(400);
                b.Property(p => p.Status).HasConversion<int>();
                b.Property(p => p.Gender).HasConversion<int>();
                b.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<Condition>(b =>
            {
                b.ToTable("Conditions");
                b.HasKey(c => c.Id);
                b.Property(c => c.Name).HasMaxLength(150).IsRequired();
                b.HasIndex(c => c.Name).IsUnique();
                b.Property(c => c.Icd10Code).HasMaxLength(20);
                b.Property(c => c.Description).HasMaxLength(400);
                b.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<PatientCondition>(b =>
            {
                b.ToTable("PatientConditions");
                b.HasKey(pc => pc.Id);
                b.HasOne(pc => pc.Patient).WithMany().HasForeignKey(pc => pc.PatientId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(pc => pc.Condition).WithMany().HasForeignKey(pc => pc.ConditionId).OnDelete(DeleteBehavior.Restrict);
                b.Property(pc => pc.Severity).HasConversion<int>();
                b.Property(pc => pc.DiagnosedDate).HasDefaultValueSql("GETUTCDATE()");
                b.Property(pc => pc.Notes).HasMaxLength(400);
                b.HasIndex(pc => new { pc.PatientId, pc.ConditionId }).IsUnique();
            });

            builder.Entity<Ward>(b =>
            {
                b.ToTable("Wards");
                b.HasKey(w => w.Id);
                b.Property(w => w.Name).HasMaxLength(100).IsRequired();
                b.HasIndex(w => w.Name).IsUnique();
                b.Property(w => w.Floor).HasMaxLength(20);
                b.Property(w => w.Description).HasMaxLength(300);
                b.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<Bed>(b =>
            {
                b.ToTable("Beds");
                b.HasKey(be => be.Id);
                b.Property(be => be.Number).HasMaxLength(20).IsRequired();
                b.HasOne(be => be.Ward).WithMany(w => w.Beds)
                    .HasForeignKey(be => be.WardId).OnDelete(DeleteBehavior.Cascade);
                b.Property(be => be.Status).HasConversion<int>();
                b.Property(be => be.Notes).HasMaxLength(300);
                b.Property(be => be.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                b.HasIndex(be => new { be.WardId, be.Number }).IsUnique();
            });

            builder.Entity<BedAssignment>(b =>
            {
                b.ToTable("BedAssignments");
                b.HasKey(a => a.Id);
                b.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(a => a.Bed).WithMany().HasForeignKey(a => a.BedId).OnDelete(DeleteBehavior.Restrict);
                b.Property(a => a.AdmissionDate).HasDefaultValueSql("GETUTCDATE()");
                b.Property(a => a.Notes).HasMaxLength(400);
                b.HasIndex(a => new { a.BedId, a.IsActive }).IsUnique()
                    .HasFilter("[IsActive] = 1"); // only one active assignment per bed
            });

            builder.Entity<Invoice>(b =>
            {
                b.ToTable("Invoices");
                b.HasKey(i => i.Id);
                b.Property(i => i.Number).HasMaxLength(30).IsRequired();
                b.HasIndex(i => i.Number).IsUnique();
                b.Property(i => i.Status).HasConversion<int>();
                b.Property(i => i.SubTotal).HasColumnType("decimal(18,2)");
                b.Property(i => i.TaxPercent).HasColumnType("decimal(5,2)");
                b.Property(i => i.TaxAmount).HasColumnType("decimal(18,2)");
                b.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
                b.Property(i => i.Notes).HasMaxLength(400);
                b.HasOne(i => i.Patient).WithMany().HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.Restrict);
                b.Property(i => i.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<InvoiceItem>(b =>
            {
                b.ToTable("InvoiceItems");
                b.HasKey(it => it.Id);
                b.HasOne(it => it.Invoice).WithMany(i => i.Items)
                    .HasForeignKey(it => it.InvoiceId).OnDelete(DeleteBehavior.Cascade);
                b.Property(it => it.Description).HasMaxLength(200).IsRequired();
                b.Property(it => it.UnitPrice).HasColumnType("decimal(18,2)");
                b.Property(it => it.Total).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Permission>(b =>
            {
                b.ToTable("Permissions");
                b.HasKey(p => p.Id);

                // Stable ID: never auto-generated. Seeder assigns explicit IDs so
                // reseeding never shifts them — protecting existing role/user references.
                b.Property(p => p.Id).ValueGeneratedNever();
                b.Property(p => p.Key).HasMaxLength(100).IsRequired();
                b.HasIndex(p => p.Key).IsUnique();
                b.Property(p => p.Name).HasMaxLength(150).IsRequired();
                b.Property(p => p.Description).HasMaxLength(400);
                b.Property(p => p.Module).HasMaxLength(60).IsRequired();
                b.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<RolePermission>(b =>
            {
                b.ToTable("RolePermissions");
                b.HasKey(rp => new { rp.RoleId, rp.PermissionId });

                b.HasOne(rp => rp.Role)
                    .WithMany()
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(rp => rp.Permission)
                    .WithMany()
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.Property(rp => rp.GrantedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<UserPermission>(b =>
            {
                b.ToTable("UserPermissions");
                b.HasKey(up => new { up.UserId, up.PermissionId });

                b.HasOne(up => up.User)
                    .WithMany()
                    .HasForeignKey(up => up.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(up => up.Permission)
                    .WithMany()
                    .HasForeignKey(up => up.PermissionId)
                    .OnDelete(DeleteBehavior.Restrict); // never cascade-delete a referenced permission

                b.Property(up => up.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}