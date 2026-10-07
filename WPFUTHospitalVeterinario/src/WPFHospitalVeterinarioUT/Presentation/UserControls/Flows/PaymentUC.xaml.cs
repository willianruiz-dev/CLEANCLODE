using Domain.Enumerables;
using Domain;
using Presentation.UserControls.Bases;
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
using WPFHospitalVeterinarioUT.ApiService;
using UI.Modals;
using Domain.Peripherals;
using Domain.UIServices;
using Domain.Variables;
using Domain.Peripherals.Recorder;
namespace Presentation.UserControls.Flows
{
    /// <summary>
    /// Interaction logic for PaymentUC.xaml
    /// </summary>
    public partial class PaymentUC : AppUserControl
    {
        private Transaction _ts;
        private ArduinoController _peripherals;
        private PaymentViewModel _paymentViewModel;
        private VideoRecorder _videoRecorder;
        private bool _isRecording = false;

        private bool _isPayCanceled = false;

        private ModalWindow? _currentLoadModal;

        private StateTransaction _tranStateTemp = StateTransaction.Iniciada;
        public PaymentUC()
        {
            InitializeComponent();

            _ts = Transaction.Instance;
            _ts.paymentProcess.DevueltaCorrecta = false;

            _videoRecorder = null;
            _isRecording = false;

#if NO_PERIPHERALS
            Button dynamicButton = new Button();

            // Set properties of the button
            dynamicButton.Content = "Add minor value";
            dynamicButton.Width = 100;
            dynamicButton.Height = 50;
            dynamicButton.VerticalAlignment = VerticalAlignment.Top;
            dynamicButton.HorizontalAlignment = HorizontalAlignment.Left;
            // Set background color
            dynamicButton.Background = new SolidColorBrush(Colors.Transparent); // Change to the desired color
            dynamicButton.Foreground = new SolidColorBrush(Colors.White); // Change to the desired color

            // Set border brush and thickness
            dynamicButton.BorderBrush = new SolidColorBrush(Colors.White); // Change to the desired color
            dynamicButton.BorderThickness = new Thickness(2); // Change thickness as needed
            dynamicButton.Click += ExecuteScanner;

            Button dynamicButton2 = new Button();

            // Set properties of the button
            dynamicButton2.Content = "Add mid value";
            dynamicButton2.Width = 100;
            dynamicButton2.Height = 50;
            dynamicButton2.VerticalAlignment = VerticalAlignment.Top;
            dynamicButton2.HorizontalAlignment = HorizontalAlignment.Center;
            // Set background color
            dynamicButton2.Background = new SolidColorBrush(Colors.Transparent); // Change to the desired color
            dynamicButton2.Foreground = new SolidColorBrush(Colors.White); // Change to the desired color

            // Set border brush and thickness
            dynamicButton2.BorderBrush = new SolidColorBrush(Colors.White); // Change to the desired color
            dynamicButton2.BorderThickness = new Thickness(2); // Change thickness as needed
            dynamicButton2.Click += ExecuteScanner2;

            void ExecuteScanner(object sender, EventArgs e)
            {
                OnCashIn(20000);
            }

            void ExecuteScanner2(object sender, EventArgs e)
            {
                OnCashIn(50000);
            }
            MainGrid.Children.Add(dynamicButton);
            MainGrid.Children.Add(dynamicButton2);
#else
            _peripherals = ArduinoController.Instance;
            _peripherals.CashIn += OnCashIn;
            _peripherals.CashDispensed += OnCashDispensed;
            _peripherals.DispenserReject += OnDispenserReject;
            _peripherals.PeripheralError += OnPeripheralError;
#endif
            this.Unloaded += OnUnloaded;
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitViewModel();
            SetupExistingVideoRecorder();
            
#if NO_PERIPHERALS
#else
            _peripherals.StartAcceptance(_paymentViewModel.PayAmount);
#endif
        }
        
