using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Drawing;


namespace PoE_Price_Tracking;

public partial class MainWindow : Window
{
    private TrackedItemsService _trackedService;
    private List<string> _trackedNames;
    private List<TrackedItem>? trackedItems;
    private AppDbContext _db;
    private List<Item> _allItems;
    private string _baseDir = "";
    public MainWindow()
    {
        InitializeComponent();
        _db = new AppDbContext();
        string _baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _allItems = _db.Items.OrderBy(i => i.Name).ToList();
        foreach (var item in _allItems)
        {
            item.Icon = System.IO.Path.Combine(_baseDir, "assets", "images", "items",$"{item.Name}_orig.png");
        }
        string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tracked_items.json");
        _trackedService = new TrackedItemsService(filePath);
        _trackedNames = _trackedService.Load();
        if (_trackedNames.Count > 0)
        {
            var itemsToTrack = _allItems.Where(item => _trackedNames.Contains(item.Name)).ToList();
            trackedItems = itemsToTrack.Select(item => TrackedItem.FromItem(item)).ToList();
            foreach(var tracked in trackedItems)
            {
                var catalogItem = _allItems.FirstOrDefault(i => i.Name == tracked.Name);
                if(catalogItem == null) continue;

                var lastPrice = _db.Prices.Where(p => p.ItemId == catalogItem.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                if(lastPrice != null)
                {
                    tracked.TrendText = "(Outdated)";
                    tracked.TrendColor = "Black";
                    tracked.AmountText = $"{lastPrice.Price}";
                    tracked.CurrencyIcon = System.IO.Path.Combine(_baseDir, "assets/images/currency", lastPrice.Currency + ".png");
                }
                else
                {
                    tracked.TrendText = "";
                    tracked.AmountText = "";
                    tracked.Price = "";
                    tracked.CurrencyIcon = "";
                }
                tracked.IsLoading = false;
            }
            GeneralViewControl.MainTable.ItemsSource = trackedItems;
        }
        else
        {
            GeneralViewControl.MainTable.ItemsSource = new List<TrackedItem>();
        }
    }

    private async Task FetchPricesStreaming(List<string> itemNames, Action<PriceEntry> onPriceReceived)
    {

        string input = string.Join(",", itemNames);

        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"scripts/price_taker.py \"{input}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi)!;
        using var reader = process.StandardOutput;
        using var errorReader = process.StandardError;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            var price = JsonSerializer.Deserialize<PriceEntry>(line);
            if (price != null)
            {
                Dispatcher.Invoke(() => onPriceReceived(price));
            }
        }
        string error = await errorReader.ReadToEndAsync();
        process.WaitForExit();
        if (!string.IsNullOrEmpty(error))
        {
            Dispatcher.Invoke(() => MessageBox.Show("Ошибка Python:\n" + error));
        }

    }

    private async void RefreshPrices_Click(object sender, RoutedEventArgs e)
    {
        if (trackedItems == null || trackedItems.Count == 0) return;
        var names = trackedItems.Select(t => t.Name).ToList();
        try
        {
            foreach (var item in trackedItems)
            {
                item.IsLoading = true;
                item.TrendText = "";
                item.AmountText = "";          
                item.CurrencyIcon = "";
            }
            await FetchPricesStreaming(names, price =>
            {
                var item = trackedItems.FirstOrDefault(t => t.Name == price.Name);
                if (item != null)
                {
                    string trend = price.Trend ?? "";
                    if (string.IsNullOrEmpty(trend) || trend == "+0.00%" || trend == "0%")
                        item.TrendColor = "Gray";
                    else if (trend.StartsWith("-"))
                        item.TrendColor = "Red";
                    else
                        item.TrendColor = "Green";

                    item.TrendText = trend;
                    item.AmountText = $"{price.Amount}";
                    item.CurrencyIcon = System.IO.Path.Combine(_baseDir, "assets/images/currency", price.Currency + ".png");
                    item.IsLoading = false;
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.StackTrace}");
        }
    }

    private void SelectItems_Click(object sender, RoutedEventArgs e)
    {
        var selectedNames = new List<string>(_trackedNames);
        var window = new ItemSelectionWindow(_allItems, selectedNames);
        window.Owner = this;
        if (window.ShowDialog() == true)
        {
            _trackedNames = new List<string>(window.GetSelectedNames());
            _trackedService.Save(_trackedNames);
            var itemsToTrack = _allItems.Where(item => _trackedNames.Contains(item.Name)).ToList();
            trackedItems = itemsToTrack.Select(item => TrackedItem.FromItem(item)).ToList();
            foreach(var tracked in trackedItems)
            {
                var catalogItem = _allItems.FirstOrDefault(i => i.Name == tracked.Name);
                if(catalogItem == null) continue;

                var lastPrice = _db.Prices.Where(p => p.ItemId == catalogItem.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                if(lastPrice != null)
                {
                    tracked.TrendText = "(Outdated)";
                    tracked.TrendColor = "Black";
                    tracked.AmountText = $"{lastPrice.Price}";
                    tracked.CurrencyIcon = System.IO.Path.Combine(_baseDir, "assets/images/currency", lastPrice.Currency + ".png");
                }
                else
                {
                    tracked.TrendText = "";
                    tracked.AmountText = "";
                    tracked.Price = "";
                    tracked.CurrencyIcon = "";
                }
                tracked.IsLoading = false;
            }
            GeneralViewControl.MainTable.ItemsSource = trackedItems;
        }
    }
}
public class TrackedItem : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    private string _price = "Загрузка...";

    private string _currencyIcon = "";
    public string CurrencyIcon
{
    get => _currencyIcon;
    set { _currencyIcon = value; OnPropertyChanged(); }
}
    public string Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(); }
    }
    private bool _isLoading = true;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }
    private string _trendText = "";
    public string TrendText
    {
        get => _trendText;
        set { _trendText = value; OnPropertyChanged(); }
    }

    private string _amountText = "";
    public string AmountText
    {
        get => _amountText;
        set { _amountText = value; OnPropertyChanged(); }
    }

    private string _trendColor = "Gray";
    public string TrendColor
    {
        get => _trendColor;
        set { _trendColor = value; OnPropertyChanged(); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static TrackedItem FromItem(Item item)
    {
        return new TrackedItem
        {
            TrendText = "",
            AmountText = "",
            TrendColor = "Black",
            Name = item.Name,
            Icon = item.Icon,
            Price = "",
            IsLoading = true,
            CurrencyIcon = ""
        };
    }
}