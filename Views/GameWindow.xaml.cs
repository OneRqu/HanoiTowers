using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using HanoiTowers.Models;

namespace HanoiTowers.Views
{
    public partial class GameWindow : Window
    {
        private readonly GameModel _model = new GameModel(3);

        private const double SlowestMs = 900;   // уровень Minimum (0)
        private const double FastestMs = 40;    // уровень Maximum (7)

        private Rectangle _baseShape;
        private Rectangle[] _pegShapes;
        private TextBlock[] _pegLabels;
        private readonly Dictionary<int, Rectangle> _rings = new Dictionary<int, Rectangle>();

        private double _ringHeight = 24;

        private bool _finished;                 // игра завершена
        private bool _solving;                  // идёт авторешение
        private CancellationTokenSource _cts;
        private bool _returnToMenuOnClose = true;

        private Rectangle _dragged;
        private int _dragFromPeg;
        private Point _dragOffset;

        // Цвета поля под тёмную тему
        private static readonly Brush BaseBrush = HexBrush("#2C3852");
        private static readonly Brush PegBrush = HexBrush("#485678");
        private static readonly Brush PegLabelBrush = HexBrush("#93A0B8");
        private static readonly Brush RingStrokeBrush = HexBrush("#141B2C");

        private static Brush HexBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        public GameWindow(int diskCount)
        {
            InitializeComponent();
            sliderSpeed.ValueChanged += (s, e) => tbSpeedValue.Text = ((int)sliderSpeed.Value).ToString();
            tbSpeedValue.Text = ((int)sliderSpeed.Value).ToString();
            _model.Restart(diskCount);
            BuildBoard();
            board.Loaded += (s, e) => LayoutAll();   // раскладка, когда размеры известны
            UpdateStatus();
        }

        public void CloseWithoutMenu()
        {
            StopSolving();
            _returnToMenuOnClose = false;
            Close();
        }

        // ================= построение поля =================

        private void BuildBoard()
        {
            board.Children.Clear();
            _rings.Clear();

            _baseShape = new Rectangle { Fill = BaseBrush, RadiusX = 4, RadiusY = 4 };
            board.Children.Add(_baseShape);

            _pegShapes = new Rectangle[3];
            _pegLabels = new TextBlock[3];
            string[] names = { "A", "B", "C" };
            for (int i = 0; i < 3; i++)
            {
                _pegShapes[i] = new Rectangle { Width = 12, Fill = PegBrush, RadiusX = 5, RadiusY = 5 };
                board.Children.Add(_pegShapes[i]);

                _pegLabels[i] = new TextBlock
                {
                    Text = names[i],
                    FontSize = 17,
                    FontWeight = FontWeights.Bold,
                    Foreground = PegLabelBrush
                };
                board.Children.Add(_pegLabels[i]);
            }

            for (int d = 1; d <= _model.DiskCount; d++)
            {
                var r = new Rectangle
                {
                    Height = _ringHeight,
                    RadiusX = 5,
                    RadiusY = 5,
                    Fill = RingBrush(d),
                    Stroke = RingStrokeBrush,
                    StrokeThickness = 1,
                    Cursor = Cursors.Hand,
                    Tag = d
                };
                r.MouseLeftButtonDown += Ring_MouseLeftButtonDown;
                r.MouseMove += Ring_MouseMove;
                r.MouseLeftButtonUp += Ring_MouseLeftButtonUp;
                _rings[d] = r;
                board.Children.Add(r);
            }
        }

        private Brush RingBrush(int disk)
        {
            double t = _model.DiskCount <= 1 ? 0.5 : (disk - 1) / (double)(_model.DiskCount - 1);
            return new SolidColorBrush(HsvColor(t * 280, 0.65, 0.92));
        }

        private static Color HsvColor(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
        }

        // ================= геометрия =================

        private double PegCenterX(int peg) { return board.ActualWidth * (2 * peg + 1) / 6.0; }

        private double BaseTopY { get { return board.ActualHeight - 90; } }

        private double RingWidth(int disk)
        {
            // самый широкий диск должен лежать НА платформе с отступом от её края
            double maxW = board.ActualWidth / 3.0 - 56;
            if (maxW < 60) maxW = 60;
            double minW = Math.Min(46, maxW * 0.5);
            if (_model.DiskCount == 1) return Math.Min(maxW, 90);
            double t = (disk - 1) / (double)(_model.DiskCount - 1);
            return minW + t * (maxW - minW);
        }