        private void SetupExistingVideoRecorder()
        {
            try
            {
                // Solo obtener referencia al VideoRecorder existente desde ReferenceToPayUC
                if (_ts.videoRecorder != null)
                {
                    _videoRecorder = _ts.videoRecorder;
                    _isRecording = _videoRecorder.IsRecording;
                    EventLogger.SaveLog(EventType.Info, $"Usando VideoRecorder existente. Estado grabación: {_isRecording}");
                }
                else
                {
                    EventLogger.SaveLog(EventType.Warning, "No se encontró VideoRecorder existente desde ReferenceToPayUC");
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Error al configurar VideoRecorder existente: {ex.Message}", ex);
            }
        }
        private void InitViewModel()
        {

            _paymentViewModel = new PaymentViewModel
            {
                PayAmount = _ts.paymentProcess.Total,
                RemainingAmount = _ts.paymentProcess.Total,
                ReturnAmount = 0,
                EnteredAmount = 0,
                Denominations = new List<Denomination>(),
                DispensedAmount = 0
            };
            DataView.DataContext = _paymentViewModel;
            this.DataContext = _paymentViewModel;
            DataView.ItemsSource = _paymentViewModel.Denominations;

        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
#if NO_PERIPHERALS
#else
            _peripherals.CashIn -= OnCashIn;
            _peripherals.CashDispensed -= OnCashDispensed;
            _peripherals.DispenserReject -= OnDispenserReject;
            _peripherals.PeripheralError -= OnPeripheralError;
#endif
        }


        #region Responses to Peripheral Events
        private async void OnCashIn(decimal value)
        {

            if (_paymentViewModel.IsPayCompleted) return;

            // ═══════════════════════════════════════════════════════════════
            // SIEMPRE registrar y contar el dinero recibido, porque el
            // hardware ya lo aceptó físicamente. Si no lo contamos, el
            // dinero queda atrapado en la máquina sin devolverse.
            // ═══════════════════════════════════════════════════════════════
            EventLogger.SaveLog(EventType.Info, $"Recibido dinero: {value}. Grabación de video en curso: {_isRecording}");
            _paymentViewModel.EnteredAmount += value;

            _paymentViewModel.RefreshAmountsList(Convert.ToInt32(value), 1);

            SendTransactionDetail(TypeOperation.AP, value, 1);

            _ = RefreshView(); // Se refresca la vista asincronamente

            // Si el pago fue cancelado, NO iniciar proceso de pago.
            // CancelPay se encargará de devolver el dinero usando EnteredAmount.
            if (_isPayCanceled)
            {
                EventLogger.SaveLog(EventType.Warning, $"Dinero recibido ({value}) durante cancelación. Se contabilizó pero no se procesará el pago. CancelPay devolverá el dinero.");
                return;
            }

            if (_paymentViewModel.EnteredAmount < _paymentViewModel.PayAmount) return;

            //Finaliza pago cantidad completa
            _ = Dispatcher.BeginInvoke(() => BtnCancel.Visibility = Visibility.Collapsed);
#if NO_PERIPHERALS
#else
            await _peripherals.StopAceptance();
#endif

            _currentLoadModal = _nav.ShowLoadModal("Estamos procesando el pago...");
            await Task.Delay(3000);
            EventLogger.SaveLog(EventType.Info, "Iniciando Proceso de pago...");

            await PaymentProcess();
        }

        private async void OnCashDispensed(decimal totalDispensed, Dictionary<int, int> details)
        {

            _paymentViewModel.DispensedAmount = totalDispensed;

            _paymentViewModel.RemainingAmount = _paymentViewModel.ReturnAmount - _paymentViewModel.DispensedAmount;
            string strValueToReturn = "$" + _paymentViewModel.RemainingAmount.ToString("N0");

            SendDispenseDetails(details);

            // ═══════════════════════════════════════════════════════════════════════
            // CORRECCION CRITICA: El evento CashDispensed se dispara cuando el
            // hardware REPORTA que terminó la dispensación, pero el dinero físico
            // todavía está saliendo de la máquina. Debemos mantener la pantalla
            // de devolución y la grabación de video activas hasta que el dinero
            // termine de salir completamente, para que el video capture toda la
            // devolución física.
            // ═══════════════════════════════════════════════════════════════════════
            CloseLoadModal();

            if (_paymentViewModel.DispensedAmount == _paymentViewModel.ReturnAmount)
            {
                _ts.paymentProcess.DevueltaCorrecta = true;

                // Mantener pantalla para que el usuario recoja su dinero
                // y el video capture la devolución física completa
                _currentLoadModal = _nav.ShowLoadModal("Por favor recoja su dinero...");
                EventLogger.SaveLog(EventType.Info, "Esperando a que el dinero termine de salir físicamente (video grabando)...");
                await Task.Delay(8000); // Espera para que el dispensador termine físicamente y el video lo capture
                CloseLoadModal();

                await SavePay();
            }
            else
            {
                // Incluso con faltante, esperar a que salga el dinero que sí se dispensó
                _currentLoadModal = _nav.ShowLoadModal("Por favor recoja su dinero...");
                EventLogger.SaveLog(EventType.Info, "Esperando a que el dinero termine de salir físicamente (con faltante, video grabando)...");
                await Task.Delay(8000); // Espera para dispensación física y captura de video
                CloseLoadModal();

                _currentLoadModal = _nav.ShowLoadModal("No se pudo entregar la totalidad del dinero hay un faltante de:" + $" {strValueToReturn} " + ".Por favor comunícate con un administrador.");
                await Task.Delay(5000); // Timer para mostrar la modal y que se pueda leer
                _ts.paymentProcess.DevueltaCorrecta = false;
                await SavePay();
            }

        }

        private void OnDispenserReject(Dictionary<int, int> rejectData)
        {
            // Se registra el reject en la api
            SendRejectDetails(rejectData);

        }

        private void OnPeripheralError(Exception ex)
        {
            //TODO: Evaluar Si es necesario reportar errores de perifericos al Dashboard por que ya los errores de perifericos se reportan internamente
        }
        #endregion

        #region UI control methods
        private async Task RefreshView()
        {
            await Dispatcher.BeginInvoke(() =>
            {
                DataView.Items.Refresh();
            });
        }
        private void CloseLoadModal()
        {
            if (_currentLoadModal != null)
            {
                Dispatcher.Invoke(() =>
                {
                    _currentLoadModal.Close();
                    _currentLoadModal = null;
                });
            }
        }
        private async void BtnCancel_TouchDown(object sender, MouseButtonEventArgs e)
        {
            _ = Dispatcher.BeginInvoke(() => BtnCancel.Visibility = Visibility.Collapsed);

            if (!_nav.ShowModal(Messages.CANCEL_TRANSACTION, new ConfirmationModal()))
            {
                _ = Dispatcher.BeginInvoke(() => BtnCancel.Visibility = Visibility.Visible);
                return;
            }
            EventLogger.SaveLog(EventType.Info, "Pago cancelado por el usuario.");
            await CancelPay();
        }

        #endregion

        #region Internal Operation process
        private async Task PaymentProcess()
        {
            if (_isPayCanceled)
            {
                EventLogger.SaveLog(EventType.Warning, "PaymentProcess bloqueado: pago ya fue cancelado.");
                return;
            }

            EventLogger.SaveLog(EventType.Info, "Pago completado; iniciando cierre de transacción.");
            _tranStateTemp = StateTransaction.Aprobada;
            await FinishSuccessfulPay();
        }

        private async Task FinishSuccessfulPay()
        {
            // Defensa: si se canceló durante el procesamiento, no continuar como exitoso
            if (_isPayCanceled)
            {
                EventLogger.SaveLog(EventType.Warning, "FinishSuccessfulPay bloqueado: pago ya fue cancelado.");
                return;
            }

            // Recalcular ReturnAmount para evitar valores contaminados por CancelPay
            decimal calculatedReturn = _paymentViewModel.EnteredAmount - _paymentViewModel.PayAmount;
            if (calculatedReturn < 0) calculatedReturn = 0;
            _paymentViewModel.ReturnAmount = calculatedReturn;

            if (_paymentViewModel.EnteredAmount > 0 && _paymentViewModel.ReturnAmount > 0)
            {

                CloseLoadModal();
                _currentLoadModal = _nav.ShowLoadModal("Pago completado con éxito devolución en curso...");
                await Task.Delay(3000);
                EventLogger.SaveLog(EventType.Info, $"Iniciando devuelta de {_paymentViewModel.ReturnAmount}");
                ReturnMoney(_paymentViewModel.ReturnAmount);
            }
            else
            {
                _ts.paymentProcess.DevueltaCorrecta = true;
                await SavePay();
            }
        }

        private async Task SavePay()
        {
            EventLogger.SaveLog(EventType.Info, "Iniciando proceso de guardado de pago...");

            // Defensa: si se devolvió todo el dinero, la transacción es Cancelada, no Aprobada
            if (_tranStateTemp == StateTransaction.Aprobada
                && _paymentViewModel.DispensedAmount >= _paymentViewModel.EnteredAmount
                && _paymentViewModel.EnteredAmount > 0)
            {
                EventLogger.SaveLog(EventType.Error,
                    $"ALERTA INCONSISTENCIA: DispensedAmount ({_paymentViewModel.DispensedAmount}) >= EnteredAmount ({_paymentViewModel.EnteredAmount}) con estado Aprobada. Se devolvió todo el dinero, cambiando a Cancelada.");
                _tranStateTemp = StateTransaction.Cancelada;
            }

            try
            {
                // Detenemos la grabación si está activa
                if (_isRecording && _videoRecorder != null)
                {
                    EventLogger.SaveLog(EventType.Info, "Intentando detener la grabación de video en SavePay...");
                    
                    try
                    {
                        bool stopResult = await _videoRecorder.StopAsync();
                        EventLogger.SaveLog(EventType.Info, $"Resultado de detener grabación en SavePay: {(stopResult ? "Exitoso" : "Fallido")}");
                        
                        if (!stopResult)
                        {
                            await Task.Delay(500);
                            stopResult = await _videoRecorder.StopAsync();
                            EventLogger.SaveLog(EventType.Info, $"Segundo intento de detener grabación en SavePay: {(stopResult ? "Exitoso" : "Fallido")}");
                        }
                        
                        _isRecording = false;
                    }
                    catch (Exception ex)
                    {
                        EventLogger.SaveLog(EventType.Error, $"Error al detener grabación en SavePay: {ex.Message}", ex);
                    }
                    finally
                    {
                        // Liberar recursos y limpiar referencia
                        try
                        {
                            if (_videoRecorder != null)
                            {
                                _videoRecorder.Dispose();
                                _videoRecorder = null;
                                _ts.videoRecorder = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            EventLogger.SaveLog(EventType.Error, $"Error al liberar recursos de grabación en SavePay: {ex.Message}", ex);
                        }
                    }
                }
                else
                {
                    EventLogger.SaveLog(EventType.Info, $"No hay grabación activa para detener en SavePay. _isRecording={_isRecording}, _videoRecorder={((_videoRecorder == null) ? "nulo" : "no nulo")}");
                }
                
                _paymentViewModel.IsPayCompleted = true;
                _ts.paymentProcess.TotalIngresado = _paymentViewModel.EnteredAmount;
                _ts.paymentProcess.TotalDevuelta = _paymentViewModel.DispensedAmount;

                SetTransactionDescription();

                if ((_tranStateTemp == StateTransaction.Aprobada || _tranStateTemp == StateTransaction.Cancelada)
                    && !_ts.paymentProcess.DevueltaCorrecta)
                {
                    // Si el estado de transacción es aprobada o cancelada y además hay error de devuelta se cambia a su respectivo estado
                    // CanceladoErrorDevuelta o AprobadaErrorDevuelta
                    _ts.transactionProcess.EstadoTransaccion = (StateTransaction)((int)_tranStateTemp + 2);
                    _ts.paymentProcess.ValorFaltante = _paymentViewModel.RemainingAmount;
                }
                else
                {
                    _ts.transactionProcess.EstadoTransaccion = _tranStateTemp;
                }

                ApiDashboard.UpdateTransaction();

          




                CloseLoadModal();
                Dispatcher.Invoke(() => GoTo(new FinishUC()));


            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                if (!_isPayCanceled)
                {
                    EventLogger.SaveLog(EventType.Info, "Pago cancelado por error guardando el pago");
                    await CancelPay();
                }

                CloseLoadModal();
                _currentLoadModal = _nav.ShowLoadModal("Ocurrió un error fatal intentando reportar los datos del pago. Por favor comuníquese con soporte técnico.");
            }
        }

        private void ReturnMoney(decimal returnValue)
        {
            _ts.paymentProcess.DevueltaCorrecta = false;
#if NO_PERIPHERALS
            OnCashDispensed(returnValue, new Dictionary<int, int>());
#else
            _peripherals.StartDispenser(returnValue);
#endif

        }

        private async Task CancelPay()
        {
            EventLogger.SaveLog(EventType.Info, "Iniciando proceso de cancelacion de pago...");

            _isPayCanceled = true;
            _tranStateTemp = StateTransaction.Cancelada;
#if NO_PERIPHERALS
#else
            _peripherals.CashIn -= OnCashIn;
#endif

            try
            {
#if NO_PERIPHERALS
#else
                await _peripherals.StopAceptance();
#endif

                // CORRECCION CRITICA: Devolver dinero ANTES de detener grabacion
                // para que el video capture la devolucion completa
                if (_paymentViewModel.EnteredAmount > 0)
                {
                    _paymentViewModel.ReturnAmount = _paymentViewModel.EnteredAmount;
                    _currentLoadModal = _nav.ShowLoadModal("Transaccion cancelada. Devolucion en curso...");
                    EventLogger.SaveLog(EventType.Info, "CancelPay: Devolviendo dinero con grabacion activa...");
                    ReturnMoney(_paymentViewModel.EnteredAmount);
                    // OnCashDispensed esperará a que termine la dispensación física,
                    // mantendrá el video grabando, y luego llamará a SavePay()
                    return;
                }
                else
                {
                    _ts.paymentProcess.DevueltaCorrecta = true;
                    await StopRecordingAndSave();
                }

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrio un error en CancelPay: {ex.Message}", ex);
            }
        }

        private async Task StopRecordingAndSave()
        {
            if (_isRecording && _videoRecorder != null)
            {
                EventLogger.SaveLog(EventType.Info, "Deteniendo grabacion de video...");
                try
                {
                    bool stopResult = false;
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        stopResult = await _videoRecorder.StopAsync();
                        EventLogger.SaveLog(EventType.Info, $"Intento {attempt}: {(stopResult ? "Exitoso" : "Fallido")}");
                        if (stopResult) break;
                        await Task.Delay(500);
                    }
                    _isRecording = false;
                }
                catch (Exception ex)
                {
                    EventLogger.SaveLog(EventType.Error, $"Error al detener grabacion: {ex.Message}", ex);
                }
                finally
                {
                    try
                    {
                        if (_videoRecorder != null)
                        {
                            _videoRecorder.Dispose();
                            _videoRecorder = null;
                            _ts.videoRecorder = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        EventLogger.SaveLog(EventType.Error, $"Error al liberar grabacion: {ex.Message}", ex);
                    }
                }
            }
            
            await SavePay();
        }
        #endregion

        #region HTTP API Consume
        private void SendDispenseDetails(Dictionary<int, int> details)
        {

            foreach (var denom in details.Keys)
            {
                var quantity = details[denom];
                if (quantity <= 0) continue;
                SendTransactionDetail(TypeOperation.DP, Convert.ToDecimal(denom), quantity);
            }
        }

        private void SendRejectDetails(Dictionary<int, int> details)
        {

            foreach (var denom in details.Keys)
            {
                var quantity = details[denom];
                if (quantity <= 0) continue;
                SendTransactionDetail(TypeOperation.Reject, Convert.ToDecimal(denom), quantity);

            }
        }

        private void SendTransactionDetail(TypeOperation op, decimal denom, int quantity)
        {
            try
            {
                ApiDashboard.CreateTransactionDetail(op, (int)denom, quantity);

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }

        private void SetTransactionDescription()
        {
            switch (_tranStateTemp)
            {
                case StateTransaction.Aprobada:
                    _ts.transactionProcess.EstadoTransaccionVerb = "Exitoso";
                    _ts.paymentProcess.Descripcion += "Transacción finalizada correctamente. ";
                    break;
                case StateTransaction.Cancelada:
                    _ts.transactionProcess.EstadoTransaccionVerb = "Declinada";
                    _ts.paymentProcess.Descripcion += "Transacción Cancelada, No se realizó el pago.";
                    break;
                case StateTransaction.AprobadaSinNotificar:
                    _ts.transactionProcess.EstadoTransaccionVerb = "Exitoso";
                    _ts.paymentProcess.Descripcion += "Transacción aprobada, pero no se ha podido notificar el pago a la entidad correspondiente. ";
                    break;
                case StateTransaction.ErrorServicioTercero:
                    _ts.transactionProcess.EstadoTransaccionVerb = "Declinada";
                    _ts.paymentProcess.Descripcion += $"Transacción cancelada ocurrió un error en el servicio tercero, No se realizó el pago.";
                    break;
                default:
                    break;
            }

            if (!_ts.paymentProcess.DevueltaCorrecta)
                _ts.paymentProcess.Descripcion += $"Ocurrió un error durante la devolución del dinero. Cantidad faltante ${_paymentViewModel.RemainingAmount.ToString("N0")} ";
        }

        #endregion

    }

    public class PaymentViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;


        #region Attributes
        private decimal _payAmount;

        private decimal _enteredAmount;

        private decimal _remainingAmount;

        private decimal _returnAmount;

        public decimal _dispensedAmount;

        public bool _isReturnSuccess;

        private List<Denomination> _denominations;


        public List<Denomination> Denominations
        {
            get { return _denominations; }
            set
            {
                _denominations = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Denominations)));
            }
        }

