using ApiService.Models;
using Domain.Enumerables;
using Domain.Peripherals.Recorder;
using Domain.UIServices.Models;
using System.ComponentModel;
using WPFHospitalVeterinarioUT.ApiService;

namespace Domain.UIServices
{
    public class Transaction
    {
        // Patron de Diseño Singleton
        private static Transaction? _instance;
  

        public static Transaction Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Transaction();
                return _instance;
            }
        }

        public static void Reset()
        {
            _instance = null;
        }

        private Transaction() { }
        public int IdPaypad { get; set; }
        public int IdTransaccionApi { get; set; }
        public TransactionProcess transactionProcess { get; set; } = new();
        public PaymentProcess paymentProcess { get; set; } = new();
        public CustomFlowHelpers customFlows { get; set; } = new();


        //Integración
        public List<Invoice> ListaFacturas { get; set; } = new List<Invoice>();
        public VideoRecorder? videoRecorder { get; set; }

        /// <summary>
        /// Guarda la calificación de la transacción actual
        /// </summary>
        /// <param name="rating">Calificación de 1 a 5 estrellas</param>
        /// <returns>True si se guardó exitosamente</returns>
        public bool SaveRating(int rating)
        {
            try
            {
                paymentProcess.Calificacion = rating.ToString();
                EventLogger.SaveLog(EventType.Info, $"Guardando calificación {rating} estrellas para la transacción {IdTransaccionApi}");
                ApiDashboard.SetTransactionRating(IdTransaccionApi, rating);
                return true;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Error guardando calificación: {ex.Message}", ex);
                return false;
            }
        }
    }
    //public class ScreensProcess
    //{
    //    public IUIManager ScreenManger;
    //}

    public class TransactionProcess {

        public ApiService.Models.TransactionDto ApiDto { get; set; }
        public string? TipoRecaudo { get; set; }
        public string? TipoConsulta {  get; set; }
        public TypeTransaction TipoTransaccion { get; set; }
        public TypePayment TipoPago { get; set; }
        public StateTransaction EstadoTransaccion { get; set; }
        public string EstadoTransaccionVerb { get; set; }
    }
    public class PaymentProcess
    {
        public string? Referencia { get; set; }
        public string? Documento { get; set; }
        public string? Descripcion { get; set; }
        public string? FechaVencimiento { get; set; }
        public decimal TotalSinRedondear { get; set; }
        public decimal Total { get; set; }
        public decimal TotalDevuelta { get; set; }
        public decimal TotalIngresado { get; set; }
        public decimal ValorFaltante { get; set; }
        public bool DevueltaCorrecta { get; set; }
        public string Calificacion { get; set; }

    }


    public class CustomFlowHelpers
    {
        public GeneralInformationClient generaLInformationClient { get; set; } = new GeneralInformationClient();
    }
    public class GeneralInformationClient : INotifyPropertyChanged
    {
        #region Properties
        public string _firstName;
        public string _lastName;
        public string _documentType;
        public string _document;
        public string _mobile;
        public string _email;

        public string FirstName
        {
            get { return _firstName; }
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    OnPropertyRaised(nameof(FirstName));
                }
            }
        }
        public string LastName
        {
            get { return _lastName; }
            set
            {
                if (_lastName != value)
                {
                    _lastName = value;
                    OnPropertyRaised(nameof(LastName));
                }
            }
        }
        public string DocumentType
        {
            get { return _documentType; }
            set
            {
                if (_documentType != value)
                {
                    _documentType = value;
                    OnPropertyRaised(nameof(DocumentType));
                }
            }
        }
        public string Document
        {
            get { return _document; }
            set
            {
                if (_document != value)
                {
                    _document = value;
                    OnPropertyRaised(nameof(Document));
                }
            }
        }
        public string Mobile
        {
            get { return _mobile; }
            set
            {
                if (_mobile != value)
                {
                    _mobile = value;
                    OnPropertyRaised(nameof(Mobile));
                }
            }
        }
        public string Email
        {
            get { return _email; }
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyRaised(nameof(Email));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyRaised(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public void ResetValues()
        {
            this.FirstName = string.Empty;
            this.LastName = string.Empty;
            this.Document = string.Empty;
            this.DocumentType = string.Empty;
            this.Mobile = string.Empty;
            this.Email = string.Empty;
        }
    }
}
