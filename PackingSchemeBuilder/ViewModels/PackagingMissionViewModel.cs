using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using PackingSchemeBuilder.Commands;
using PackingSchemeBuilder.Data;
using PackingSchemeBuilder.Data.Tables;
using PackingSchemeBuilder.Models;
using PackingSchemeBuilder.Services;

namespace PackingSchemeBuilder.ViewModels
{
    public class PackagingMissionViewModel : INotifyPropertyChanged
    {

        #region OnPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private readonly MarkingAPIService _getCurrentTaskInfoService;
        private readonly FileService _fileService;
        private readonly DataBaseServices _dataBaseServices;
        public PackagingMissionViewModel() 
        {
            _getCurrentTaskInfoService = new MarkingAPIService();
            _fileService = new FileService("", new DataContext());
            _dataBaseServices = new DataBaseServices();
            InitializeCommands();
            LoadMissionCommand.Execute(null);
        }

        private ObservableCollection<Bottle> _bottleCollection  = new ObservableCollection<Bottle>();

        public ObservableCollection<Bottle> BottleCollection
        {
            get => _bottleCollection;
            set
            {
                _bottleCollection = value;
                OnPropertyChanged("BottleCollection");
            }
        }

        private ObservableCollection<Box> _boxCollection = new ObservableCollection<Box>();

        public ObservableCollection<Box> BoxCollection
        {
            get => _boxCollection;
            set
            {
                _boxCollection = value;
                OnPropertyChanged("BoxCollection");
            }
        }

        private ObservableCollection<Pallet> _palletCollection = new ObservableCollection<Pallet>();

        public ObservableCollection<Pallet> PalletCollection
        {
            get => _palletCollection;
            set
            {
                _palletCollection = value;
                OnPropertyChanged("PalletCollection");
            }
        }
        public ICommand LoadMissionCommand { get; private set; }
        public ICommand LoadDataFromFileCommand { get; private set; }

        public ICommand CreateMapCommand { get; private set; }
        private void InitializeCommands()
        {
            LoadMissionCommand = new RelayCommand(async () => await LoadMissionAsync(), () => !IsBusy);
            LoadDataFromFileCommand = new RelayCommand(async () => await LoadFromFileAsync(), () => !IsBusy);
            CreateMapCommand = new RelayCommand(async () => await CreateMapAsync(), () => !IsBusy);
        }


        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        private CurrentTaskInfoModel _missionInformation;

        public CurrentTaskInfoModel MissionInformation
        {
            get { return _missionInformation; }
            set
            {
                if (_missionInformation != value)
                {
                    _missionInformation = value;
                    OnPropertyChanged("MissionInformation");
                }
            }
        }



        private async Task LoadMissionAsync()
        {
            try
            {
                IsBusy = true;

                MissionInformation = await _getCurrentTaskInfoService.GetPackagingMissionInformation();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }


        private Task LoadFromFileAsync()
        {
            try
            {
                IsBusy = true;

                _fileService.SelectFile();
                _fileService.ReadeCodesFromFile(MissionInformation);
                BottleCollection = _dataBaseServices.GetTableData<Bottle, int>(bottle => bottle.Id);
                BoxCollection = _dataBaseServices.GetTableData<Box, int>(box => box.Id);
                PalletCollection = _dataBaseServices.GetTableData<Pallet, int>(pallet => pallet.Id);

                MessageBox.Show("Данные загружены");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }

            return Task.CompletedTask;
        }

        private Task CreateMapAsync()
        {
            try
            {
                IsBusy = true;
                if (MissionInformation != null && BottleCollection != null && BoxCollection != null && PalletCollection != null)
                {
                    _fileService.SerializeAllDataFromDatabase(MissionInformation, BottleCollection, BoxCollection, PalletCollection);
                    MessageBox.Show("Файл успешно создан.");
                }
                else
                {
                    MessageBox.Show("Ошибка генерации файла: Одно или несколько из необходимых значений равны null.");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка генерации файла json: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }

            return Task.CompletedTask;
        }
    }
}
