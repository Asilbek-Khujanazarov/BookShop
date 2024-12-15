using Microsoft.EntityFrameworkCore;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<ArchivedBook> ArchivedBooks { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Qo'shimcha sozlashlar
        modelBuilder.Entity<User>()
            .Property(u => u.IsAdmin)
            .HasDefaultValue(false); // IsAdmin ustuni uchun default qiymat false
    }
    
}


