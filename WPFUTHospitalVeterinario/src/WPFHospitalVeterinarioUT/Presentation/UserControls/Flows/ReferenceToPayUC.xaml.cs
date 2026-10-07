using Domain;
using Domain.Enumerables;
using Domain.Peripherals.Recorder;
using Domain.UIServices;
using Domain.Variables;
using Presentation.UserControls.Bases;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Bases;
using UI.Modals;
using WPFHospitalVeterinarioUT.ApiService;

namespace Presentation.UserControls.Flows
{
    /// <summary>
    /// Lógica de interacción para ReferenceToPayUC.xaml
    /// </summary>
    public partial class ReferenceToPayUC : AppUserControl
    {
        private const string STR_TIMER = "02:30";
        private Transaction _ts;
        private TimerGeneric? _timer;
        public ReferenceToPayUC()
        {
            InitializeComponent();
            _ts = Transaction.Instance;
            Keyboard.KeyboardPressed += OnKeyboardPressed;

            nameUser.Text = "Hola, " + CapitaliceWord(FindShortWord(_ts.customFlows.generaLInformationClient.FirstName));
            this.Unloaded += OnUnloaded;
            InputInvoice.Text = FormatMoney("0");
            GoTimer();


        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {


            Keyboard.KeyboardPressed -= OnKeyboardPressed;
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


        private static string FormatMoney(string? value)
        {
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var amount))
                amount = 0;

            return "$" + amount.ToString("N0", CultureInfo.GetCultureInfo("es-CO"));
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
                _ts.videoRecorder ??= new VideoRecorder(_ts);
                var started = await _ts.videoRecorder.StartAsync();
                EventLogger.SaveLog(
                    started ? EventType.Info : EventType.Warning,
                    started
                        ? "Grabación iniciada antes de crear la transacción."
                        : "No se pudo iniciar la grabación; la transacción continuará sin video.");
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "Error inicializando la grabación de video.", ex);
            }
        }

        public void GoTimer()
        {
            try
            {
                _timer?.Dispose();
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
                _timer?.Dispose();
                _timer = null;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }
        #endregion

    }
}
