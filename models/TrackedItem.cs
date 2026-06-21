using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoE_Price_Tracking
{
    public class TrackedItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        private string _league = "Standard";
        public string League
        {
            get => _league;
            set { _league = value; OnPropertyChanged(); OnPropertyChanged(nameof(LeagueIcon)); }
        }

        public string LeagueIcon => League switch
        {
            "Standard" => "assets/images/leagues/standard.png",
            "Hardcore" => "assets/images/leagues/hardcore.png",
            "Mirage" => "assets/images/leagues/mirage.png",
            _ => ""
        };

        private string _currencyIcon = "";
        public string CurrencyIcon
        {
            get => _currencyIcon;
            set { _currencyIcon = value; OnPropertyChanged(); }
        }
        private string _price = "Загрузка...";
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

        public static TrackedItem FromItem(Item item, string league = "Standard")
        {
            return new TrackedItem
            {
                TrendText = "",
                AmountText = "",
                TrendColor = "Black",
                Name = item.Name,
                Icon = item.Icon,
                League = league,
                Price = "",
                IsLoading = true,
                CurrencyIcon = ""
            };
        }
    }
}