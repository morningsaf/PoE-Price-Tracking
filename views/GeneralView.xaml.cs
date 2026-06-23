using System.Windows.Controls;
using System.Diagnostics;
using System.Windows;

namespace PoE_Price_Tracking.views
{
    public partial class GeneralView : UserControl
    {
        public GeneralView()
        {
            InitializeComponent();
        }

        private void TradeLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url && !string.IsNullOrEmpty(url))
                System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}