using MamMap.Data.Configurations;
using MamMap.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MamMap.Data.EF
{
    public class MamMapDBContext : IdentityDbContext<AspNetUsers, AspNetRoles, Guid>
    {
        public MamMapDBContext(DbContextOptions<MamMapDBContext> options) : base(options) { }

        public DbSet<SnackPlaces> SnackPlaces { get; set; }
        public DbSet<Dishes> Dishes { get; set; }
        public DbSet<BusinessModels> BusinessModels { get; set; }
        public DbSet<Tastes> Tastes { get; set; }
        public DbSet<Diets> Diets { get; set; }
        public DbSet<FoodTypes> FoodTypes { get; set; }
        public DbSet<Reviews> Reviews { get; set; }
        public DbSet<Replies> Replies { get; set; }
        public DbSet<ReviewRecommendation> ReviewRecommendations { get; set; }
        public DbSet<SnackPlaceAttributes> SnackPlaceAttributes { get; set; }
        public DbSet<SnackPlaceClick> SnackPlaceClicks { get; set; }
        public DbSet<Payments> Payment { get; set; }
        public DbSet<PremiumPackage> PremiumPackages { get; set; }
        public DbSet<PackageDescription> PackageDescriptions { get; set; }
        public DbSet<UserPremiumPackage> UserPremiumPackages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<SnackPlaces>()
                .HasOne(sp => sp.User)
                .WithMany()
                .HasForeignKey(sp => sp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SnackPlaces>()
                .HasOne(sp => sp.BusinessModels)
                .WithMany() 
                .HasForeignKey(sp => sp.BusinessModelId);


            modelBuilder.Entity<Dishes>()
                .HasOne(d => d.SnackPlace)
                .WithMany()
                .HasForeignKey(d => d.SnackPlaceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SnackPlaceAttributes>()
                .HasOne(s => s.SnackPlace)
                .WithMany(sp => sp.SnackPlaceAttributes)
                .HasForeignKey(s => s.SnackPlaceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SnackPlaceAttributes>()
                .HasOne(s => s.Taste)
                .WithMany(t => t.SnackPlaceAttributes)
                .HasForeignKey(s => s.TasteId);

            modelBuilder.Entity<SnackPlaceAttributes>()
                .HasOne(s => s.Diet)
                .WithMany(d => d.SnackPlaceAttributes)
                .HasForeignKey(s => s.DietId);

            modelBuilder.Entity<SnackPlaceAttributes>()
                .HasOne(s => s.FoodType)
                .WithMany(f => f.SnackPlaceAttributes)
                .HasForeignKey(s => s.FoodTypeId);

            modelBuilder.Entity<Reviews>()
                .Property(r => r.Comment)
                .HasMaxLength(2000);

            modelBuilder.Entity<Reviews>()
                .HasOne(r => r.SnackPlace)
                .WithMany()
                .HasForeignKey(r => r.SnackPlaceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reviews>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReviewRecommendation>(entity =>
            {
                entity.HasKey(rr => new { rr.ReviewId, rr.UserId });

                entity.HasOne(rr => rr.Review)
                    .WithMany()
                    .HasForeignKey(rr => rr.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rr => rr.User)
                    .WithMany()
                    .HasForeignKey(rr => rr.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Payments>(entity =>
            {
                entity.ToTable("Payments");

                entity.HasKey(p => p.Id);

                entity.Property(p => p.Amount)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(p => p.TransactionId)
                      .HasMaxLength(100);

                entity.Property(p => p.PaymentCode)
                      .HasMaxLength(100);

                entity.Property(p => p.PaymentStatus)
                      .IsRequired()
                      .HasDefaultValue(false);

                entity.Property(p => p.CreatedAt)
                      .HasDefaultValueSql("GETDATE()");

                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.PremiumPackage)
                      .WithMany()
                      .HasForeignKey(p => p.PremiumPackageId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserPremiumPackage>()
                .HasOne(x => x.User)
                .WithMany(u => u.UserPremiumPackages)
                .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<UserPremiumPackage>()
                .HasOne(x => x.PremiumPackage)
                .WithMany(p => p.UserPremiumPackages)
                .HasForeignKey(x => x.PremiumPackageId);

            modelBuilder.Entity<Replies>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Comment)
                    .IsRequired();

                entity.Property(r => r.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(r => r.Status)
                    .HasDefaultValue(true);

                entity.HasOne(r => r.Review)
                    .WithMany()
                    .HasForeignKey(r => r.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.ParentReply)
                    .WithMany(r => r!.Reply!)
                    .HasForeignKey(r => r.ParentReplyId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Dishes>().HasKey(d => d.DishId);
            modelBuilder.Entity<SnackPlaces>().HasKey(sp => sp.SnackPlaceId);
            modelBuilder.Entity<BusinessModels>().HasKey(sp => sp.BusinessModelId);
            modelBuilder.Entity<Tastes>().HasKey(sp => sp.Id);
            modelBuilder.Entity<Diets>().HasKey(sp => sp.Id);
            modelBuilder.Entity<FoodTypes>().HasKey(sp => sp.Id);
            modelBuilder.Entity<Reviews>().HasKey(sp => sp.Id);
            modelBuilder.Entity<ReviewRecommendation>().HasKey(sp => sp.ReviewRecommendationId);
            modelBuilder.Entity<SnackPlaceClick>().HasKey(sp => sp.Id);
            modelBuilder.Entity<SnackPlaceClick>().HasKey(sp => sp.Id);
            modelBuilder.Entity<Payments>().HasKey(p => p.Id);
            modelBuilder.Entity<PremiumPackage>().HasKey(p => p.Id);
            modelBuilder.Entity<Replies>().HasKey(p => p.Id);
            modelBuilder.Entity<UserPremiumPackage>().HasKey(x => new { x.UserId, x.PremiumPackageId });
        }
    }
}
