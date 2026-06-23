using System.Configuration;
using System.Data;
using System.Windows;

namespace PoE_Price_Tracking;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public void SetTheme(bool isDark)
    {
        Resources.MergedDictionaries.Clear();
        string theme = isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(theme, UriKind.Relative) });
    }
}

