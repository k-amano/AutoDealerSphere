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
		}
	}
}
