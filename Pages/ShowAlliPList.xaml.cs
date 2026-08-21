using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;

namespace ServerBrowser.Pages
{
    /// <summary>
    /// Interaction logic for ShowAlliPList.xaml
    /// </summary>

    public partial class ShowAlliPList : Page
    {
        private ConcurrentBag<string> results;
        private string originalTitle;

        public ShowAlliPList()
        {
            InitializeComponent();
            results = new ConcurrentBag<string>();
            originalTitle = Application.Current.MainWindow.Title;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadResultsFromJsonFile();
        }

        private void LoadResultsFromJsonFile()
        {
            try
            {
                string jsonData = File.ReadAllText("results.json");
                if (!string.IsNullOrEmpty(jsonData))
                {
                    List<string> dataList = JsonConvert.DeserializeObject<List<string>>(jsonData);
                    dataList.Sort(CompareIPAddresses);
                    Dispatcher.Invoke(() => listBoxItems.ItemsSource = dataList);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void buttonSearch_Click(object sender, RoutedEventArgs e)
        {
            await ScanNetworkAsync();
        }

        private async Task ScanNetworkAsync()
        {
            results = new ConcurrentBag<string>();
            Application.Current.MainWindow.Title = "Сканирование сети...";
            await Task.Run(() => ScanNetworkCore());
            SaveResultsToJsonFile();
            Dispatcher.Invoke(() =>
            {
                List<string> sortedResults = results.ToList();
                sortedResults.Sort((a, b) => CompareIPAddresses(a, b));
                listBoxItems.ItemsSource = sortedResults;
                Application.Current.MainWindow.Title = originalTitle;
            });
        }

        private void ScanNetworkCore()
        {
            string subnet = ServerBrowser.AppConfig.ScanSubnet;
            Parallel.For(1, 256, i => PingHost(subnet + i));
        }

        private void PingHost(string ip)
        {
            using (var ping = new Ping())
            {
                try
                {
                    PingReply reply = ping.Send(ip, 100);
                    if (reply.Status == IPStatus.Success)
                    {
                        string hostName = Dns.GetHostEntry(ip).HostName;
                        string result = $"IP: {ip}, Host: {hostName}";
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error pinging {ip}: {ex.Message}");
                }
            }
        }

        private void SaveResultsToJsonFile()
        {
            List<string> sortedResults = results.ToList();
            sortedResults.Sort((a, b) => CompareIPAddresses(a, b));
            string json = JsonConvert.SerializeObject(sortedResults, Formatting.Indented);
            File.WriteAllText("results.json", json);
        }

        private static int CompareIPAddresses(string a, string b)
        {
            string ipA = ExtractIPAddress(a);
            string ipB = ExtractIPAddress(b);

            string[] partsA = ipA.Split('.');
            string[] partsB = ipB.Split('.');

            for (int i = 0; i < 4; i++)
            {
                int partA = int.Parse(partsA[i]);
                int partB = int.Parse(partsB[i]);

                if (partA < partB)
                    return -1;
                else if (partA > partB)
                    return 1;
            }

            return 0; // IP-адреса равны
        }

        private static string ExtractIPAddress(string result)
        {
            int startIndex = result.IndexOf("IP: ") + 4;
            int endIndex = result.IndexOf(",");
            return result.Substring(startIndex, endIndex - startIndex);
        }
    }
}
