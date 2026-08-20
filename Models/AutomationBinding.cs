using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace xCris.Models
{
    public class AutomationBinding : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
        private string _name = "New binding";
        private string _selector = "button";
        private string _eventType = "click";
        private string _propertyName = string.Empty;
        private string _propertyValue = string.Empty;
        private string _actionType = "ApplicationCommand";
        private string _actionTarget = "ShowMessage";
        private string _actionArgument = "Triggered by {selector}";

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Selector
        {
            get => _selector;
            set { _selector = value; OnPropertyChanged(); }
        }

        public string EventType
        {
            get => _eventType;
            set { _eventType = value; OnPropertyChanged(); }
        }

        public string PropertyName
        {
            get => _propertyName;
            set { _propertyName = value; OnPropertyChanged(); }
        }

        public string PropertyValue
        {
            get => _propertyValue;
            set { _propertyValue = value; OnPropertyChanged(); }
        }

        public string ActionType
        {
            get => _actionType;
            set { _actionType = value; OnPropertyChanged(); }
        }

        public string ActionTarget
        {
            get => _actionTarget;
            set { _actionTarget = value; OnPropertyChanged(); }
        }

        public string ActionArgument
        {
            get => _actionArgument;
            set { _actionArgument = value; OnPropertyChanged(); }
        }

        public AutomationBinding Clone() =>
            new()
            {
                IsEnabled = IsEnabled,
                Name = Name,
                Selector = Selector,
                EventType = EventType,
                PropertyName = PropertyName,
                PropertyValue = PropertyValue,
                ActionType = ActionType,
                ActionTarget = ActionTarget,
                ActionArgument = ActionArgument
            };

        public override string ToString() => $"{Name} ({EventType} → {ActionType})";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
