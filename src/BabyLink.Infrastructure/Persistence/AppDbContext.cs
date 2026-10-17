// ה-DbContext: DbSet<User>, DbSet<ServiceRequest>, DbSet<Tag>.
// OnModelCreating: ApplyConfigurationsFromAssembly - טוען את כל קבצי ה-Configurations.
// דריסת SaveChangesAsync: לכל ServiceRequest שהשתנה - Version = Guid.NewGuid()
// (כך שאף Service לא צריך לזכור לעשות את זה).
