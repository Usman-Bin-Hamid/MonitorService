using Microsoft.EntityFrameworkCore;
using SharedModels;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using Machine = SharedModels.Machine;

namespace ServiceMonitoringAPI.Context
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Machine> Machines { get; set; }
        public DbSet<Service> Services { get; set; }
    }
}
