using Microsoft.EntityFrameworkCore;
using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Xml;
using Microsoft.Extensions.Configuration;

namespace AutoDealerSphere.Server.Services
{
	public class SQLDBContext : DbContext
	{
		protected readonly IConfiguration Configuration;
		public SQLDBContext(IConfiguration configuration)
		{
			Configuration = configuration;
		}
		public virtual DbSet<User> Users { get; set; }
		public virtual DbSet<AutoDealerSphere.Shared.Models.Client> Clients { get; set; }
		public virtual DbSet<Vehicle> Vehicles { get; set; }
		public virtual DbSet<Part> Parts { get; set; }
		public virtual DbSet<VehicleCategory> VehicleCategories { get; set; }
		public virtual DbSet<StatutoryFee> StatutoryFees { get; set; }
		public virtual DbSet<Invoice> Invoices { get; set; }
		public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }
		public virtual DbSet<IssuerInfo> IssuerInfos { get; set; }
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlite(Configuration.GetConnectionString("crm01"));
			}
			base.OnConfiguring(optionsBuilder);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id)
				.UseIdentityColumn();
			});

			modelBuilder.Entity<AutoDealerSphere.Shared.Models.Client>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id)
				.UseIdentityColumn();
			});

			modelBuilder.Entity<Vehicle>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id)
				.UseIdentityColumn();
				
				b.HasOne(v => v.Client)
					.WithMany()
					.HasForeignKey(v => v.ClientId)
					.OnDelete(DeleteBehavior.Cascade);
					
				b.HasOne(v => v.VehicleCategory)
					.WithMany()
					.HasForeignKey(v => v.VehicleCategoryId)
					.OnDelete(DeleteBehavior.SetNull);
			});

			modelBuilder.Entity<Part>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id).UseIdentityColumn();
			});

			modelBuilder.Entity<VehicleCategory>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id).UseIdentityColumn();
			});

			modelBuilder.Entity<StatutoryFee>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id).UseIdentityColumn();
				
				b.HasOne(s => s.VehicleCategory)
					.WithMany()
					.HasForeignKey(s => s.VehicleCategoryId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<Invoice>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id).UseIdentityColumn();
				b.HasIndex(e => new { e.InvoiceNumber, e.Subnumber }).IsUnique();
				
				b.HasOne(i => i.Client)
					.WithMany()
					.HasForeignKey(i => i.ClientId)
					.OnDelete(DeleteBehavior.Restrict);
					
				b.HasOne(i => i.Vehicle)
					.WithMany()
					.HasForeignKey(i => i.VehicleId)
					.OnDelete(DeleteBehavior.SetNull);
			});

			modelBuilder.Entity<InvoiceDetail>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id).UseIdentityColumn();
				
				b.HasOne(d => d.Invoice)
					.WithMany(i => i.InvoiceDetails)
					.HasForeignKey(d => d.InvoiceId)
					.OnDelete(DeleteBehavior.Cascade);
					
				b.HasOne(d => d.Part)
					.WithMany()
					.HasForeignKey(d => d.PartId)
					.OnDelete(DeleteBehavior.SetNull);
			});

			modelBuilder.Entity<IssuerInfo>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id).UseIdentityColumn();
			});
		}
	}
}
