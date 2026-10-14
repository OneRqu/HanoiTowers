using System.Windows;
using HanoiTowers.Models;

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
            _game = new GameWindow((int)sliderDisks.Value);
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
    }
}