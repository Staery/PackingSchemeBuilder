using PackingSchemeBuilder.Data.Tables;
using SQLite.CodeFirst;
using System.Data.Entity;

namespace PackingSchemeBuilder.Data
{
    public class DataContext : DbContext
    {
        public DataContext()
            : base() { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<DataContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);

            var model = modelBuilder.Build(Database.Connection);
            ISqlGenerator sqlGenerator = new SqliteSqlGenerator();
            _ = sqlGenerator.Generate(model.StoreModel);
        }
        public DbSet<Bottle> Bottles { get; set; }
        public DbSet<Box> Boxes { get; set; }
        public DbSet<Pallet> Pallets { get; set; }
    }
}
