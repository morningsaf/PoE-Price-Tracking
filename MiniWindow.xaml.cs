using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;

namespace PoE_Price_Tracking
{
    public partial class MiniWindow : Window
    {
        public Action? RefreshAction { get; set; }
        public MiniWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAction?.Invoke();
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            Application.Current.MainWindow.Show();
        }

        public void SetLeague(string league)
        {
            StatusLeague.Text = league;
        }

        public void SetParserStatus(string text, string color)
        {
            StatusParser.Text = text;
            var converter = new System.Windows.Media.BrushConverter();
            StatusParser.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(color)!;
        }

        private void TradeLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url && !string.IsNullOrEmpty(url))
                System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}