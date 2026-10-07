using Domain;
using Domain.Enumerables;
using Domain.UIServices;
using Domain.Validation;
using ApiService.Models;
using WPFHospitalVeterinarioUT.ApiService;
using Presentation.UserControls.Bases;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using UI.Bases;
using UI.Modals;


namespace Presentation.UserControls.Flows
{
    /// <summary>
    /// Interaction logic for FormUC.xaml
    /// </summary>
    public partial class FormUC : AppUserControl
    {
        private const string STR_TIMER = "03:00";

        /// <summary>Cantidad de dígitos de un celular en Colombia.</summary>
        private const int MOBILE_DIGITS = 10;

        public Transaction _ts = Transaction.Instance;
        private readonly HospitalUserService _userService = new();
        private CancellationTokenSource? _documentLookupCancellation;
        private bool _isRegistered;

        /// <summary>Indica si los datos personales visibles se autocompletaron con la consulta del documento.</summary>
        private bool _autoFilledFromLookup;

        private string typeDocument = string.Empty;

        /// <summary>Evita la reentrada cuando el texto del campo se normaliza en vivo.</summary>
        private bool _isAdjustingText;

        /// <summary>Indica si el usuario aceptó la política de tratamiento de datos.</summary>
        private bool _policyAccepted;

        // Campos "tocados": un asterisco no se pinta en rojo antes de que el usuario haya pasado
        // por el campo. Así el formulario se ve igual que siempre hasta que se interactúa con él.
        private bool _touchedDocumentType;
        private bool _touchedDocument;
        private bool _touchedFirstName;
        private bool _touchedLastName;
        private bool _touchedMobile;
        private bool _touchedEmail;
        private bool _touchedPolicy;

        private Brush? _successBrush;
        private Brush? _errorBrush;

        /// <summary>Verde del proyecto (Fonts.xaml) para el campo ya diligenciado bien.</summary>
        private Brush SuccessBrush => _successBrush ??= TryFindResource("SUCCESSCOLOR") as Brush ?? Brushes.Green;

        /// <summary>Rojo del proyecto (Fonts.xaml) para el campo que aún no está bien.</summary>
        private Brush ErrorBrush => _errorBrush ??= TryFindResource("ERRORCOLOR") as Brush ?? Brushes.Red;

        private readonly TimerService _timer = new();

        public FormUC()
        {
            InitializeComponent();
            this.DataContext = _ts.customFlows.generaLInformationClient;
            Transaction.Instance.transactionProcess.TipoTransaccion = TypeTransaction.Pago;
            Transaction.Instance.transactionProcess.TipoRecaudo = "Pago de factura";
            BtnForm.Visibility = Visibility.Hidden;
            BtnForm.IsEnabled = false;
            this.Unloaded += OnUnloaded;

            // Los datos autocompletados desde la API también deben actualizar el estado del botón.
            _ts.customFlows.generaLInformationClient.PropertyChanged += OnPersonalInformationChanged;

            UpdateContinueState();

            GoTimer();

        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _ts.customFlows.generaLInformationClient.PropertyChanged -= OnPersonalInformationChanged;
            _documentLookupCancellation?.Cancel();
            _documentLookupCancellation?.Dispose();
            StopTimer();
        }

        private void OnPersonalInformationChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateContinueState();
        }

        /// <summary>
        /// Habilita el botón continuar únicamente cuando la información del formulario
        /// es válida y el usuario aceptó la política de tratamiento de datos.
        /// </summary>
        private void UpdateContinueState()
        {
            var personalInfo = _ts.customFlows.generaLInformationClient;

            bool isFormValid = PersonalInformationValidator.GetValidationError(
                TypeDocument.SelectedItem != null,
                personalInfo.Document,
                personalInfo.FirstName,
                personalInfo.LastName,
                personalInfo.Mobile,
                personalInfo.Email) == null;

            BtnForm.IsEnabled = _policyAccepted && isFormValid;

            // Una imagen deshabilitada no recibe el toque, de modo que el área que la contiene
            // captura la pulsación para poder indicarle al usuario qué dato falta o está mal.
            // Solo se activa cuando el botón está a la vista; si está oculto, no captura nada.
            BtnFormArea.Background = BtnForm.Visibility == Visibility.Visible
                ? Brushes.Transparent
                : null;

            UpdateRequirementHints();
        }

