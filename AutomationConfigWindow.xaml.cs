using System.Collections.ObjectModel;
using System.Windows;
using xCris.Models;

namespace xCris
{
    public partial class AutomationConfigWindow : Window
    {
        public ObservableCollection<AutomationBinding> Bindings { get; }

        public IReadOnlyList<AutomationBinding> SavedBindings { get; private set; } = Array.Empty<AutomationBinding>();

        public AutomationConfigWindow(IEnumerable<AutomationBinding> bindings)
        {
            InitializeComponent();
            Bindings = new ObservableCollection<AutomationBinding>(bindings.Select(binding => binding.Clone()));
            DataContext = this;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var binding = new AutomationBinding();
            Bindings.Add(binding);
            DgBindings.SelectedItem = binding;
            DgBindings.ScrollIntoView(binding);
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (DgBindings.SelectedItem is AutomationBinding binding)
            {
                Bindings.Remove(binding);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            DgBindings.CommitEdit();
            DgBindings.CommitEdit();
            SavedBindings = Bindings.Select(binding => binding.Clone()).ToList();
            DialogResult = true;
            Close();
        }
    }
}
