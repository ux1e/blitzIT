using System.ComponentModel;
using System.Windows.Media;

namespace ServerBrowser
{
    public class IPStatusItem : INotifyPropertyChanged
    {
        private string ipAddress;
        private Brush statusColor;

        public string IPAddress
        {
            get => ipAddress;
            set
            {
                ipAddress = value;
                OnPropertyChanged(nameof(IPAddress));
            }
        }

        public Brush StatusColor
        {
            get => statusColor;
            set
            {
                statusColor = value;
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Переопределяем метод ToString()
        public override string ToString()
        {
            return IPAddress; // Возвращаем только IP-адрес
        }
    }
}
