using HD.Station.Authorization.Demo.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HD.Station.Authorization.Demo.Data
{
    public class ContactDbContext : DbContext
    {
        public ContactDbContext( DbContextOptions<ContactDbContext> options) :base (options)
        {

        }
        public DbSet<Contact> Contacts { set; get; }
    }
}
