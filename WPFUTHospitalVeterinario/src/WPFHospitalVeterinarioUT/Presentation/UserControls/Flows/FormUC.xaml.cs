using Domain;
using Domain.Enumerables;
using Domain.UIServices;
using Domain.Validation;
using ApiService.Models;
using WPFHospitalVeterinarioUT.ApiService;
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
        private readonly HospitalUserService _userService = new();
        private CancellationTokenSource? _documentLookupCancellation;
        private bool _isRegistered;
        private string typeDocument = string.Empty;
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
           
            _documentLookupCancellation?.Cancel();
            _documentLookupCancellation?.Dispose();
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
            if (sender is not TextBox textBox)
                return;

            var document = new string(textBox.Text.Where(char.IsDigit).ToArray());
            _isRegistered = false;

            _documentLookupCancellation?.Cancel();
            _documentLookupCancellation?.Dispose();
            _documentLookupCancellation = new CancellationTokenSource();
            var cancellationToken = _documentLookupCancellation.Token;

            // Evita consultar la API por cada tecla y descarta respuestas de documentos anteriores.
            if (document.Length < 6)
                return;

            try
            {
                await Task.Delay(400, cancellationToken);
                Cursor = Cursors.Wait;
                var user = await _userService.GetByDocumentAsync(document, cancellationToken);

                if (cancellationToken.IsCancellationRequested ||
                    !string.Equals(textBox.Text, document, StringComparison.Ordinal))
                    return;

                if (user == null)
                    return;

                _isRegistered = true;
                AssignUser(user);
                SelectDocumentType(user.DocumentType);
            }
            catch (OperationCanceledException)
            {
                // Se escribió otro documento antes de finalizar la consulta anterior.
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error,
                    "No fue posible consultar la información personal mediante la API.", ex);
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                    Cursor = Cursors.Arrow;
            }
        }

        private void AssignUser(UserPersonalInfoDto user)
        {
            var personalInfo = _ts.customFlows.generaLInformationClient;
            personalInfo.Document = user.Document;
            personalInfo.DocumentType = user.DocumentType;
            personalInfo.FirstName = user.Name;
            personalInfo.LastName = user.LastName;
            personalInfo.Mobile = user.Mobile;
            personalInfo.Email = user.Email;
        }

        private void SelectDocumentType(string documentType)
        {
            foreach (var item in TypeDocument.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Content?.ToString(), documentType, StringComparison.OrdinalIgnoreCase))
                {
                    TypeDocument.SelectedItem = item;
                    return;
                }
            }
        }

        private async void BtnForm_MouseDown(object sender, MouseButtonEventArgs e)
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

            if (!_isRegistered)
            {
                var saved = await _userService.CreateOrUpdateAsync(new UserPersonalInfoDto
                {
                    Document = personalInfo.Document,
                    DocumentType = personalInfo.DocumentType,
                    Name = personalInfo.FirstName,
                    LastName = personalInfo.LastName,
                    Mobile = personalInfo.Mobile,
                    Email = personalInfo.Email
                });

                if (!saved)
                {
                    _nav.ShowModal(
                        "No fue posible almacenar su información personal para futuras ocasiones. Sin embargo, podrá continuar con la transacción iniciada.",
                        new InfoModal());
                }
            }

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
