using System.Windows;
using System.Windows.Input;

namespace PoE_Price_Tracking
{
    public partial class MiniWindow : Window
    {
        public MiniWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // Будет привязано из MainWindow
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            Application.Current.MainWindow.Show();
        }
    }
}