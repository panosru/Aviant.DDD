namespace Aviant.DDD.Infrastructure.Persistence.Contexts
{
    using System.Linq;
    using Exceptions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;

    internal interface IDbContextReadImplementation
    {
        public void OnPreBaseModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
               .SelectMany(e => e.GetForeignKeys()))
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;

            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }

        public static void TrackerSettings(ChangeTracker changeTracker)
        {
            changeTracker.LazyLoadingEnabled    = false;
            changeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public static void ThrowWriteException()
        {
            throw new InfrastructureException("Read-only context");
        }
    }
}