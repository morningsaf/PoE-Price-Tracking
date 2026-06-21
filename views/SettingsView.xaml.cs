using System.Windows.Controls;
using System.Windows.Media;

namespace PoE_Price_Tracking.views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void LeagueComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LeagueComboBox.SelectedItem is ComboBoxItem item)
            {
                string league = item.Content.ToString()!;
                LeagueChanged?.Invoke(league);
            }
        }

        public event Action<string>? LeagueChanged;

        public void SetLeague(string league)
        {
            foreach (ComboBoxItem item in LeagueComboBox.Items)
            {
                if (item.Content.ToString() == league) 
                {
                    LeagueComboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }
}