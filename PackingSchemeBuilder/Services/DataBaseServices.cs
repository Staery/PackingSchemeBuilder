using PackingSchemeBuilder.Data;
using PackingSchemeBuilder.Data.Tables;
using PackingSchemeBuilder.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace PackingSchemeBuilder.Services
{
    public class DataBaseServices
    {
        public DataBaseServices() { }

        public ObservableCollection<T> GetTableData<T, TKey>(Expression<Func<T, TKey>> orderBy) where T : class
        {
            using (var db = new DataContext())
            {
                var data = db.Set<T>().OrderBy(orderBy).ToList();

                return new ObservableCollection<T>(data);
            }
        }

        public void CreateDatabaseEntries(string code, CurrentTaskInfoModel taskInfo)
        {


            using (var db = new DataContext())
            {
                //db.Bottles.RemoveRange(db.Bottles);
                //db.Boxes.RemoveRange(db.Boxes);
                //db.Pallets.RemoveRange(db.Pallets);
                //db.Database.ExecuteSqlCommand("UPDATE SQLITE_SEQUENCE SET SEQ=0;");
                //db.SaveChanges();

                if (db.Bottles.Any(b => b.Code == code))
                    return;

                int lastBoxId = db.Bottles.Any() ? db.Bottles.Max(b => b.BoxId) : 0;
                int lastPalletId = db.Boxes.Any() ? db.Boxes.Max(b => b.PalletId) : 0;

                string gtin = taskInfo.mission.lot.product.gtin;
                int palletFormat = taskInfo.mission.lot.package.palletFormat;
                int boxFormat = taskInfo.mission.lot.package.boxFormat;




                var pallet = new Pallet();
                var box = new Box();
                var bottel = new Bottle();

                db.Bottles.Add(bottel);

                if (db.Boxes.Count(p => p.PalletId == lastPalletId) >=
                    palletFormat && db.Bottles.Count(p => p.BoxId == lastBoxId) >= 
                    boxFormat || db.Boxes.Count(p => p.PalletId == lastPalletId) == 0)
                {
                    db.Pallets.Add(pallet);

                    if (db.Bottles.Count(p => p.BoxId == lastBoxId) >= boxFormat || db.Bottles.Count(p => p.BoxId == lastBoxId) == 0)
                    {
                        db.Boxes.Add(box);
                        db.SaveChanges();

                        string boxCodet = $"01{gtin}37{db.Bottles.Count(b => b.BoxId == box.Id)}21{box.Id}";
                        box.Code = boxCodet;
                        box.PalletId = pallet.Id;
                    }

                    else
                    {
                        db.SaveChanges();
                    }
                    string palletCode = $"01{gtin}37{db.Boxes.Count(p => p.PalletId == pallet.Id)}21{pallet.Id}";
                    pallet.Code = palletCode;

                    bottel.Code = code;
                    if (lastBoxId == 0)
                    {
                        bottel.BoxId = box.Id;
                    }
                    else if (box.Id != 0)
                    {
                        bottel.BoxId = box.Id;
                    }
                    else
                    {
                        bottel.BoxId = lastBoxId;
                    }

                    string boxCode = $"01{gtin}37{db.Bottles.Count(b => b.BoxId == bottel.BoxId)}21{bottel.BoxId}";

                    foreach (var tbox in db.Boxes)
                    {
                        if (tbox.Id == bottel.BoxId)
                        {
                            tbox.Code = boxCode;
                        }
                    }

                    db.SaveChanges();
                }

                else
                {
                    if (db.Bottles.Count(p => p.BoxId == lastBoxId) >= boxFormat || db.Bottles.Count(p => p.BoxId == lastBoxId) == 0)
                    {
                        db.Boxes.Add(box);
                        db.SaveChanges();

                        string boxCodet = $"01{gtin}37{db.Bottles.Count(b => b.BoxId == box.Id)}21{box.Id}";
                        box.Code = boxCodet;
                        box.PalletId = lastPalletId;

                        db.SaveChanges();

                        string palletCode = $"01{gtin}37{db.Boxes.Count(p => p.PalletId == box.PalletId)}21{box.PalletId}";
                        foreach (var tpal in db.Pallets)
                        {
                            if (tpal.Id == box.PalletId)
                            {
                                tpal.Code = palletCode;
                            }
                        }
                    }

                    else
                    {
                        db.SaveChanges();
                    }

                    bottel.Code = code;
                    if (lastBoxId == 0)
                    {
                        bottel.BoxId = box.Id;
                    }
                    else if (box.Id != 0)
                    {
                        bottel.BoxId = box.Id;
                    }
                    else
                    {
                        bottel.BoxId = lastBoxId;
                    }

                    db.SaveChanges();

                    string boxCode = $"01{gtin}37{db.Bottles.Count(b => b.BoxId == bottel.BoxId)}21{bottel.BoxId}";

                    foreach (var tbox in db.Boxes)
                    {
                        if (tbox.Id == bottel.BoxId)
                        {
                            tbox.Code = boxCode;
                        }
                    }

                }
                db.SaveChanges();
            }
        }



    }
}