        /// <summary>
        /// Pinta el asterisco de cada campo: queda como siempre (heredado) mientras el usuario no
        /// haya pasado por el campo, verde cuando el dato ya es válido y rojo cuando ya lo tocó y
        /// todavía no está bien. Usa las mismas reglas del validador que arma el modal, así que el
        /// color y el mensaje nunca pueden contradecirse.
        /// </summary>
        private void UpdateRequirementHints()
        {
            var personalInfo = _ts.customFlows.generaLInformationClient;

            PaintRequirement(ReqDocumentType, _touchedDocumentType, TypeDocument.SelectedItem != null);
            PaintRequirement(ReqDocument, _touchedDocument,
                PersonalInformationValidator.IsValidDocument(personalInfo.Document));
            PaintRequirement(ReqFirstName, _touchedFirstName,
                PersonalInformationValidator.IsValidNameField(personalInfo.FirstName));
            PaintRequirement(ReqLastName, _touchedLastName,
                PersonalInformationValidator.IsValidNameField(personalInfo.LastName));
            PaintRequirement(ReqMobile, _touchedMobile,
                PersonalInformationValidator.IsValidMobile(personalInfo.Mobile));
            PaintRequirement(ReqEmail, _touchedEmail,
                PersonalInformationValidator.IsValidEmail(personalInfo.Email));
            PaintRequirement(ReqPolicy, _touchedPolicy, _policyAccepted);
        }

        /// <summary>Deja el asterisco neutro, verde o rojo según el estado del campo.</summary>
        private void PaintRequirement(System.Windows.Documents.Run asterisk, bool touched, bool isValid)
        {
            if (asterisk == null) return;

            if (!touched)
            {
                // Neutro: hereda el color del rótulo, exactamente como se veía antes.
                asterisk.ClearValue(System.Windows.Documents.Run.ForegroundProperty);
                return;
            }

            asterisk.Foreground = isValid ? SuccessBrush : ErrorBrush;
        }

        /// <summary>
        /// Al intentar enviar con datos incompletos ya se mostró el modal: a partir de ahí todos los
        /// asteriscos quedan pintados, para que se vea de una cuáles faltan.
        /// </summary>
        private void MarkAllFieldsTouched()
        {
            _touchedDocumentType = true;
            _touchedDocument = true;
            _touchedFirstName = true;
            _touchedLastName = true;
            _touchedMobile = true;
            _touchedEmail = true;
            _touchedPolicy = true;
        }

        #region Normalización en vivo de los campos

        /// <summary>Documento: exclusivamente numérico.</summary>
        private static string KeepDigits(string? value) =>
            new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>Campo numérico con una longitud máxima (celular: 10 dígitos).</summary>
        private static string KeepDigitsMax(string? value, int maxLength)
        {
            var digits = KeepDigits(value);
            return digits.Length > maxLength ? digits[..maxLength] : digits;
        }

        /// <summary>Nombres y apellidos: sin números.</summary>
        private static string KeepNameCharacters(string? value) =>
            new string((value ?? string.Empty)
                .Where(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c == '\'' || c == '-' || c == '.')
                .ToArray());

        /// <summary>Correo: sin espacios.</summary>
        private static string KeepEmailCharacters(string? value) =>
            new string((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());

        /// <summary>
        /// Aplica el filtro del campo sobre el texto actual y devuelve el valor limpio.
        /// No vuelve a entrar al manejador de TextChanged que la invoca.
        /// </summary>
        private string AdjustText(TextBox textBox, Func<string?, string> sanitize)
        {
            var current = textBox.Text ?? string.Empty;
            var sanitized = sanitize(current);

            if (!string.Equals(current, sanitized, StringComparison.Ordinal))
            {
                _isAdjustingText = true;
                try
                {
                    textBox.Text = sanitized;
                }
                finally
                {
                    _isAdjustingText = false;
                }
            }

            return sanitized;
        }

        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || _isAdjustingText)
                return;

