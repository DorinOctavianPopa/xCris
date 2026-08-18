using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace xCris.Models
{
    public class DomElement : INotifyPropertyChanged
    {
        private string _tagName = string.Empty;
        private string _id = string.Empty;
        private string _className = string.Empty;
        private string _innerText = string.Empty;
        private string _innerHTML = string.Empty;
        private string _selector = string.Empty;

        public string TagName
        {
            get => _tagName;
            set { _tagName = value; OnPropertyChanged(); }
        }

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string ClassName
        {
            get => _className;
            set { _className = value; OnPropertyChanged(); }
        }

        public string InnerText
        {
            get => _innerText;
            set { _innerText = value; OnPropertyChanged(); }
        }

        public string InnerHTML
        {
            get => _innerHTML;
            set { _innerHTML = value; OnPropertyChanged(); }
        }

        public string Selector
        {
            get => _selector;
            set { _selector = value; OnPropertyChanged(); }
        }

        public override string ToString() =>
            string.IsNullOrEmpty(Id)
                ? (string.IsNullOrEmpty(ClassName) ? TagName : $"{TagName}.{ClassName.Replace(" ", ".")}")
                : $"{TagName}#{Id}";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
