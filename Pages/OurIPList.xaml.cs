using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;
using ServerBrowser.Functions;

namespace ServerBrowser.Pages
{
    /// <summary>
    /// Interaction logic for OurIPList.xaml
    /// </summary>
    public partial class OurIPList : Page
    {
        private const string _filePath = "items.json";
        private HashSet<string> _items = new HashSet<string>();
        private DispatcherTimer _timer;
        private ObservableCollection<IPStatusItem> _listItems = new ObservableCollection<IPStatusItem>();
        private Dictionary<string, bool> _previousStatus = new Dictionary<string, bool>();

        public OurIPList()
        {
            InitializeComponent();

            DataContext = this; // Устанавливаем DataContext
            LoadItems(); // Загружаем IP-адреса из файла
            UpdateListBox(); // Обновляем ListBox для отображения загруженных IP-адресов
            StartCheckIPStatusTimer(); // Запускаем таймер для периодической проверки статуса
            CheckIPStatus(); // Выполняем проверку статуса сразу после загрузки
        }

        private void StartCheckIPStatusTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            //ShowNotification("список обновлен");
            await CheckIPStatus();
        }

        private async Task CheckIPStatus()
        {
            // Словарь для кэширования результатов
            var statusCache = new Dictionary<string, bool>();

            // Параллельная проверка статуса IP-адресов
            var tasks = _items.Select(async ip =>
            {
                // Проверяем, есть ли результат в кэше
                if (!statusCache.TryGetValue(ip, out bool isReachable))
                {
                    isReachable = await Helper.IsIPReachable(ip);
                    statusCache[ip] = isReachable; // Кэшируем результат
                }

                return (ip, isReachable);
            });

            // Ждем завершения всех задач
            var results = await Task.WhenAll(tasks);

            // Обновляем интерфейс после завершения всех проверок
            foreach (var (ip, isReachable) in results)
            {
                UpdateIPStatus(ip, isReachable);
            }
        }

