using Domain;
using Domain.UIServices;
using Presentation.UserControls.Flows;
using UI.Bases;

namespace Presentation.UserControls.Bases
{
    public partial class WelcomeUC : AppUserControl
    {
        public WelcomeUC()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            await StopActiveRecording();
            Transaction.Reset();
        }

        private static async Task StopActiveRecording()
        {
            var transaction = Transaction.Instance;
            var recorder = transaction.videoRecorder;
            if (recorder == null)
                return;

            try
            {
                var stopped = await recorder.StopAsync();
                if (!stopped)
                {
                    await Task.Delay(500);
                    stopped = await recorder.StopAsync();
                }

                if (!stopped)
                {
                    EventLogger.SaveLog(EventType.Error,
                        "No fue posible detener la grabación al regresar a la bienvenida.");
                    return;
                }

                recorder.Dispose();
                if (ReferenceEquals(transaction.videoRecorder, recorder))
                    transaction.videoRecorder = null;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error,
                    "Error deteniendo la grabación al regresar a la bienvenida.", ex);
            }
        }

        private void Continuar_Touch(object sender, EventArgs e)
        {
            GoTo(new FormUC());
        }
    }
}
