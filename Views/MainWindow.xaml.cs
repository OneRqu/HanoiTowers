using HanoiTowers.Models;
using System.Windows;
using System.Windows.Input;

namespace HanoiTowers.Views
{
    public partial class MainWindow : Window
    {
        private GameWindow _game;   // текущая партия (одна на приложение)

        public MainWindow()
        {
            InitializeComponent();
            sliderDisks.ValueChanged += (s, e) => UpdateInfo();
            UpdateInfo();
        }

        private void UpdateInfo()
        {
            int n = (int)sliderDisks.Value;
            tbDiskCount.Text = n.ToString();
            tbMinMoves.Text = GameModel.MinMoves(n).ToString();
            tbMinFor.Text = string.Format("для {0} колец", n);
        }

        private void BtnNewGame_Click(object sender, RoutedEventArgs e)
        {
            if (_game != null) { _game.CloseWithoutMenu(); _game = null; }

            _game = new GameWindow((int)sliderDisks.Value)
            {
                // переносим геометрию меню на окно игры
                WindowState = this.WindowState,
                WindowStartupLocation = WindowState == WindowState.Normal
                    ? WindowStartupLocation.Manual : WindowStartupLocation.CenterScreen
            };

            if (WindowState == WindowState.Normal)
            {
                _game.Width = this.ActualWidth;    // +- те же размеры
                _game.Height = this.ActualHeight;
                _game.Left = this.Left + (this.ActualWidth - _game.Width) / 2;
                _game.Top = this.Top + (this.ActualHeight - _game.Height) / 2;
                // MinWidth/MinHeight из XAML не дадут окну стать меньше минимума — это нормально
            }

            _game.Show();
            Hide();
            btnContinue.IsEnabled = true;
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            if (_game == null) return;
            _game.Show();
            Hide();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { ToggleMaximize(); return; }
            Helpers.WindowDrag.Drag(this);
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e) { ToggleMaximize(); }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            bool max = WindowState == WindowState.Maximized;
            RootGrid.Margin = max ? new Thickness(8) : new Thickness(0);
            WindowBorder.CornerRadius = max ? new CornerRadius(0) : new CornerRadius(16);
            HeaderBorder.CornerRadius = max ? new CornerRadius(0) : new CornerRadius(16, 16, 0, 0);
            btnMaximize.Content = max ? "\uE923" : "\uE922";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}