        private void UpdateSizes()
        {
            if (_baseShape == null || board.ActualWidth < 20 || board.ActualHeight < 20) return;

            int n = Math.Max(_model.DiskCount, 1);

            // высота кольца подбирается так, чтобы башня помещалась при любом n
            double availH = board.ActualHeight - 210;
            if (availH < n * 8) availH = n * 8;
            _ringHeight = Math.Min(34, availH / n);
            if (_ringHeight < 8) _ringHeight = 8;

            double baseY = BaseTopY;
            _baseShape.Width = Math.Max(60, board.ActualWidth - 32);
            _baseShape.Height = 12;
            Canvas.SetLeft(_baseShape, 16);
            Canvas.SetTop(_baseShape, baseY);

            // стержень утоплен в платформу на полтолщины — выглядит «посаженным»
            double pegBottom = baseY + 6;
            double pegHeight = _ringHeight * n + 30;
            for (int i = 0; i < 3; i++)
            {
                _pegShapes[i].Height = pegHeight;
                Canvas.SetLeft(_pegShapes[i], PegCenterX(i) - 6);
                Canvas.SetTop(_pegShapes[i], pegBottom - pegHeight);

                // буквы: точное центрирование по стержню, ПОД платформой
                _pegLabels[i].Width = 40;
                _pegLabels[i].TextAlignment = TextAlignment.Center;
                Canvas.SetLeft(_pegLabels[i], PegCenterX(i) - 20);
                Canvas.SetTop(_pegLabels[i], baseY + 14);
            }

            foreach (var kv in _rings) kv.Value.Height = _ringHeight;
        }

        private void LayoutAll()
        {
            if (_baseShape == null) return;
            if (board.ActualWidth < 20 || board.ActualHeight < 20) return;
            UpdateSizes();
            for (int p = 0; p < 3; p++)
                for (int level = 0; level < _model.Pegs[p].Count; level++)
                    PlaceRing(_rings[_model.Pegs[p][level]], p, level);
        }

        private void PlaceRing(Rectangle ring, int peg, int level)
        {
            int disk = (int)ring.Tag;
            double w = RingWidth(disk);
            ring.Width = w;
            Canvas.SetLeft(ring, PegCenterX(peg) - w / 2);
            Canvas.SetTop(ring, BaseTopY - (level + 1) * _ringHeight);
        }

        private void Board_SizeChanged(object sender, SizeChangedEventArgs e) { LayoutAll(); }

        // ================= статус и блокировки =================

        private void UpdateStatus()
        {
            tbMoves.Text = string.Format("Ходы: {0}", _model.MoveCount);
            tbMin.Text = string.Format("минимум: {0}", GameModel.MinMoves(_model.DiskCount));
            btnUndo.IsEnabled = !_finished && !_solving && _model.History.Count > 0;
            btnSolve.IsEnabled = !_finished && !_solving;
            sliderSpeed.IsEnabled = !_solving;   // скорость меняется только вне авторешения
            rbRecursive.IsEnabled = !_solving;
            rbStack.IsEnabled = !_solving;
        }

        private void CheckFinished()
        {
            if (_finished || !_model.IsSolved) return;
            _finished = true;
            UpdateStatus();

            long min = GameModel.MinMoves(_model.DiskCount);
            string verdict = _model.MoveCount == min
                ? "Идеально! Это минимально возможное число ходов."
                : string.Format("Минимально возможное число ходов: {0}.", min);
            MessageBox.Show(this,
                string.Format("Башня перенесена на стержень C!\nСделано ходов: {0}.\n{1}",
                    _model.MoveCount, verdict),
                "Игра окончена", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ================= ручное перетаскивание =================

        private void Ring_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_solving || _finished) return;

            var ring = (Rectangle)sender;
            int disk = (int)ring.Tag;
            int peg = _model.PegOf(disk);

            if (_model.Pegs[peg][_model.Pegs[peg].Count - 1] != disk) return;

            _dragged = ring;
            _dragFromPeg = peg;
            Point p = e.GetPosition(board);
            _dragOffset = new Point(p.X - Canvas.GetLeft(ring), p.Y - Canvas.GetTop(ring));
            ring.CaptureMouse();
            Panel.SetZIndex(ring, 50);
            e.Handled = true;
        }

