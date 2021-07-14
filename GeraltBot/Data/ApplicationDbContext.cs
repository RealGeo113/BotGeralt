using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeraltBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;

namespace GeraltBot.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Server> Servers { get; set; } 
        private Config _config { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {
            _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Server>().ToTable("Server");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql(String.Format("Host={0};Database={1};Username={2};Password={3}", _config.Database.Host, _config.Database.Name, _config.Database.User, _config.Database.Password));
    }
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        private Config _config { get; set; }

        public ApplicationDbContextFactory()
        {
            _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
        }
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(String.Format("Host={0};Database={1};Username={2};Password={3}", _config.Database.Host, _config.Database.Name, _config.Database.User, _config.Database.Password));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
