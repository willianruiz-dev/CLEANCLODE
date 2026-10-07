using Presentation.UserControls.Bases;
using System;
using System.Collections.Generic;
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
using Domain.UIServices;
using UI.Bases;
using Domain;
using System.ComponentModel;
using UI.Modals;
using VirtualKeyboard.Wpf;
using Domain.Enumerables;
using Domain.Variables;
using WPFHospitalVeterinarioUT.ApiService;
using Domain.Peripherals;
using Domain.Peripherals.Recorder;

namespace Presentation.UserControls.Flows
{
    /// <summary>
    /// Lógica de interacción para ReferenceToPayUC.xaml
    /// </summary>
    public partial class ReferenceToPayUC : AppUserControl
    {
        private const string STR_TIMER = "02:30";
        private Transaction _ts;
        private ManualInputViewModel _viewModel;
        private ModalWindow? _currentLoadModal = null;
        private Border? _borderSelected = null;
        private TimerGeneric _timer;
        public ReferenceToPayUC()
        {
            InitializeComponent();
            _ts = Transaction.Instance;
            _viewModel = new ManualInputViewModel();
            this.DataContext = _viewModel;
            Keyboard.KeyboardPressed += OnKeyboardPressed;

            _ts = Transaction.Instance;
            nameUser.Text = "Hola, " + CapitaliceWord(FindShortWord(_ts.customFlows.generaLInformationClient.FirstName));
            this.Unloaded += OnUnloaded;
            InputInvoice.Text = FormatMoney("0");
            GoTimer();


        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {


            StopTimer();
        }

        #region UI EVENTS
        private void BtnSalir_MouseDown(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => GoTo(new PublicityUC()));
        }

        private void BtnAtras_MouseDown(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => GoTo(new FormUC()));
        }

