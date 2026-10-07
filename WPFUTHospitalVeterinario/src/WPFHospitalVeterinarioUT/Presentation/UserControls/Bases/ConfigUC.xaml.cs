using Domain.Variables;
using Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UI.Bases;
using UI.Modals;
using WPFHospitalVeterinarioUT.ApiService;
using Domain.UIServices;
using Domain.Peripherals;

namespace Presentation.UserControls.Bases
{
    /// <summary>
    /// Lógica de interacción para ConfigUC.xaml
    /// </summary>
    public partial class ConfigUC : AppUserControl
    {
        private ConfigViewModel _viewModel;
        public ConfigUC()
        {
            InitializeComponent();

            DataContext = new ConfigViewModel();
            _viewModel = (ConfigViewModel)DataContext;

            Transaction.Reset();
            Transaction.Instance.customFlows.generaLInformationClient.ResetValues();
            this.Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(200);
            await InitPayPad();
        }

        private async Task InitPayPad()
        {
            EventLogger.SaveLog(EventType.Info, "Inicializando Pay+");
            try
            {
                // TODO: Finalizar cualquier grabación

                _viewModel.StatusMsg = Messages.LOGIN_IN;
                if (!await ApiDashboard.Login())
                {
                    await Retry(Messages.NO_SERVICE + " No se logró iniciar sesión en los servicios de E-City.");
                    return;
                }
                await Task.Delay(1000);
                _viewModel.StatusMsg = Messages.VALIDATING_PAYPLUS;
                if (!await ApiDashboard.Validate())
                {
                    await Retry(Messages.NO_SERVICE + " No cuenta con suficiente dinero para operar.");
                    return;
                }
                await Task.Delay(1000);

#if NO_PERIPHERALS
#else
                // Validación de perifericos
                _viewModel.StatusMsg = Messages.VALIDATING_PERIPHERALS;
                var peripheralController = ArduinoController.Instance;
                if (!await peripheralController.SendStart())
                {
                    await Retry(Messages.NO_SERVICE + " " + Messages.PERIPHERALS_FAILED_VALIDATE);
                    return;
                }
                //var printerStatusMsg = PrintService.CheckPrintStatus();
                //if (printerStatusMsg != string.Empty)
                //{
                //    await Retry(Messages.NO_SERVICE + " " + printerStatusMsg);
                //    return;
                //}

                peripheralController.StartAcceptance(0);
                await Task.Delay(1000);
                await peripheralController.StopAceptance();
#endif
                _viewModel.StatusMsg = "Exitoso";

                Dispatcher.Invoke(() => GoTo(new WelcomeUC()));
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución {ex.Message}", ex);
                await Retry(Messages.NO_SERVICE + " Ocurrió un error inesperado" + " Presiona continuar para intentar de nuevo.");
            }
        }

        private async Task Retry(string msgModal)
        {

            _nav.ShowModal(msgModal, new InfoModal());
            await InitPayPad();
        }
    }
    public class ConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _statusMsg = string.Empty;
        public string StatusMsg
        {
            get
            {
                return _statusMsg;
            }
            set
            {
                if (_statusMsg != value)
                {
                    _statusMsg = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusMsg)));
                }

            }
        }

    }
}
