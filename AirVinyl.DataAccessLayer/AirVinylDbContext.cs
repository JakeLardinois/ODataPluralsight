using AirVinyl.Model;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace AirVinyl.DataAccessLayer
{
    public class AirVinylDbContext : DbContext
    {
        public DbSet<Person> People { get; set; }
        public DbSet<VinylRecord> VinylRecords { get; set; }
        public DbSet<RecordStore> RecordStores { get; set; }
        public DbSet<PressingDetail> PressingDetails { get; set; }
        public DbSet<DynamicProperty> DynamicVinylRecordProperties { get; set; }

        public AirVinylDbContext()
        {
            Database.SetInitializer(new AirVinylDBInitializer());
            // disable lazy loading
            Configuration.LazyLoadingEnabled = false;          
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // ensure the same person can be added to different collections
            // of friends (self-referencing many-to-many relationship)
            modelBuilder.Entity<Person>().HasMany(m => m.Friends).WithMany();

            modelBuilder.Entity<Person>().HasMany(p => p.VinylRecords)
                .WithRequired(r => r.Person).WillCascadeOnDelete(true);
        }

        public override int SaveChanges()
        {
            // we need unchanged values as well - the object dictionary isn't tracked
            // by EF, thus changes to it will not be tracked either.
            var modifiedOrAddedVinylRecords = ChangeTracker.Entries<VinylRecord>()
                .Where(e => e.State == EntityState.Added
                        || e.State == EntityState.Modified
                        || e.State == EntityState.Unchanged).ToList();

            for (int i = 0; i < modifiedOrAddedVinylRecords.Count; i++)
            {
                var vinylRecord = modifiedOrAddedVinylRecords[i];

                // get the dynamic properties, and save them in a list
                var dynamicProperties = new List<DynamicProperty>();
                foreach (var dynamicPropertyKeyValue in vinylRecord.Entity.Properties)
                {
                    dynamicProperties
                        .Add(new DynamicProperty()
                        {
                            Key = dynamicPropertyKeyValue.Key,
                            Value = dynamicPropertyKeyValue.Value
                        });
                }

                // remove the current dynamic property references (this does
                // not remove the record itself!)
                vinylRecord.Entity.DynamicVinylRecordProperties.Clear();
                foreach (var dynamicPropertyToSave in dynamicProperties)
                {
                    // first, find & delete the actual record itself if it exists - this 
                    // avoids duplicate key errors.
                    var existingDynamicProperty =
                        ChangeTracker.Entries<DynamicProperty>()
                        .FirstOrDefault(d => d.Entity.Key == dynamicPropertyToSave.Key);

                    if (existingDynamicProperty != null)
                    {
                        DynamicVinylRecordProperties.Remove(existingDynamicProperty.Entity);
                    }

                    // add to the collection, so it's saved
                    vinylRecord.Entity.DynamicVinylRecordProperties.Add(dynamicPropertyToSave);
                }
            }
            return base.SaveChanges();
        }
    }
}