        private void Ring_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragged == null) return;
            Point p = e.GetPosition(board);
            Canvas.SetLeft(_dragged, p.X - _dragOffset.X);
            Canvas.SetTop(_dragged, p.Y - _dragOffset.Y);
        }

        private void Ring_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragged == null) return;
            Rectangle ring = _dragged;
            _dragged = null;
            ring.ReleaseMouseCapture();
            Panel.SetZIndex(ring, 0);

            int to = HitPeg(e.GetPosition(board));
            bool moved = to >= 0 && _model.TryMove(_dragFromPeg, to);

            LayoutAll();
            UpdateStatus();
            if (moved) CheckFinished();
        }

        private int HitPeg(Point p)
        {
            double w = board.ActualWidth;
            if (p.X < 0 || p.X > w || p.Y < 0 || p.Y > board.ActualHeight) return -1;
            int peg = (int)(p.X / (w / 3.0));
            if (peg < 0) peg = 0;
            if (peg > 2) peg = 2;
            return peg;
        }

        // ================= кнопки =================

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_solving || _finished) return;
            if (_model.Undo()) { LayoutAll(); UpdateStatus(); }
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            StopSolving();
            _finished = false;
            _model.Restart(_model.DiskCount);
            LayoutAll();
            UpdateStatus();
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e) { ReturnToMenu(); }

        private void ReturnToMenu()
        {
            StopSolving();
            Hide();
            var menu = Application.Current.MainWindow as MainWindow;
            if (menu != null)
            {
                menu.WindowState = this.WindowState;   // fullscreen игры → fullscreen меню
                if (WindowState == WindowState.Normal)
                {
                    menu.Width = this.ActualWidth;
                    menu.Height = this.ActualHeight;
                    menu.Left = this.Left + (this.ActualWidth - menu.Width) / 2;
                    menu.Top = this.Top + (this.ActualHeight - menu.Height) / 2;
                }
                menu.Show();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_returnToMenuOnClose)   // крестик = вернуться в меню
            {
                e.Cancel = true;
                ReturnToMenu();
            }
            base.OnClosing(e);
        }

        // ================= авторешение =================

        private async void BtnSolve_Click(object sender, RoutedEventArgs e)
        {
            if (_solving || _finished) return;

            List<Move> plan = rbStack.IsChecked == true
                ? _model.BuildSolutionWithStack()
                : _model.BuildSolution();
            if (plan.Count == 0) { CheckFinished(); return; }

            _solving = true;
            _cts = new CancellationTokenSource();
            btnStop.Visibility = Visibility.Visible;
            btnRestart.IsEnabled = false;
            UpdateStatus();

            try { await PlaySolutionAsync(plan, _cts.Token); }
            finally
            {
                _solving = false;
                btnStop.Visibility = Visibility.Collapsed;
                btnRestart.IsEnabled = true;
                LayoutAll();
                UpdateStatus();
                CheckFinished();
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e) { StopSolving(); }

        private void StopSolving()
        {
            if (_cts != null) _cts.Cancel();
        }

        private double SpeedToMs()
        {
            double min = sliderSpeed.Minimum;
            double max = sliderSpeed.Maximum;
            double t = (sliderSpeed.Value - min) / (max - min);       // 0..1 при любых Min/Max
            return SlowestMs * Math.Pow(FastestMs / SlowestMs, t);    // плавная шкала
        }

        private async Task PlaySolutionAsync(List<Move> plan, CancellationToken ct)
        {
            double perMove = SpeedToMs();
            
            foreach (Move m in plan)
            {
                if (ct.IsCancellationRequested || _model.IsSolved) break;
                try { await AnimateMoveAsync(m.From, m.To, perMove, ct); }
                catch (OperationCanceledException) { break; }

                _model.TryMove(m.From, m.To);
                LayoutAll();
                UpdateStatus();
            }
        }

        private async Task AnimateMoveAsync(int from, int to, double ms, CancellationToken ct)
        {
            int disk = _model.Pegs[from][_model.Pegs[from].Count - 1];
            Rectangle ring = _rings[disk];
            double w = RingWidth(disk);

            double x1 = PegCenterX(from) - w / 2;
            double x2 = PegCenterX(to) - w / 2;
            double yStart = BaseTopY - _model.Pegs[from].Count * _ringHeight;
            double yEnd = BaseTopY - (_model.Pegs[to].Count + 1) * _ringHeight;
            double yLift = Math.Min(yStart, yEnd) - _ringHeight * 1.2;
            if (yLift < 10) yLift = 10;
            double phase = Math.Max(ms / 3.0, 12);

            Panel.SetZIndex(ring, 100);
            try
            {
                await AnimateAsync(ring, Canvas.TopProperty, v => Canvas.SetTop(ring, v), yStart, yLift, phase);
                ct.ThrowIfCancellationRequested();
                await AnimateAsync(ring, Canvas.LeftProperty, v => Canvas.SetLeft(ring, v), x1, x2, phase);
                ct.ThrowIfCancellationRequested();
                await AnimateAsync(ring, Canvas.TopProperty, v => Canvas.SetTop(ring, v), yLift, yEnd, phase);
            }
            finally
            {
                ring.BeginAnimation(Canvas.LeftProperty, null);
                ring.BeginAnimation(Canvas.TopProperty, null);
                Canvas.SetLeft(ring, x2);
                Canvas.SetTop(ring, yEnd);
                Panel.SetZIndex(ring, 0);
            }
        }

        private static Task AnimateAsync(UIElement el, DependencyProperty prop,
            Action<double> setBase, double from, double to, double ms)
        {
            if (!el.IsVisible) { setBase(to); return Task.FromResult<object>(null); }

            var tcs = new TaskCompletionSource<object>();
            var anim = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms))
            {
                FillBehavior = FillBehavior.Stop
            };
            anim.Completed += (s, args) =>
            {
                el.BeginAnimation(prop, null);
                setBase(to);
                tcs.TrySetResult(null);
            };
            el.BeginAnimation(prop, anim);
            return tcs.Task;
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
            Close();   // сработает OnClosing и вернёт в меню
        }
    }
}