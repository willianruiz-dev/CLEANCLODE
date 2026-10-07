using Domain;
using Domain.Enumerables;
using Domain.UIServices;
using LocalDataBase.Services;
using Presentation.UserControls.Bases;
using System.Text.RegularExpressions;
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
        private bool isRegistered = false;
        string typeDocument;
        private const string STR_TIMER = "03:00";

        private TimerGeneric _timer;

        public FormUC()
        {
            InitializeComponent();
            this.DataContext = _ts.customFlows.generaLInformationClient;
            Transaction.Instance.transactionProcess.TipoTransaccion = TypeTransaction.Pago;
            Transaction.Instance.transactionProcess.TipoRecaudo = "Pago de factura";
            BtnForm.Visibility = Visibility.Hidden;
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

            GoTimer();

        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
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

       
        private async void TxtDocument_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (!string.IsNullOrEmpty(textBox?.Text))
            {
                // Mostrar indicador de carga
                this.Cursor = Cursors.Wait;
                
                try
                {
                    var registeredUser = (await DB_PersonalInfoService.GetByDocument(textBox?.Text ?? string.Empty)).FirstOrDefault();
                    if(registeredUser == null)
                    {
                        return;
                    }
                    isRegistered = true;
                    _ts.customFlows.generaLInformationClient.AssignValues(registeredUser);
                }
                finally
                {
                    // Restaurar cursor normal
                    this.Cursor = Cursors.Arrow;
                }
            }
        }
        
       

        private async void BtnForm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TypeDocument.SelectedItem == null)
            {
                _nav.ShowModal("Por favor, seleccione su tipo de documento.", new InfoModal());
                return;

            }
            if (string.IsNullOrWhiteSpace(_ts.customFlows.generaLInformationClient.Document))
            {
                _nav.ShowModal("Por favor, ingrese su número de documento.", new InfoModal());


                return;  // Retorna temprano para evitar que el usuario continúe
            }
            if (!IsNumeric(_ts.customFlows.generaLInformationClient.Document))
            {
                _nav.ShowModal("Por favor, ingrese un número de documento válido.", new InfoModal());


                return;
            }
            if (string.IsNullOrWhiteSpace(_ts.customFlows.generaLInformationClient.FirstName))
            {
                _nav.ShowModal("Por favor, ingrese sus  nombres.", new InfoModal());
                return;
            }
            if (string.IsNullOrWhiteSpace(_ts.customFlows.generaLInformationClient.LastName))
            {
                _nav.ShowModal("Por favor, ingrese sus  apellidos.", new InfoModal());
                return;
            }
            if (string.IsNullOrWhiteSpace(_ts.customFlows.generaLInformationClient.Mobile))
            {
                _nav.ShowModal("Por favor, ingrese su número de celular.", new InfoModal());
                return;
            }
            if (!IsNumeric(_ts.customFlows.generaLInformationClient.Mobile))
            {
                _nav.ShowModal("Por favor, ingrese un número de celular válido.", new InfoModal());


                return;
            }
            if (!IsValidEmail(_ts.customFlows.generaLInformationClient.Email))
            {
                _nav.ShowModal("Por favor, ingrese un correo electrónico válido.", new InfoModal());
                return;
            }
            _ts.paymentProcess.Documento = _ts.customFlows.generaLInformationClient.Document;
            if(!isRegistered && !(await DB_PersonalInfoService.Create(_ts.customFlows.generaLInformationClient.GenerateDBUserPersonalInfo())))
            {
                _nav.ShowModal("No fue posible almacenar su información personal para futuras ocasiones. Sin embargo, podrá continuar con la transacción iniciada.", new InfoModal());
            }
            Dispatcher.Invoke(() => GoTo(new ReferenceToPayUC()));

        }

       
        private bool IsValidEmail(string email)
        {
            // Regular expression to validate email
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

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
            var a = 1;
        }
    }
}
