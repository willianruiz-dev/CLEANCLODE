using Domain;
using Domain.Enumerables;
using Domain.UIServices;
using Domain.Validation;
using Presentation.UserControls.Bases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using UI.Bases;
using UI.Modals;


namespace Presentation.UserControls.Flows
{
    /// <summary>
    /// Interaction logic for FormUC.xaml
    /// </summary>
    public partial class FormUC : AppUserControl
    {
        
        public Transaction _ts = Transaction.Instance;
        string typeDocument;
        private const string STR_TIMER = "03:00";

        private TimerGeneric? _timer;

        public FormUC()
        {
            InitializeComponent();
            this.DataContext = _ts.customFlows.generaLInformationClient;
            Transaction.Instance.transactionProcess.TipoTransaccion = TypeTransaction.Pago;
            Transaction.Instance.transactionProcess.TipoRecaudo = "Pago de factura";
            BtnForm.Visibility = Visibility.Hidden;
            this.Unloaded += OnUnloaded;

            GoTimer();

        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
           
            StopTimer();
        }

        
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                typeDocument = selectedItem.Content.ToString();
                _ts.customFlows.generaLInformationClient.DocumentType = typeDocument ?? string.Empty;
            }
        }

        private void TxtDocument_TextChanged(object sender, TextChangedEventArgs e)
        {
            // El documento se conserva mediante el binding; no se consulta almacenamiento local.
        }

        private void BtnForm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var personalInfo = _ts.customFlows.generaLInformationClient;
            var validationError = PersonalInformationValidator.GetValidationError(
                TypeDocument.SelectedItem != null,
                personalInfo.Document,
                personalInfo.FirstName,
                personalInfo.LastName,
                personalInfo.Mobile,
                personalInfo.Email);

            if (validationError != null)
            {
                _nav.ShowModal(validationError, new InfoModal());
                return;
            }

            _ts.paymentProcess.Documento = personalInfo.Document;
            GoTo(new ReferenceToPayUC());
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


        private void BtnSalir_MouseDown(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => GoTo(new PublicityUC()));
        }

        private void BtnAtras_MouseDown(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => GoTo(new WelcomeUC()));
        }

        private void BtnTreatments_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.Invoke(()=> GoTo(new TreatmentPolicy()));
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // Obtener el ToggleButton desde el sender
            ToggleButton toggleButton = sender as ToggleButton;

            if (toggleButton != null)
            {
                bool isChecked = toggleButton.IsChecked ?? false;
                if (isChecked) 
                    { BtnForm.Visibility = Visibility.Visible; } 
                else
                {
                    BtnForm.Visibility = Visibility.Hidden;
                }
               
            }
        }

        private void TxtEmail_changed(object sender, TextChangedEventArgs e)
        {
            // El valor se actualiza mediante binding.
        }
    }
}
