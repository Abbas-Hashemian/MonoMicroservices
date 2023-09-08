using Microsoft.EntityFrameworkCore;
using MonoMicroservices.SearchDomain.DataModels;

namespace MonoMicroservices.SearchDataAccess;

public class SearchDbContext : DbContext
{
	public SearchDbContext(DbContextOptions<SearchDbContext> options)
		: base(options) { }
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);
	}
	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);
		//builder.Entity
	}

	//public DbSet<WebPage> WebPages { get; set; }
}

