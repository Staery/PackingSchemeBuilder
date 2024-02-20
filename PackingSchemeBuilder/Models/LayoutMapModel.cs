using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackingSchemeBuilder.Models
{
    public class PalletMap
    {
        public int id { get; set; }
        public string code { get; set; }
        public List<BoxMap> boxes { get; set; }
    }

    public class BoxMap
    {
        public int id { get; set; }
        public string code { get; set; }
        public List<BottleMap> bottles { get; set; }
    }

    public class BottleMap
    {
        public int id { get; set; }
        public string code { get; set; }
    }

    public class LayoutMap
    {
        public string productName { get; set; }
        public string gtin { get; set; }
        public int boxFormat { get; set; }
        public int palletFormat { get; set; }
        public PalletMap pallet { get; set; }
    }
}
