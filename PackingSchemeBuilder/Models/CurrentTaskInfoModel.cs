using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackingSchemeBuilder.Models
{
    public class CurrentTaskInfoModel
    {
        public MissionModel mission { get; set; }
    }

    public class MissionModel
    {
        public LotModel lot { get; set; }
    }

    public class LotModel
    {
        public PackageModel package { get; set; }
        public ProductModel product { get; set; }
    }

    public class PackageModel
    {
        public string volume { get; set; }
        public int boxFormat { get; set; }
        public int palletFormat { get; set; }
    }

    public class ProductModel
    {
        public string name { get; set; }
        public string gtin { get; set; }
    }
}
