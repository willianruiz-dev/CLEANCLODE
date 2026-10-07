using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System.Reflection;

namespace LocalDataBase
{

    public class LocalContext : DbContext
    {
        //public LocalContext( DbContextOptions<LocalContext> options) : base(options) { }
        public DbSet<DB_Transaction> DB_Transactions { get; set; }
        public DbSet<DB_TransactionDetail> DB_TransactionDetails {  get; set; }
        public DbSet<DB_UserPersonalInfo> DB_UserPersonalInfos {  get; set; }

        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["SqlServerConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'SqlServerConnection' not found in configuration.");
            }
            
            optionsBuilder.UseSqlServer(connectionString);
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
    }
}