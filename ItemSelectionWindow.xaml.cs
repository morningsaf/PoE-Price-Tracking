using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Linq.Expressions;


namespace PoE_Price_Tracking
{
    public partial class ItemSelectionWindow : Window
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
        public ItemSelectionWindow(List<Item> allItems, List<string> selectedNames)
        {
            InitializeComponent();
            _allItems = allItems;
            _filteredItems = new List<Item>();
            _selectedNames = selectedNames;
            LoadCategories();
            CartListBox.ItemsSource = _selectedNames;
            ItemsListBox.ItemsSource = _filteredItems;
            SearchBox.Focus();
            foreach (string name in _selectedNames)
            {
                var item = _allItems.FirstOrDefault(i => i.Name == name);
                if (item != null)
                {
                    ItemsListBox.SelectedItems.Add(item);
                }
            };
        } 

        private void LoadCategories()
        {
            string[] categories = { "weapon", "armour", "jewellery", "jewel", "map", "flask" };
            string[] displayNames = { "Weapon", "Armour", "Jewellery", "Jewel", "Map", "Flask" };
            for (int i = 0; i < categories.Length; i++)
            {
                Button btn = new Button
                {
                    Content = displayNames[i],
                    Width = 80,
                    Height = 26,
                    Margin = new Thickness(2),
                    Tag = categories[i]
                };
                btn.MouseEnter += CategoryButton_MouseEnter;
                FilterPanel.Children.Add(btn);
            }
        }
        private Popup? _currentPopup;

        private void CategoryButton_MouseEnter(object sender,MouseEventArgs e)
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
                    Width = 100, Height = 26,
                    Margin = new Thickness(2)
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
            panel.MouseLeave += (s, args) =>
            {
                _currentPopup.IsOpen = false;
            };

            _currentPopup.Child = panel;
            _currentPopup.IsOpen = true;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = SearchBox.Text.ToLower();

            if(string.IsNullOrEmpty(filter))
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

        public List<Item> SelectedItems {get; private set;} = new();

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public List<string> GetSelectedNames()
        {
            return _selectedNames;
        }

        private void RemoveFromCart_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string itemName = (string)btn.Tag;

            _selectedNames.Remove(itemName);

            UpdateCartAndSelection();

            var cardItem = _allItems.FirstOrDefault(i => i.Name == itemName);
            if (cardItem != null)
                ItemsListBox.SelectedItems.Remove(cardItem);
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                {
                    _selectedNames.Clear();
                    UpdateCartAndSelection();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateCartAndSelection()
        {
            CartListBox.ItemsSource = null;
            CartListBox.ItemsSource = _selectedNames;
            ItemsListBox.SelectedItems.Clear();
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

        private void Card_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
        }
    }

}