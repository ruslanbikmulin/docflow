namespace Docflow.DAL.EntityContext
{
    using Docflow.DAL.Models;
    using System.Data.Entity;

    public class DocumentContext : DbContext
    {
        public DocumentContext() : base("DBConnection")
        {
            Configuration.ProxyCreationEnabled = true;
            Configuration.LazyLoadingEnabled = true;
        }
        
        public DbSet<UploadEntity> UploadEntityTable { get; set; }

        public DbSet<UploadProgress> UploadProgressTable { get; set; }

        public DbSet<ScanPath> ScanPaths { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UploadEntity>().HasMany(up => up.UploadProgressRows).WithRequired(c => c.UploadEntity).HasForeignKey(x => x.UploadEntityId);
            modelBuilder.Entity<UploadProgress>().HasMany(up => up.ScanPaths).WithRequired(c => c.UploadProgress).HasForeignKey(x => x.UploadProgressId);
        }
    }
}