        private void UpdateIPStatus(string ip, bool isReachable)
        {
            var statusColor = isReachable ? Brushes.Green : Brushes.Red;

            // Используем Dispatcher для обновления UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existingItem = _listItems.FirstOrDefault(item => item.IPAddress == ip);
                if (existingItem != null)
                {
                    if (_previousStatus.ContainsKey(ip) && _previousStatus[ip] != isReachable)
                    {
                        Helper.ShowNotification($"Статус IP-адреса {ip} изменился на {(isReachable ? "доступен" : "недоступен")}");
                    }
                    // Обновляем статус и цвет
                    existingItem.StatusColor = statusColor; // Это вызовет обновление UI
                }
                else
                {
                    // Если элемент не существует, добавляем его
                    _listItems.Add(new IPStatusItem
                    {
                        IPAddress = ip,
                        StatusColor = statusColor
                    });
                }

                _previousStatus[ip] = isReachable;
            });
        }

        public void UpdateListBox()
        {
            foreach (var item in _listItems)
            {
                // Если элемент уже существует, обновляем его
                var existingItem = _listItems.FirstOrDefault(i => i.IPAddress == item.IPAddress);
                if (existingItem != null)
                {
                    existingItem.StatusColor = item.StatusColor; // Обновляем цвет
                }
                else
                {
                    _listItems.Add(item); // Добавляем новый элемент
                }
            }
            listBoxItems.ItemsSource = _listItems; // Установите источник данных
        }

        private void LoadItems()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string jsonData = File.ReadAllText(_filePath);
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        // Десериализация JSON в HashSet<string>
                        var loadedItems = JsonConvert.DeserializeObject<HashSet<string>>(jsonData);
                        if (loadedItems != null)
                        {
                            _items = loadedItems;

                            // Очищаем listItems перед загрузкой новых элементов
                            _listItems.Clear();

                            // Загружаем IP-адреса в listItems
                            foreach (var ip in _items)
                            {
                                _listItems.Add(new IPStatusItem
                                {
                                    IPAddress = ip,
                                    StatusColor = Brushes.Gray // Или любой другой цвет по умолчанию
                                });
                            }
                        }
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                Helper.ShowError($"Ошибка при загрузке данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                Helper.ShowError($"Произошла ошибка: {ex.Message}");
            }
        }

        private void SaveItems()
        {
            try
            {
                // Сохраняем данные в файл синхронно
                File.WriteAllText(_filePath, JsonConvert.SerializeObject(_items, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Helper.ShowError($"Ошибка при сохранении данных: {ex.Message}");
            }
        }

        private async void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBoxInput.Text))
            {
                // Вызываем метод AddIpAddress
                if (AddIpAddress(textBoxInput.Text)) // Добавляем только если IP-адрес уникален
                {
                    await CheckIPStatus(); // Проверяем статус сразу после добавления
                    textBoxInput.Clear();
                }
                else
                {
                    Helper.ShowWarning("Элемент с таким значением уже существует или некорректен");
                }
            }
        }

        private void Button_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxItems.SelectedItem is IPStatusItem selectedItem)
            {
                // Удаляем IP-адрес из HashSet
                _items.Remove(selectedItem.IPAddress);

                // Удаляем элемент из ObservableCollection
                _listItems.Remove(selectedItem);

                // Сохраняем изменения в файл
                SaveItems();
            }
            else
            {
                Helper.ShowWarning("Не выбрана строка для удаления");
            }
        }

        private void Button_Shutdown_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxItems.SelectedItem is IPStatusItem selectedItem)
            {
                Helper.ShutDownBy(selectedItem.IPAddress, AppConfig.ShutdownDelaySeconds);
            }
        }

        public bool RemoveIpAddress(string ip)
        {
            // Проверяем, существует ли IP-адрес
            if (!_items.Contains(ip))
            {
                return false; // Возвращаем false, если IP-адрес не найден
            }

            // Удаляем IP-адрес из HashSet
            _items.Remove(ip);

            // Используем Dispatcher для удаления элемента из ObservableCollection
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Находим элемент в listItems и удаляем его
                var itemToRemove = _listItems.FirstOrDefault(item => item.IPAddress == ip);
                if (itemToRemove != null)
                {
                    _listItems.Remove(itemToRemove); // Удаляем элемент из ObservableCollection
                }
            });

            SaveItems(); // Сохраняем изменения в файл
            return true; // Возвращаем true, если IP-адрес был успешно удален
        }

        private void Button_ConnectTo_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxItems.SelectedItem is IPStatusItem selectedItem)
            {
                System.Diagnostics.Process.Start("CMD.exe", $"/C mstsc /v:{selectedItem.IPAddress}");
            }
        }

        private void Button_ShutdownAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _listItems)
            {
                Helper.ShutDownBy(item.IPAddress, AppConfig.ShutdownDelaySeconds);
            }
        }

        private void Button_AntiShutdown_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxItems.SelectedItem is IPStatusItem selectedItem)
            {
                Helper.AntiShutdown(selectedItem.IPAddress);
            }
        }

        // Метод для добавления IP-адреса
        public bool AddIpAddress(string ip)
        {
            // Проверяем, является ли строка корректным IP-адресом
            if (Helper.IsValidIp(ip))
            {
                bool added = false;

                // Используем Dispatcher для добавления IP-адреса
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_items.Add(ip)) // Добавляет IP-адрес и возвращает true, если он был добавлен
                    {
                        _listItems.Add(new IPStatusItem
                        {
                            IPAddress = ip,
                            StatusColor = Brushes.Gray // Или любой другой цвет по умолчанию
                        });
                        added = true;
                    }
                });

                if (added)
                {
                    SaveItems(); // Сохраняем изменения в файл
                    CheckIPStatus(); // Проверяем статус сразу после добавления
                    return true;
                }
            }
            return false; // Возвращаем false, если IP-адрес некорректен или уже существует
        }

        public List<string> GetAllIpAddresses()
        {
            return _items.ToList();
        }
    }
}