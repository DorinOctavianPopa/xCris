using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace xCris.Models
{
    public class PageEvent : INotifyPropertyChanged
    {
        private string _eventType = string.Empty;
        private string _targetSelector = string.Empty;
        private string _detail = string.Empty;
        private DateTime _timestamp = DateTime.Now;

        public string EventType
        {
            get => _eventType;
            set { _eventType = value; OnPropertyChanged(); }
        }

        public string TargetSelector
        {
            get => _targetSelector;
            set { _targetSelector = value; OnPropertyChanged(); }
        }

        public string Detail
        {
            get => _detail;
            set { _detail = value; OnPropertyChanged(); }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public string FormattedTimestamp => _timestamp.ToString("HH:mm:ss.fff");

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
