using Domain;
using Domain.UIServices;
using Domain.Peripherals.Recorder;
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
            await RecordingService.Instance.StopAsync();
            Transaction.Reset();
        }

        private void Continuar_Touch(object sender, EventArgs e)
        {
            GoTo(new FormUC());
        }
    }
}