            _touchedFirstName = true;
            var names = AdjustText(textBox, KeepNameCharacters);
            _ts.customFlows.generaLInformationClient.FirstName = names;
            UpdateContinueState();
        }

        private void TxtLastName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || _isAdjustingText)
                return;

            _touchedLastName = true;
            var lastNames = AdjustText(textBox, KeepNameCharacters);
            _ts.customFlows.generaLInformationClient.LastName = lastNames;
            UpdateContinueState();
        }

        private void TxtMobile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || _isAdjustingText)
                return;

            _touchedMobile = true;
            var mobile = AdjustText(textBox, value => KeepDigitsMax(value, MOBILE_DIGITS));
            _ts.customFlows.generaLInformationClient.Mobile = mobile;
            UpdateContinueState();
        }

        private void TxtEmail_changed(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || _isAdjustingText)
                return;

            _touchedEmail = true;
            var email = AdjustText(textBox, KeepEmailCharacters);
            _ts.customFlows.generaLInformationClient.Email = email;
            UpdateContinueState();
        }

        #endregion

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _touchedDocumentType = true;
            ComboBox comboBox = sender as ComboBox;
            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                typeDocument = selectedItem.Content.ToString();
                _ts.customFlows.generaLInformationClient.DocumentType = typeDocument ?? string.Empty;
            }

            UpdateContinueState();
        }

        private async void TxtDocument_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || _isAdjustingText)
                return;

            _touchedDocument = true;
            // El documento solo admite dígitos: se descarta cualquier otro carácter.
            var document = AdjustText(textBox, KeepDigits);
            _ts.customFlows.generaLInformationClient.Document = document;

            _isRegistered = false;

            _documentLookupCancellation?.Cancel();
            _documentLookupCancellation?.Dispose();
            _documentLookupCancellation = new CancellationTokenSource();
            var cancellationToken = _documentLookupCancellation.Token;

            // Evita consultar la API por cada tecla y descarta respuestas de documentos anteriores.
            if (document.Length < 6)
            {
                // Al borrar el documento se descartan los datos que se habían autocompletado.
                ClearAutoFilledPersonalData();
                UpdateContinueState();
                return;
            }

            try
            {
                await Task.Delay(400, cancellationToken);
                Cursor = Cursors.Wait;
                var user = await _userService.GetByDocumentAsync(document, cancellationToken);

                if (cancellationToken.IsCancellationRequested ||
                    !string.Equals(textBox.Text, document, StringComparison.Ordinal))
                    return;

                if (user == null)
                {
                    // El documento consultado no corresponde a un usuario registrado:
                    // se limpian los datos que hubieran quedado de una consulta anterior.
                    ClearAutoFilledPersonalData();
                    return;
                }

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

                UpdateContinueState();
            }
        }

        /// <summary>
        /// Borra los datos personales que se autocompletaron desde la consulta del documento.
        /// Solo se limpian los campos que provinieron de la consulta, no lo que el usuario escribió.
        /// </summary>
        private void ClearAutoFilledPersonalData()
        {
            if (!_autoFilledFromLookup)
                return;

            _autoFilledFromLookup = false;

            var personalInfo = _ts.customFlows.generaLInformationClient;
            personalInfo.FirstName = string.Empty;
            personalInfo.LastName = string.Empty;
            personalInfo.Mobile = string.Empty;
            personalInfo.Email = string.Empty;
        }

        private void AssignUser(UserPersonalInfoDto user)
        {
            var personalInfo = _ts.customFlows.generaLInformationClient;
            _autoFilledFromLookup = true;
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

            // Se descartan los espacios sobrantes antes de validar y almacenar.
            personalInfo.Document = (personalInfo.Document ?? string.Empty).Trim();
            personalInfo.FirstName = (personalInfo.FirstName ?? string.Empty).Trim();
            personalInfo.LastName = (personalInfo.LastName ?? string.Empty).Trim();
            personalInfo.Mobile = (personalInfo.Mobile ?? string.Empty).Trim();
            personalInfo.Email = (personalInfo.Email ?? string.Empty).Trim();

            var validationError = PersonalInformationValidator.GetValidationError(
                TypeDocument.SelectedItem != null,
                personalInfo.Document,
                personalInfo.FirstName,
                personalInfo.LastName,
                personalInfo.Mobile,
                personalInfo.Email);

            if (validationError != null)
            {
                // Ya hay un intento de envío: los asteriscos se pintan para señalar lo que falta.
                MarkAllFieldsTouched();
                UpdateRequirementHints();

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
                TxtTimer.Text = STR_TIMER;

                _timer.Start(STR_TIMER,
                    onTick: time => TxtTimer.Text = time,
                    onTimeout: () => GoTo(new PublicityUC()));
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }

        public void StopTimer()
        {
            _timer.Stop();
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
                _touchedPolicy = true;
                _policyAccepted = toggleButton.IsChecked ?? false;

                if (_policyAccepted)
                    { BtnForm.Visibility = Visibility.Visible; }
                else
                {
                    BtnForm.Visibility = Visibility.Hidden;
                }

                UpdateContinueState();
            }
        }
    }
}
