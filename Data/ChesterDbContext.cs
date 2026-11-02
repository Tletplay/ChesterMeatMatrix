using System.Data.Entity;
using ChesterMeatMatrix.Models;

namespace ChesterMeatMatrix.Data
{
    public class ChesterDbContext : DbContext
    {
        public ChesterDbContext() : base("name=MeatMatrixDB")
        {
        }

        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Route> Routes { get; set; }
        public virtual DbSet<Vehicle> Vehicles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // simple configurations if needed
            modelBuilder.Entity<Product>().HasKey(p => p.Id);
            modelBuilder.Entity<Customer>().HasKey(c => c.Id);
            modelBuilder.Entity<Order>().HasKey(o => o.Id);
            modelBuilder.Entity<User>().HasKey(u => u.Id);
            modelBuilder.Entity<Route>().HasKey(r => r.Id);
            modelBuilder.Entity<Vehicle>().HasKey(v => v.Id);
        }
    }
}
