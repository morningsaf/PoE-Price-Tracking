using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace PoE_Price_Tracking.views
{
    public partial class ItemCatalogView : UserControl
    {
        private Dictionary<string, Dictionary<string, string>> _subCategories = new()
        {
            { "weapon", new() {
                {"Axe", "axe"}, {"Bow", "bow"}, {"Claw", "claw"}, {"Dagger", "dagger"},
                {"Rod", "rod"}, {"Mace", "mace"}, {"Sceptre", "sceptre"}, {"Staff", "staff"},
                {"Sword", "sword"}, {"Wand", "wand"}
            }},
            { "armour", new() {
                {"Body Armour", "body_armour"}, {"Boots", "boots"}, {"Gloves", "gloves"},
                {"Helmet", "helmet"}, {"Shield", "shield"}, {"Quiver", "quiver"}
            }},
            { "jewellery", new() {
                {"Amulet", "amulet"}, {"Belt", "belt"}, {"Ring", "ring"}
            }},
            { "jewel", new() {
                {"Jewel", "jewel"}
            }},
            { "map", new() {
                {"Map", "map"}
            }},
            { "flask", new() {
                {"Life Flask", "life_flask"}, {"Mana Flask", "mana_flask"},
                {"Hybrid Flask", "hybrid_flask"}, {"Utility Flask", "utility_flask"}
            }},
        };

        private List<Item> _allItems;
        private List<Item> _filteredItems;
        private List<string> _selectedNames;
        private Popup? _currentPopup;

        public event Action? CartChanged;

        public ItemCatalogView()
        {
            InitializeComponent();
            _allItems = new List<Item>();
            _filteredItems = new List<Item>();
            _selectedNames = new List<string>();
            LoadCategories();
            CartListBox.ItemsSource = _selectedNames;
            ItemsListBox.ItemsSource = _filteredItems;
        }

        public void Initialize(List<Item> allItems, List<string> trackedNames)
        {
            _allItems = allItems;
            _selectedNames = new List<string>(trackedNames);
            _filteredItems = allItems.Take(50).ToList();
            CartListBox.ItemsSource = null;
            CartListBox.ItemsSource = _selectedNames;
            ItemsListBox.ItemsSource = null;
            ItemsListBox.ItemsSource = _filteredItems;
            this.IsVisibleChanged += (s, e) =>
            {
                if (!this.IsVisible && _currentPopup != null)
                    _currentPopup.IsOpen = false;
            };
            SearchBox.Focus();
            RestoreSelection();
        }

        public List<string> GetSelectedNames() => _selectedNames;

        private void LoadCategories()
        {
            string[] categories = { "weapon", "armour", "jewellery", "jewel", "map", "flask" };
            string[] displayNames = { "Weapon", "Armour", "Jewellery", "Jewel", "Map", "Flask" };
            for (int i = 0; i < categories.Length; i++)
            {
                Button btn = new Button
                {
                    Content = displayNames[i],
                    Width = 80, Height = 26,
                    Margin = new Thickness(2),
                    Tag = categories[i],
                    Style = (Style)FindResource("SubCategoryButtonStyle")
                };
                btn.MouseEnter += CategoryButton_MouseEnter;
                btn.MouseLeave += CategoryButton_MouseLeave;
                FilterPanel.Children.Add(btn);
            }
        }

        private void CategoryButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_currentPopup != null && _currentPopup.IsOpen)
                _currentPopup.IsOpen = false;

            Button btn = (Button)sender;
            string category = (string)btn.Tag;
            _currentPopup = new Popup
            {
                PlacementTarget = btn,
                Placement = PlacementMode.Bottom,
                StaysOpen = true,
                AllowsTransparency = true
            };

            StackPanel panel = new StackPanel
            {
                Background = System.Windows.Media.Brushes.White,
                MinWidth = 100
            };

            foreach (var kvp in _subCategories[category])
            {
                string displayName = kvp.Key;
                string dbValue = kvp.Value;
                Button subBtn = new Button
                {
                    Content = displayName,
                    Style = (Style)FindResource("SubCategoryButtonStyle")
                };
                subBtn.Click += (s, args) =>
                {
                    _filteredItems = _allItems.Where(i => i.ItemSubType == dbValue).ToList();
                    ItemsListBox.ItemsSource = _filteredItems;
                    RestoreSelection();
                    _currentPopup.IsOpen = false;
                };
                panel.Children.Add(subBtn);
            }
            panel.MouseLeave += (s, args) => _currentPopup.IsOpen = false;

            _currentPopup.Child = panel;
            _currentPopup.IsOpen = true;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = SearchBox.Text.ToLower();

            if (string.IsNullOrEmpty(filter))
            {
                ItemsListBox.ItemsSource = _filteredItems;
                SuggestionsPopup.IsOpen = false;
                return;
            }

            var suggestions = _allItems.Where(i => i.Name.ToLower().Contains(filter)).Take(10).ToList();

            if (suggestions.Count > 0 && filter.Length >= 2)
            {
                SuggestionsListBox.ItemsSource = suggestions;
                SuggestionsPopup.IsOpen = true;
            }
            else
            {
                SuggestionsPopup.IsOpen = false;
            }

            ItemsListBox.ItemsSource = _allItems.Where(i => i.Name.ToLower().Contains(filter)).Take(30).ToList();
            RestoreSelection();
        }

        private void SuggestionsListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SuggestionsListBox.SelectedItem is Item selected)
            {
                SearchBox.Text = selected.Name;
                SuggestionsPopup.IsOpen = false;
                SearchBox.CaretIndex = SearchBox.Text.Length;
            }
        }

        private void RemoveFromCart_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string itemName = (string)btn.Tag;
            _selectedNames.Remove(itemName);
            CartListBox.ItemsSource = null;
            CartListBox.ItemsSource = _selectedNames;

            var cardItem = _allItems.FirstOrDefault(i => i.Name == itemName);
            if (cardItem != null)
                ItemsListBox.SelectedItems.Remove(cardItem);

            CartChanged?.Invoke();
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            _selectedNames.Clear();
            CartListBox.ItemsSource = null;
            CartListBox.ItemsSource = _selectedNames;
            ItemsListBox.SelectedItems.Clear();
            RestoreSelection();
            CartChanged?.Invoke();
        }

        private void RestoreSelection()
        {
            ItemsListBox.SelectedItems.Clear();
            foreach (var item in _filteredItems)
            {
                if (_selectedNames.Contains(item.Name))
                    ItemsListBox.SelectedItems.Add(item);
            }
        }

        private void Card_Click(object sender, MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            Item item = (Item)border.Tag;

            if (_selectedNames.Contains(item.Name))
                _selectedNames.Remove(item.Name);
            else
                _selectedNames.Add(item.Name);

            CartListBox.ItemsSource = null;
            CartListBox.ItemsSource = _selectedNames;
            RestoreSelection();
            CartChanged?.Invoke();
        }

        private void CategoryButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_currentPopup != null && _currentPopup.IsOpen)
            {
                var popup = _currentPopup;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (popup.IsOpen && !popup.IsMouseOver)
                        popup.IsOpen = false;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}