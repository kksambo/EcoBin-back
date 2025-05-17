using Microsoft.EntityFrameworkCore;

namespace WasteManagement.Models
{
    public class WasteManagementContext : DbContext
    {
        public WasteManagementContext(DbContextOptions<WasteManagementContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<SmartBin> SmartBins { get; set; }
        public DbSet<WasteItem> WasteItems { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<DepositRequest> DepositRequests { get; set; }
        

       
    }
}