        public decimal PayAmount
        {
            get { return _payAmount; }
            set
            {
                if (_payAmount != value)
                {
                    _payAmount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PayAmount)));
                }
            }
        }


        public decimal EnteredAmount
        {
            get { return _enteredAmount; }
            set
            {
                if (_enteredAmount != value)
                {
                    _enteredAmount = value;
                    RemainingAmount = (EnteredAmount < PayAmount) ? PayAmount - EnteredAmount : 0;
                    ReturnAmount = (EnteredAmount > PayAmount) ? EnteredAmount - PayAmount : 0;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnteredAmount)));
                }
            }
        }

        public decimal RemainingAmount
        {
            get { return _remainingAmount; }
            set
            {
                if (_remainingAmount != value)
                {
                    _remainingAmount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemainingAmount)));
                }
            }
        }

        public decimal ReturnAmount
        {
            get { return _returnAmount; }
            set
            {
                if (_returnAmount != value)
                {
                    _returnAmount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReturnAmount)));
                }
            }
        }

        public decimal DispensedAmount
        {
            get { return _dispensedAmount; }
            set
            {
                if (_dispensedAmount != value)
                {
                    _dispensedAmount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DispensedAmount)));
                }
            }
        }

        public bool IsPayCompleted
        {
            get { return _isReturnSuccess; }
            set
            {
                if (_isReturnSuccess != value)
                {
                    _isReturnSuccess = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPayCompleted)));
                }
            }
        }

        #endregion

        #region Methods
        public void RefreshAmountsList(int denomination, int quantity)
        {

            var itemDenomination = Denominations.Where(d => d.DenominationValue == denomination).FirstOrDefault();
            if (itemDenomination == null)
            {
                Denominations.Add(new Denomination
                {
                    DenominationValue = denomination,
                    Quantity = quantity,
                    TotalDenomAmount = denomination * quantity,
                });
                return;
            }

            itemDenomination.Quantity += quantity;
            itemDenomination.TotalDenomAmount = denomination * itemDenomination.Quantity;
        }

        #endregion
    }

    public class Denomination
    {
        public decimal DenominationValue { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalDenomAmount { get; set; }
    }
}