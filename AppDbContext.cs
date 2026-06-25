using Microsoft.EntityFrameworkCore;

namespace PoE_Price_Tracking
{
    public class AppDbContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<PriceRecord> Prices { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=poe.db");
        }
    }
}