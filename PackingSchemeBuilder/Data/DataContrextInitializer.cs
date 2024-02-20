using SQLite.CodeFirst;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PackingSchemeBuilder.Data
{
    public class DataContrextInitializer : SqliteDropCreateDatabaseAlways<DataContext>
    {
        public DataContrextInitializer(DbModelBuilder modelBuilder) 
            : base(modelBuilder) { }
        protected override void Seed(DataContext context)
        {
        }
    }
}
