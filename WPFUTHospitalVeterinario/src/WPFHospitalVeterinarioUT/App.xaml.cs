using Domain;
using System.Windows;
using System.Windows.Controls;
using VirtualKeyboard.Wpf;
using System.Diagnostics;

namespace WPFHospitalVeterinarioUT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.Exit += AppExit;
            this.DispatcherUnhandledException += OnUnhandledException;
            VKeyboard.Listen<TextBox>(e => e.Text);
            Application.Current.Resources["PrimaryHueMidBrush"] = Application.Current.Resources["PRIMARYCOLOR"];
            var openedApplications = Process.GetProcessesByName("WPFHospitalVeterinarioUT");
            var actualApplication = Process.GetCurrentProcess();
            foreach (var item in openedApplications)
            {
                if (actualApplication.Id != item.Id) item.Kill();
            }
        }
        private void AppExit(object? sender, ExitEventArgs e)
        {
            EventLogger.SaveLog(EventType.Info, $"La aplicación se ha cerrado manualmente con codigo: {e.ApplicationExitCode}");
        }
        private void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Muestra un mensaje de error
            EventLogger.SaveLog(EventType.FatalError, $"Ocurrió un error fatal en la aplicación, excepción no manejada: {e.Exception.Message}", e.Exception);
            MessageBox.Show("Ha ocurrido un error: " + e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = false;
        }
    }

}
