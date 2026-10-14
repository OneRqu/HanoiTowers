using System.Windows;

namespace HanoiTowers
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Больше никаких «молчаливых» падений: любая ошибка покажет окно с текстом.
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show("Ошибка: " + args.Exception.Message +
                                "\n\nПодробности:\n" + args.Exception,
                    "Непредвиденная ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}