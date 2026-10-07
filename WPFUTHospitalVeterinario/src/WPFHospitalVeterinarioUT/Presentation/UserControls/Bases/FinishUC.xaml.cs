using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ControlzEx.Standard;
using Domain;
using Domain.Peripherals;
using Domain.UIServices;
using Domain.Peripherals.Recorder;
using Presentation.UserControls.Flows;
using UI.Bases;
using UI.Modals;
namespace Presentation.UserControls.Bases
{
    /// <summary>
    /// Interaction logic for FinishUC.xaml
    /// </summary>
    public partial class FinishUC : AppUserControl
    {
        private const string STR_TIMER = "01:30";

        private ObservableCollection<CalificacionData> _listCalificacion = new();
        private FinishViewModel _viewModel = new();
        private static string[] Numbers = { "1", "2", "3", "4", "5" };
        private static string[] Labels = { "Muy\ninsatisfecho", "Insatisfecho", "Neutral", "Satisfecho", "Muy\nsatisfecho" };
        private DocumentFormat _document = new();

        private Transaction _ts;

        public FinishUC()
        {
            InitializeComponent();
            this.DataContext = _viewModel;
            _viewModel.SendCalification = false;
            _ts = Transaction.Instance;
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {

            await StopVideoRecording();

            nameUser.Text = "Feliz día " + CapitaliceWord(FindShortWord(_ts.customFlows.generaLInformationClient.FirstName));
            for (int i = 0; i < 5; i++)
            {
                CalificacionData calificacionType = new CalificacionData
                {
                    Id = i + 1,
                    Label = Labels[i],
                    Number = Numbers[i],
                    IsSelected = false,
                };
                _listCalificacion.Add(calificacionType);
            }
            DataView.ItemsSource = _listCalificacion;
            DisableView();

            // Solo imprimir si la transacción NO fue cancelada
            bool esCancelada = _ts.transactionProcess.EstadoTransaccion == Domain.Enumerables.StateTransaction.Cancelada
                            || _ts.transactionProcess.EstadoTransaccion == Domain.Enumerables.StateTransaction.CanceladaErrorDevuelta
                            || _ts.transactionProcess.EstadoTransaccion == Domain.Enumerables.StateTransaction.ErrorServicioTercero;

            if (!esCancelada)
            {
                await CompletePrintProcess();
                bool continuar = _nav.ShowModal(
                    $"¿Desea una segunda impresión?", new ConfirmationModal());
                if(continuar) await CompletePrintProcess();
            }

            EnableView();
            StartTimer();
        }

        private async Task CompletePrintProcess()
        {
            try
            {
                // CORRECCION: Manejo de excepciones para debug sin impresora
                PrintService.CleanPrintQueue();
                PrintVoucher();
                var currentModal = _nav.ShowLoadModal("Imprimiendo factura...");
                await Task.Delay(TimeSpan.FromSeconds(PrintService.numberOfSecondsToPrint));
                while (!(PrintService.recentImpressionSuccess ?? false))
                {
                    currentModal.Close();
                    if (!HandlePrintingError()) break;
                    currentModal = _nav.ShowLoadModal("Imprimiendo factura...");
                await Task.Delay(TimeSpan.FromSeconds(PrintService.numberOfSecondsToPrint));
            }
            currentModal.Close();
            }
            catch (Exception ex)
            {
                // CORRECCION: En debug sin impresora, no bloquear el flujo
                EventLogger.SaveLog(EventType.Warning, $"Error en proceso de impresion (probablemente debug sin impresora): {ex.Message}");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            PrintService.recentImpressionSuccess = false;
        }

        #region UI control methods
        private async void BtnSalir_MouseDown(object sender, EventArgs e)
        {
            await FinishTransaction();
        }

        private void BtnAtras_MouseDown(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => GoTo(new PaymentUC()));
        }

        private void BtnCalificacion_TouchDown(object sender, MouseEventArgs e)
        {
            try
            {
                _viewModel.SendCalification = true;
                if (!(sender is FrameworkElement element)) return;

                var fila = element.DataContext as CalificacionData;
                if (fila == null) return;
                foreach (var filaCalificacion in _listCalificacion)
                {
                    filaCalificacion.IsSelected = filaCalificacion.Id <= fila.Id;
                }

                _ts.SaveRating(fila.Id);
            }
            catch (Exception ex)
            {

            }
        }

        private async Task FinishTransaction()
        {
            StopTimer();

            if (string.IsNullOrEmpty(_ts.paymentProcess.Calificacion))
            {
                _ts.paymentProcess.Calificacion = "Sin calificación";
            }

            if (!_ts.paymentProcess.DevueltaCorrecta)
            {
                var loadModal = _nav.ShowLoadModal(
                    "No se pudo entregar la totalidad del dinero hay un faltante de:" +
                    $" ${_ts.paymentProcess.ValorFaltante.ToString("N0")} " +
                    ". Por favor comunícate con un administrador.");
                await Task.Delay(TimeSpan.FromSeconds(20));
                if (loadModal != null)
                {
                    loadModal.Close();
                    loadModal = null;
                }
            }

            Dispatcher.Invoke(() => GoTo(new PublicityUC()));
        }
        #endregion

        #region Internal Operation process
        private void PrintVoucher()
        {
            try
            {
                var header = new Dictionary<string, string?>
                {
                    //{"NIT:","890920814-5"},
                    {"Recaudo",_ts.transactionProcess.TipoRecaudo},
                    {"Fecha", DateTime.Now.ToString("yyyy/MM/dd")},
                    {"Hora", DateTime.Now.ToString("HH:mm:ss")},
                };


                var body = new Dictionary<string, string?>
                {
                    {"Nombre", _ts.customFlows.generaLInformationClient.FirstName},
                    {"Apellidos",_ts.customFlows.generaLInformationClient.LastName},
                    {"Nro. Documento",_ts.customFlows.generaLInformationClient.Document},
                    {"Nro. Transacción",_ts.transactionProcess.ApiDto.Id.ToString()},
                    {"Estado", _ts.transactionProcess.EstadoTransaccionVerb },
                    {"Referencia", _ts.paymentProcess.Referencia },
                    {"Pago sin redondear", "$" + _ts.paymentProcess.TotalSinRedondear.ToString("N0") },
                    {"Pago redondeado", "$" + _ts.paymentProcess.Total.ToString("N0") },
                    {"Valor Ingresado", "$" + _ts.paymentProcess.TotalIngresado.ToString("N0") },
                    {"Valor Devuelto", "$" + _ts.paymentProcess.TotalDevuelta.ToString("N0")}
                };


                var footer = new Dictionary<string, string?>
                {
                    {"Dirección","Calle 20 Sur n° 23 a - 160 Barrio miramar"},
                    {"Línea de Atención", "(+57) 3204240394"},

                };
                _document.header = header;
                _document.body = body;
                _document.footer = footer;
                PrintService.BuildPrint(header, body, footer);
                PrintService.Start();

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }

        private bool HandlePrintingError()
        {
            bool result = false;

            Application.Current.Dispatcher.Invoke(delegate
            {
                var _currentModal = new BillReportWindow(_document.header, _document.body, _document.footer);
                _currentModal.ShowDialog();
                if (_currentModal.DialogResult.HasValue)
                {
                    result = _currentModal.DialogResult.Value;
                    if (result) PrintVoucher();
                }

            });
            return result;
        }
        #endregion

        #region Timer
        protected override string TimerDuration => STR_TIMER;

        protected override void OnTimerTimeout() => GoTo(new PublicityUC());
        #endregion

        private Task StopVideoRecording()
        {
            return RecordingService.Instance.StopAsync();
        }
    }

    public class FinishViewModel : INotifyPropertyChanged
    {

        private bool _sendCalification;
        
        public bool SendCalification
        {
            get { return _sendCalification; }
            set
            {
                if (_sendCalification != value)
                {
                    _sendCalification = value;
                    OnPropertyRaised(nameof(SendCalification));
                }
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyRaised(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class CalificacionData : INotifyPropertyChanged
    {
        private string _label;
        private int _id;
        private bool _isSelected;
        private string _number;

        public string Label
        {
            get { return _label; }
            set
            {
                if (_label != value)
                {
                    _label = value;
                    OnPropertyRaised(nameof(Label));
                }
            }
        }

        public int Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyRaised(nameof(Id));
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyRaised(nameof(IsSelected));
                }
            }
        }

        public string Number
        {
            get { return _number; }
            set
            {
                if (_number != value)
                {
                    _number = value;
                    OnPropertyRaised(nameof(Number));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyRaised(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }









    public class DocumentFormat
    {
        public Dictionary<string, string> header;
        public Dictionary<string, string> body;
        public Dictionary<string, string> footer;
    }
}
