using Microsoft.Win32;
using Newtonsoft.Json;
using PackingSchemeBuilder.Data;
using PackingSchemeBuilder.Data.Tables;
using PackingSchemeBuilder.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Shapes;


namespace PackingSchemeBuilder.Services
{

    public class FileService
    {
        private string _filePath;
        private readonly DataContext _dbContext;
        private readonly DataBaseServices _dbServices;

        public FileService(string filePath, DataContext dbContext)
        {
            _filePath = filePath;
            _dbContext = dbContext;
            _dbServices = new DataBaseServices();
        }

        public void ReadeCodesFromFile(CurrentTaskInfoModel taskInfo)
        {
            try
            {
                //Здесь удалил символ GS в некоторых случаях подобные символы нужны для сканеров, но по заданию я не видел что бы в базу они писались.
                string[] codes = File.ReadAllLines(_filePath)
    .Select(line => Regex.Replace(line, @"[\x00-\x1F\x7F]", string.Empty))
    .ToArray();

                List<string> filteredCodes = codes.Where(code => code.Contains(taskInfo.mission.lot.product.gtin)).ToList();

                foreach (string code in filteredCodes)
                {
                    _dbServices.CreateDatabaseEntries(code, taskInfo);
                }
            }
            catch (Exception ex)
            {
                HandleFileException("Ошибка чтения файла", ex);
            }

        }



        public void SerializeAllDataFromDatabase(
            CurrentTaskInfoModel taskInfo,
            ObservableCollection<Bottle> bottleCollection,
            ObservableCollection<Box> boxCollection,
            ObservableCollection<Pallet> palletCollection)
        {
            using (var dbContext = new DataContext())
            {
                List<LayoutMap> mapList = new List<LayoutMap>();

                foreach (var pallet in palletCollection)
                {
                    var palletMap = new PalletMap()
                    {
                        id = pallet.Id,
                        code = pallet.Code,
                        boxes = new List<BoxMap>()  // Initialize the list of boxes
                    };

                    foreach (var box in boxCollection.Where(b => b.PalletId == pallet.Id))
                    {
                        var boxMap = new BoxMap()
                        {
                            id = box.Id,
                            code = box.Code,
                            bottles = new List<BottleMap>()
                        };

                        foreach (var bottle in bottleCollection.Where(b => b.BoxId == box.Id))
                        {
                            var bottleMap = new BottleMap()
                            {
                                id = bottle.Id,
                                code = bottle.Code
                            };
                            boxMap.bottles.Add(bottleMap);
                        }

                        palletMap.boxes.Add(boxMap); // Add the boxMap to the list of boxes in palletMap

                    }

                    LayoutMap map = new LayoutMap()
                    {
                        productName = taskInfo.mission.lot.product.name,
                        gtin = taskInfo.mission.lot.product.gtin,
                        boxFormat = taskInfo.mission.lot.package.boxFormat,
                        palletFormat = taskInfo.mission.lot.package.palletFormat,
                        pallet = palletMap
                    };

                    mapList.Add(map);
                }

                string json = SerializeToJson(mapList);

                var path = Directory.GetCurrentDirectory() +
                           $"\\{taskInfo.mission.lot.product.gtin}_result_file_{DateTime.Now.Day}{DateTime.Now.Month}{DateTime.Now.Year}_{DateTime.Now.Hour}{DateTime.Now.Minute}.json";

                File.WriteAllText(path, json);
            }
        }



        private string SerializeToJson(object obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch (JsonException ex)
            {
                HandleFileException("Ошибка сериализации JSON", ex);
            }

            return null;
        }

        public void SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                _filePath = openFileDialog.FileName;
            }
        }

        private void HandleFileException(string errorMessage, Exception ex)
        {
            throw new Exception(errorMessage + ": " + ex.Message, ex);
        }
    }
}
