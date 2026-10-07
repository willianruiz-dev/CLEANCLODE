using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace LocalDataBase
{
    public class LocalSqliteContext : DbContext
    {
        public DbSet<DB_Transaction> DB_Transactions { get; set; }
        public DbSet<DB_TransactionDetail> DB_TransactionDetails { get; set; }
        public DbSet<DB_UserPersonalInfo> DB_UserPersonalInfos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "local_hospital.db");
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DB_Transaction>().ToTable("DB_Transaction");
            modelBuilder.Entity<DB_Transaction>(ent =>
            {
                ent.HasKey(e => e.TransactionId);
                ent.Property(e => e.TransactionId).ValueGeneratedOnAdd();
                ent.HasIndex(e => e.IdApi).IsUnique();
            });

            modelBuilder.Entity<DB_UserPersonalInfo>().ToTable("DB_UserPersonalInfo");
            modelBuilder.Entity<DB_UserPersonalInfo>(ent =>
            {
                ent.HasKey(e => e.Document);
                ent.Property(e => e.Document).ValueGeneratedNever();
            });

            modelBuilder.Entity<DB_TransactionDetail>().ToTable("DB_TransactionDetail");
            modelBuilder.Entity<DB_TransactionDetail>(ent =>
            {
                ent.HasKey(e => e.TranDetailId);
                ent.Property(e => e.TranDetailId).ValueGeneratedOnAdd();
                ent.HasIndex(e => e.IdApi).IsUnique();
                ent.HasOne<DB_Transaction>()
                   .WithMany()
                   .HasForeignKey(e => e.IdTransaction)
                   .OnDelete(DeleteBehavior.Restrict);
            });
            base.OnModelCreating(modelBuilder);
        }

        public static void EnsureCreated()
        {
            try
            {
                using var context = new LocalSqliteContext();
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalSqliteContext] Error al crear la base de datos: {ex.Message}");
            }
        }
    }
}
