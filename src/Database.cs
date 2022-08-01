using Microsoft.EntityFrameworkCore;

namespace todo_odd;

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options)
    {

    }

    public DbSet<TodoItem> TodoItems { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>().ToTable("TodoItem");
    }
}

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}