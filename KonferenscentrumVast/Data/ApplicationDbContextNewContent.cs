// using System;
// using KonferenscentrumVast.Models;
// using Microsoft.EntityFrameworkCore;

// namespace KonferenscentrumVast.Data
// {
//     public class ApplicationDbContext : DbContext
//     {
//         public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
//         {
//         }

//         public DbSet<Customer> Customers { get; set; }
//         public DbSet<Facility> Facilities { get; set; }
//         public DbSet<Booking> Bookings { get; set; }
//         public DbSet<BookingContract> BookingContracts { get; set; }
//     }
// }

using Microsoft.EntityFrameworkCore;
using KonferenscentrumVast.Models;

namespace KonferenscentrumVast.Data
{
    // EF Core DbContext (ApplicationDbContext)
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // DbSet för upload metadata
        public DbSet<UploadFile> UploadFiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfigurera UploadFile tabell (Fluent API)
            modelBuilder.Entity<UploadFile>(entity =>
            {
                entity.ToTable("upload_files");

                entity.Property(e => e.Id).HasColumnName("id").IsRequired();
                entity.Property(e => e.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(1024);
                entity.Property(e => e.StoredFileName).HasColumnName("stored_file_name").HasMaxLength(1024);
                entity.Property(e => e.ContentType).HasColumnName("content_type").HasMaxLength(256);
                entity.Property(e => e.Size).HasColumnName("size");
                entity.Property(e => e.BlobUrl).HasColumnName("blob_url");
                entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(256);
                entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");
                entity.Property(e => e.Checksum).HasColumnName("checksum").HasMaxLength(128);

                // Indexar ofta sökta kolumner
                entity.HasIndex(e => e.UploadedAt).HasDatabaseName("ix_upload_files_uploaded_at");
            });
        }
    }
}