        private async void BtnReferenceToPay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(references.Text == null)
            {
                _nav.ShowModal("Por favor, ingrese la referencia de pago.", new InfoModal());
                return;
            }
            if (InputInvoice.Text == null)
            {
                _nav.ShowModal("Por favor, ingrese el valor a pagar.", new InfoModal());
                return ;
            }
            string textToPay = InputInvoice.Text.Replace("$", string.Empty).Replace(".", string.Empty);
            if (!decimal.TryParse(textToPay, out decimal valueToPay)) return;
            if (valueToPay <= 0)
            {
                _nav.ShowModal("Por favor, ingrese un valor mayor a cero.", new InfoModal());
                return;
            }
            DisableView();
            _ts.paymentProcess.Referencia = references.Text;
            _ts.transactionProcess.TipoPago = TypePayment.Efectivo;
            _ts.paymentProcess.TotalSinRedondear = valueToPay;
            _ts.paymentProcess.Total = Math.Ceiling(valueToPay / 100) * 100;
            await SendData();
        }
        private async void OnKeyboardPressed(object? sender, string keyPressed)
        {
            if (string.IsNullOrEmpty(keyPressed)) return;
            if (keyPressed == "Remove")
            {
                string textToErase = InputInvoice.Text.Replace("$", string.Empty).Replace(" ", string.Empty).Replace(".", string.Empty);
                textToErase = (textToErase.Length > 1) ? textToErase.Remove(textToErase.Length - 1) : "0";
                InputInvoice.Text = FormatMoney(textToErase);

                return;
            }

            if (keyPressed == "Clear")
            {
                InputInvoice.Text = FormatMoney("0");
                return;
            }

            string text = InputInvoice.Text.Replace("$", string.Empty).Replace(" ", string.Empty).Replace(".", string.Empty);
            text += keyPressed;
            InputInvoice.Text = FormatMoney(text);
            await Task.Delay(100);
        }


        private bool _isUpdatingText = false;

        private void TxtInvoice_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Evitar reentrada
            if (_isUpdatingText) return;
            
            string texto = InputInvoice.Text;
            
            // Si hay caracteres no numericos (XDR, letras, etc.), limpiar inmediatamente
            string soloDigitos = new string(texto.Where(char.IsDigit).ToArray());
            string formatoCorrecto = FormatMoney(soloDigitos);
            
            if (texto != formatoCorrecto)
            {
                _isUpdatingText = true;
                InputInvoice.Text = formatoCorrecto;
                _isUpdatingText = false;
            }
            
            if (InputInvoice.Text.Length > 11)
            {
                string text = InputInvoice.Text.Replace("$", string.Empty).Replace(".", string.Empty);
                _isUpdatingText = true;
                InputInvoice.Text = FormatMoney(text.Substring(0, text.Length - 1));
                _isUpdatingText = false;
                return;
            }
        }

        private async Task SendData()
        {
            ModalWindow? loadModal = null;
            try
            {
                loadModal = _nav.ShowLoadModal(Messages.VALIDATING_INFO);

                // Iniciar grabación ANTES de crear la transacción para capturar todo el proceso
                await InitializeVideoRecording();

                var tsCreated = await ApiDashboard.CreateTransaction();
                if (tsCreated == null) throw new Exception("No se pudo enviar la transacción");

#if NO_PERIPHERALS
#else
                // Cada camara es una source incremental
#endif

                if (loadModal != null)
                {
                    loadModal.Close();
                    loadModal = null;
                }

                if (_ts.transactionProcess.TipoPago == TypePayment.Efectivo)
                    Dispatcher.Invoke(() => GoTo(new PaymentUC()));


            }
            catch (Exception ex)
            {
                if (loadModal != null)
                {
                    loadModal.Close();
                    loadModal = null;

                    EnableView();
                }

                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                _nav.ShowModal("Ocurrió un error validando la información. Por favor intente nuevamente.", new InfoModal());
            }
        }

        private async Task InitializeVideoRecording()
        {
            try
            {
                // Inicializar el VideoRecorder si no existe
                if (_ts.videoRecorder == null)
                {
                    _ts.videoRecorder = new VideoRecorder(_ts);
                }

                // Iniciar grabación en hilo separado para no bloquear la UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        bool result = await _ts.videoRecorder.StartAsync();
                        if (result)
                        {
                            EventLogger.SaveLog(EventType.Info, "Grabación iniciada exitosamente desde ReferenceToPayUC");
                        }
                        else
                        {
                            EventLogger.SaveLog(EventType.Warning, "No se pudo iniciar la grabación desde ReferenceToPayUC");
                        }
                    }
                    catch (Exception ex)
                    {
                        EventLogger.SaveLog(EventType.Error, $"Error al iniciar grabación desde ReferenceToPayUC: {ex.Message}", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Error al inicializar VideoRecorder desde ReferenceToPayUC: {ex.Message}", ex);
            }
        }

        private string FormatMoney(string valor)
        {
            // Limpiar XDR y cualquier caracter no numerico
            string limpio = new string(valor.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(limpio)) limpio = "0";
            
            // Usar formato manual con "$" en lugar de {0:C0} que usa la moneda de Windows (XDR)
            decimal valorNumerico = decimal.Parse(limpio);
            return "$" + valorNumerico.ToString("N0");
        }
        #endregion
        #region Timer
        public void GoTimer()
        {
            try
            {
                _timer = new TimerGeneric(STR_TIMER);

                TxtTimer.Text = STR_TIMER;

                _timer.CallBackTimeOut = () =>
                {

                    Dispatcher.Invoke(() => GoTo(new PublicityUC()));


                };

                _timer.CallBackTick = stringTimer =>
                {
                    Dispatcher.BeginInvoke((Action)delegate
                    {
                        TxtTimer.Text = stringTimer;

                    });
                };

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }

        public void StopTimer()
        {
            try
            {
                if (_timer != null)
                {
                    _timer.CallBackTimeOut = null;
                    _timer.CallBackTick = null;
                    _timer.CallBackStop?.Invoke();
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }
        #endregion

    }
    public class ManualInputViewModel : INotifyPropertyChanged
    {
        private string _statusMsg = string.Empty;

        public string StatusMsg
        {
            get
            {
                return _statusMsg;
            }
            set
            {
                _statusMsg = value;
                OnPropertyRaised(nameof(StatusMsg));
            }
        }
        private string _title = string.Empty;
        public string HelpMessage
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                OnPropertyRaised(nameof(HelpMessage));
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyRaised(string propertyname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

        }
    }
}
