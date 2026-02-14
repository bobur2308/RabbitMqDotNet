using Microsoft.EntityFrameworkCore;
using RabbitMq.WebApi.Models;

namespace RabbitMq.WebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<Message> Messages { get; set; }
}