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
using System.Runtime.InteropServices;
using System.Windows.Interop;


namespace PoE_Price_Tracking;

public partial class MainWindow : Window
{
    private bool _isBanned = false;
    private Task? _currentRefreshTask;
    private CancellationTokenSource? _refreshCts;
    private MiniWindow? _miniWindow;
    private TrackedItemsService _trackedService;
    private List<string> _trackedNames = new();
    private List<TrackedItem>? trackedItems;
    private AppDbContext _db;
    private List<Item> _allItems = new();
    private string _baseDir = "";
    private string _currentLeague = "Standard";
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hwnd, IntPtr hRgn, bool bRedraw);

    [DllImport("gdi32.dll")]
    private static extern int DeleteObject(IntPtr hObject);

    private const int CORNER_RADIUS = 12;
    public MainWindow()
    {
        InitializeComponent();
        this.SourceInitialized += (s, e) =>
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int width = (int)this.ActualWidth;
            int height = (int)this.ActualHeight;
            IntPtr region = CreateRoundRectRgn(0, 0, width + 1, height + 1, CORNER_RADIUS, CORNER_RADIUS);
            SetWindowRgn(hwnd, region, true);
            DeleteObject(region);
        };
        _baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _db = new AppDbContext();
        _allItems = _db.Items.OrderBy(i => i.Name).ToList();
        foreach (var item in _allItems)
        {
            item.Icon = System.IO.Path.Combine(_baseDir, "assets", "images", "items", $"{item.Name}_orig.png");
        }

        string filePath = System.IO.Path.Combine(_baseDir, "tracked_items.json");
        _trackedService = new TrackedItemsService(filePath);
        UserData userData = _trackedService.Load();
        _trackedNames = userData.TrackedItems;
        _currentLeague = userData.League;
        StatusLeague.Text = _currentLeague;
        SettingsViewControl.SetLeague(_currentLeague);
        SettingsViewControl.LeagueChanged += (league) =>
        {
            _miniWindow?.SetLeague(league);
            _currentLeague = league;
            StatusLeague.Text = _currentLeague;
            foreach (var tracked in trackedItems ?? new())
            {
                tracked.League = league;
                string cleanName = tracked.Name.Contains(" (") 
                    ? tracked.Name.Substring(0, tracked.Name.LastIndexOf(" (")) 
                    : tracked.Name;
                tracked.Name = $"{cleanName} ({league})";
            }
            UserData data = new UserData { League = league, TrackedItems = _trackedNames };
            _trackedService.Save(data);
            RefreshTrackedTable();
        };
        ItemCatalogView!.Initialize(_allItems, _trackedNames);
        RefreshTrackedTable();
        RefreshPrices_Click(null!, null!);

        ItemCatalogView!.CartChanged += () =>
        {
            _trackedNames = new List<string>(ItemCatalogView.GetSelectedNames());
            UserData data = new UserData
            {
                League = _currentLeague,
                TrackedItems = _trackedNames
            };
            _trackedService.Save(data);
            RefreshTrackedTable();
        };
    }

    private async Task FetchPricesStreaming(List<string> itemNames, Action<PriceEntry> onPriceReceived)
    {
        _refreshCts?.Cancel();
        if (_currentRefreshTask != null)
        {
            await _currentRefreshTask;
        }
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;
        _currentRefreshTask = Task.Run(async () =>
        {
            string input = string.Join(",", itemNames);

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"scripts/price_taker.py --items \"{input}\" --league {_currentLeague}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi)!;
            token.Register(() => { try { process.Kill(); } catch { } });
            using var reader = process.StandardOutput;
            using var errorReader = process.StandardError;
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (token.IsCancellationRequested) break;

                var price = JsonSerializer.Deserialize<PriceEntry>(line);
                if (price != null)
                {
                    Dispatcher.Invoke(() => onPriceReceived(price));
                }
            }
            if (!token.IsCancellationRequested)
            {
                string error = await errorReader.ReadToEndAsync();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(error))
                {
                    Dispatcher.Invoke(() => MessageBox.Show("Ошибка Python:\n" + error));
                }
            }
        });
        await _currentRefreshTask;
    }

    private async void RefreshPrices_Click(object sender, RoutedEventArgs e)
    {
        _isBanned = false;
        if (trackedItems == null || trackedItems.Count == 0) return;
        var names = _trackedNames;
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
                if (_isBanned) return;
                if (price.Error == "ban")
                {
                    _isBanned = true;
                    Dispatcher.Invoke(() => 
                    {
                        StatusParser.Text = $"Banned for {price.RetryAfter} seconds";
                        StatusParser.Foreground = new SolidColorBrush(Colors.Red);
                    });
                    return;
                }
                var item = trackedItems.FirstOrDefault(t => t.Name == price.Name);
                if (item != null)
                {
                    if(price.Amount == null || price.Trend == "none")
                    {
                        item.TrendText = "No Data";
                        item.TrendColor = "Gray";
                        item.AmountText = "";
                        item.CurrencyIcon = "";
                        item.IsLoading = false;
                        return;
                    }
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
            if(!_isBanned)
            {
                StatusParser.Text = "OK";
                StatusParser.Foreground = new SolidColorBrush(Colors.Green);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.StackTrace}");
        }
    }

    private void RefreshTrackedTable()
    {
        if (_trackedNames.Count == 0)
        {
            GeneralViewControl.MainTable.ItemsSource = new List<TrackedItem>();
            return;
        }

        var newTrackedItems = new List<TrackedItem>();

        foreach (var name in _trackedNames)
        {
            var catalogItem = _allItems.FirstOrDefault(i => i.Name == name);
            if (catalogItem == null) continue;
            var tracked = TrackedItem.FromItem(catalogItem, _currentLeague);
            var lastPrice = _db.Prices.Where(p => p.ItemId == catalogItem.Id && p.League == _currentLeague).OrderByDescending(p => p.Id).FirstOrDefault();
            if (lastPrice != null)
            {
                tracked.TrendText = "";
                tracked.AmountText = "";
                tracked.CurrencyIcon = "";
                tracked.IsLoading = true;
            }
            else
            {
                tracked.TrendText = "";
                tracked.AmountText = "";
                tracked.CurrencyIcon = "";
            }
            tracked.IsLoading = false;
            newTrackedItems.Add(tracked);
        }
        trackedItems = newTrackedItems;
        GeneralViewControl.MainTable.ItemsSource = trackedItems;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            this.DragMove();
    }

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }


    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        if (_miniWindow == null)
        {
            _miniWindow = new MiniWindow();
            _miniWindow.Closed += (s, args) => _miniWindow = null;
        }
        _miniWindow.SetLeague(_currentLeague);
        _miniWindow.RefreshAction = () => RefreshPrices_Click(null!, null!);
        _miniWindow.MiniTable.ItemsSource = trackedItems;
        _miniWindow.Show();
        this.Hide();
    }
}