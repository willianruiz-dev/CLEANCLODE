// ────────────────────────────────────────────────────────────────────────────────
// Reemplazo del controlador de periféricos. Versión de referencia: proyecto
// WPF_LA_OFRENDA_V1 (Arduino que opera de forma estable en producción).
// Adaptaciones respecto a esa versión, únicamente de integración:
//   · namespace WPF_LA_OFRENDA_V1.Domain.Peripherals → Domain.Peripherals
//   · using HantleDispenserAPI → Domain.HantleDispenserAPI
//   · using WPF_LA_OFRENDA_V1.Domain.Utils → (AppConfig/EventLogger/EventType viven en Domain)
// Se elimina por completo la compilación condicional DISPENSER_CONTROLLED_BY_ARDUINO:
// los delegates quedan fijos en Dictionary<int,int>, que es lo que consume PaymentUC.
// ────────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using Domain.HantleDispenserAPI;


namespace Domain.Peripherals
{
    internal static class ArduinoCommand
    {
        public static string START = "OR:START";//Iniciar los billeteros
        public static string JCM_ON = "OR:ON:AP";//Operar billetero Aceptance
        public static string DISPENSER_ON = "OR:ON:DP";//Operar billetero Dispenser
        public static string JCM_OFF = "OR:OFF:AP";//Cerrar billetero Aceptance
        public static string DISPENSER_OFF = "OR:OFF:DP";//Cerrar billetero Dispenser
        public static string COIN_ACEPTANCE_ON = "OR:ON:MA";//Operar Monedero Aceptance
        public static string COIN_DISPENSE_ON = "OR:ON:MD:";//Operar Monedero Dispenser
        public static string COIN_ACEPTANCE_OFF = "OR:OFF:MA";//Cerrar Monedero Aceptance
    }

    public delegate void CashInHandler(decimal value);
    public delegate void CashDispensedHandler(decimal value, Dictionary<int, int> details);
    public delegate void DispenserRejectHandler(Dictionary<int, int> dataReject);
    public delegate void PeripheralErrorHandler(Exception ex);
    public class ArduinoController
    {
        // Patron de Diseño Singleton
        private static ArduinoController? _instance;
        public static ArduinoController Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ArduinoController();

                return _instance;
            }
        }

        public static void Initialize(string arduinoPort, string availDispenserDenom)
        {
            ArduinoController pc = Instance;
            try
            {
                pc._acceptorDevice = AppConfig.Get("acceptorDevice");

                pc._serialPort = new SerialPort();
                pc.InitPort(arduinoPort);

                if (pc._mei == null && pc._acceptorDevice == "MEI")
                {
                    pc._mei = new MeiAcceptor();
                    pc._mei.BillAccepted += pc.OnMeiCashIn;
                    pc._mei.AcceptorError += pc.OnMeiError;
                }

                if (string.IsNullOrEmpty(availDispenserDenom)) throw new Exception("No se proporcionaron las denominaciones disponibles en el aceptador");

                pc.SetDispenserDenoms(availDispenserDenom);

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                throw;
            }
        }

        public static void Reset()
        {
            _instance = null;
        }

        /* ------ Atributos y metodos de clase ---------------*/
        private ArduinoController() { }

        #region Atributes
        private const string _STR_TIMER = "01:00";
     

        private SerialPort _serialPort;

        private MeiAcceptor _mei;

        public event CashInHandler? CashIn;
        public event CashDispensedHandler? CashDispensed;
        public event DispenserRejectHandler? DispenserReject;
        public event PeripheralErrorHandler? PeripheralError;



        private const int _SCALE_FACTOR_BILL = 1;
        private const int _SCALE_FACTOR_COIN = 100;


        private decimal _payValue;//Valor a pagar
        private List<Tuple<string, int>> _availDenomsDispenser;
        private List<int> _denominations;
        private decimal _enteredAmount;//Valor ingresado
        private decimal _deliveryAmount;//Valor entregado
        public decimal DeliveryVal { get; set; }
        private decimal _amountToDispense;//Valor a dispensar
        private bool _arduinoStatusError;

        //Monedero
        private string _arduinoErrDescription = string.Empty;
        private string _valuesOK_DP = string.Empty;
        private string _valuesOK_MD = string.Empty;
        private string _valuesBX_DP = string.Empty;
        private string _rawReturnCoins = string.Empty;

        private bool _peripheralStartSuccess = false;
        private static string ArduinoToken;//Llave que retorna el dispenser

        private string _acceptorDevice = string.Empty;

        private HandlerAcceptorProcess _hAcceptorProcess;
        #endregion

        #region Events

        private void OnMeiCashIn(decimal value)
        {
            if (value > 0)
            {
                _enteredAmount += value;
                CashIn?.Invoke(value);
            }
        }

        private void OnMeiError(Exception ex)
        {
            PeripheralError?.Invoke(ex);
        }



        #endregion

        #region Init Methods
        /// <summary>
        /// Método para inciar el puerto de los billeteros
        /// </summary>
        private void InitPort(string portName)
        {
            try
            {
                if (_serialPort.IsOpen) throw new Exception("El puerto del arduino está ocupado, no se pudo inicializar");

                _serialPort.PortName = portName;
                _serialPort.ReadTimeout = 3000;
                _serialPort.ReceivedBytesThreshold = 2;
                _serialPort.WriteTimeout = 1000;
                _serialPort.BaudRate = 57600;
                _serialPort.DtrEnable = true;
                _serialPort.RtsEnable = true;
                _serialPort.Open();

                _serialPort.DataReceived += new SerialDataReceivedEventHandler(ArduinoDataReceived);
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                throw;
            }
        }

        private void SetDispenserDenoms(string values)
        {
            try
            {
                if (string.IsNullOrEmpty(values))
                {
                    EventLogger.SaveLog(EventType.Error, $"No se enviaron las denominaciones disponibles. no se configuraron.");
                    return;
                }

                _availDenomsDispenser = new List<Tuple<string, int>>();
                _denominations = new List<int>();
                var denominations = values.Split('-');

                if (denominations.Length <= 0)
                {
                    EventLogger.SaveLog(EventType.Error, $"No se enviaron las denominaciones disponibles con formato correcto, no se configuraron");
                    return;
                }

                foreach (var denomination in denominations)
                {
                    var value = denomination.Split(':');
                    if (value.Length == 2)
                    {
                        _availDenomsDispenser.Add(Tuple.Create(value[0], Convert.ToInt32(value[1])));
                        _denominations.Add(Convert.ToInt32(value[1]));
                    }
                }

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Método que inicializa los billeteros
        /// </summary>
        public async Task<bool> SendStart()
        {
            try
            {
                _peripheralStartSuccess = false;
                ArduinoToken = string.Empty;

                _hAcceptorProcess = new HandlerAcceptorProcess();
                if (!await Dispenser.Start())
                    return false;

                if (!SendDataArduino(ArduinoCommand.START))
                {
                    throw new Exception("No se pudieron iniciar los billeteros");
                }

                int tries = 6;
                while (tries > 0) // Esperar la respuesta del arduino
                {
                    tries--;
                    await Task.Delay(1000);
                    if (_peripheralStartSuccess)
                        return true;
                }

                throw new Exception("No se pudieron iniciar los billeteros");
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                return false;
            }
        }
        #endregion

        #region Send Message
        /// <summary>
        /// Método para enviar orden al puerto de los billeteros
        /// </summary>
        /// <param name="message">mensaje a enviar</param>
        private bool SendDataArduino(string message)
        {
            try
            {
                EventLogger.SaveLog(EventType.P_Arduino, $"Enviando {message}...");

                if (!_serialPort.IsOpen) throw new Exception($"Se ha cerrado el puerto del arduino no se pudo enviar el comando {message}");



                _serialPort.Write(message);

                return true;

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Arduino, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                return false;
            }
        }
        #endregion

        #region Get Message
        /// <summary>
        /// Método que escucha la respuesta del puerto del billetero
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArduinoDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {

                string response = _serialPort.ReadLine();

                EventLogger.SaveLog(EventType.P_Arduino, $"Respuesta Recibida {response}...");

                if (!string.IsNullOrEmpty(response))
                {
                    ProcessArduinoResponse(response.Replace("\r", string.Empty));
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Arduino, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }
        #endregion

        #region Process Response
        /// <summary>
        /// Método que procesa la respuesta del puerto de los billeteros
        /// </summary>
        /// <param name="message">respuesta del puerto de los billeteros</param>
        private void ProcessArduinoResponse(string message)
        {
            string[] response = message.Split(':');
            switch (response[0])
            {
                case "RC":
                    ProcessRC(response);
                    break;
                case "ER":
                    ProcessER(response);
                    break;
                case "UN":
                    ProcessUN(response);
                    break;
                case "TO":
                    ProcessTO(response);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region ProcessResponseCases
        /// <summary>
        /// Respuesta para el caso de Recepción de un mensaje enviado
        /// </summary>
        /// <param name="response">respuesta</param>
        private async void ProcessRC(string[] response)
        {
            EventLogger.SaveLog(EventType.P_Arduino, $"Procesando RC ({String.Join(":", response)})");

            if (response[1] != "OK")
            {
                EventLogger.SaveLog(EventType.P_Arduino, "RC no arroja OK, no se ejecuta ninguna acción");
                return;
            }

            switch (response[2])
            {
                case "AP":
                    break;
                case "DP":
                    if (response[3] == "HD" && !string.IsNullOrEmpty(response[4]))
                    {
                        ArduinoToken = response[4].Replace("\r", string.Empty);
                        EventLogger.SaveLog(EventType.P_Arduino, $"RC Correcto token recibido: {ArduinoToken}");

                        if (_acceptorDevice == "MEI")
                        {
                            if (!_mei.IsConnected) await _mei.OpenAcceptor(AppConfig.Get("meiPort"));
                            _peripheralStartSuccess = _mei.IsConnected;
                        }
                        else _peripheralStartSuccess = true;

                    }
                    break;
                case "MD":
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// Respuesta para el caso de error
        /// </summary>
        /// <param name="response">respuesta</param>
        private void ProcessER(string[] response)
        {
            EventLogger.SaveLog(EventType.P_Arduino, $"Procesando ER ({String.Join(":", response)})");

            if (response[1] == "DP" || response[1] == "MD")
            {
                if (response[2].StartsWith("Abnormal Near End sensor"))
                {
                    EventLogger.SaveLog(EventType.P_Arduino, $"{response[2]}: Alguno de los baules de dispensación está quedandose sin billetes");
                    return;
                }

                _arduinoStatusError = true;
                if (response[2] == "FATAL")
                    this._arduinoErrDescription = response[3];
                else
                    this._arduinoErrDescription = response[2];

                //Evaluar si es necesario invocar Peripheral Error para la inicialización
                EventLogger.SaveLog(EventType.P_Arduino, $"Error dispensadores: {response[2]}");
                return;
            }

            if (response[1] == "AP")
            {
                _arduinoStatusError = true;
                if (this._hAcceptorProcess.LastError == response[2])
                    return;
                EventLogger.SaveLog(EventType.P_Arduino, $"Error Aceptador Arduino: {response[2]}");
                PeripheralError?.Invoke(new Exception("Error Aceptador Arduino: " + response[2]));
                return;
            }
            else if (response[1] == "FATAL")
            {
                EventLogger.SaveLog(EventType.P_Arduino, $"Error Fatal Arduino: {response[2]}");
                PeripheralError?.Invoke(new Exception("Error Fatal Arduino: " + response[2]));
                return;
            }

            EventLogger.SaveLog(EventType.P_Arduino, $"Error desconocido Arduino: {response[2]} no se tomó ninguna acción.");
        }

        /// <summary>
        /// Respuesta para el caso de ingreso o salida de un billete/moneda
        /// </summary>
        /// <param name="response">respuesta</param>
        private void ProcessUN(string[] response)
        {

            EventLogger.SaveLog(EventType.P_Arduino, $"Procesando UN ({String.Join(":", response)})");


            switch (response[1])
            {
                case "AP":
                    _enteredAmount = Convert.ToDecimal(response[2]) * _SCALE_FACTOR_BILL;
                    break;
                case "MA":
                    _enteredAmount = Convert.ToDecimal(response[2]);
                    break;
                default:
                    break;
            }

            if (_enteredAmount > 0) CashIn?.Invoke(_enteredAmount);


        }

        /// <summary>
        /// Respuesta para el caso de total cuando responde el billetero/monedero dispenser
        /// </summary>
        /// <param name="response">respuesta</param>
        private void ProcessTO(string[] response)
        {

            //TODO: Poner un ejemplo de las respuestas

            EventLogger.SaveLog(EventType.P_Arduino, $"Procesando TO ({String.Join(":", response)})");

            string responseFull = response[2] + ":" + response[3];

            switch (response[1])
            {
                case "OK": // Primero llega el OK y lo guardamos y si hay monedas llega un OK de ultimo
                    if (response[2] == "DP")
                        _valuesOK_DP = response[3];
                    else if (response[2] == "MD")
                        _valuesOK_MD = response[3];
                    break;
                case "BX": // despues del OK llega el BX y también lo guardamos
                    _valuesBX_DP = response[3];
                    break;
                default:
                    break;

            }

            EvaluateDataDispenser(responseFull, typeTO: response[1]);


        }
        #endregion

        public void ClearValues()
        {
            _deliveryAmount = 0;
            _enteredAmount = 0;
            DeliveryVal = 0;
            _rawReturnCoins = string.Empty;
            CashIn = null;
            CashDispensed = null;
            DispenserReject = null;
            PeripheralError = null;
            _hAcceptorProcess = new HandlerAcceptorProcess();
        }

        #region Dispenser
        /// <summary>
        /// Inicia el proceso paara el billetero dispenser
        /// </summary>
        /// <param name="valueDispenser">valor a dispensar</param>
        public async void StartDispenser(decimal valueDispenser)
        {
            try
            {
                EventLogger.SaveLog(EventType.P_Arduino, "Iniciando dispensación");
                _arduinoStatusError = false;
                _arduinoErrDescription = string.Empty;
                _amountToDispense = valueDispenser;

                await Dispenser.DispenseAmount((int)_amountToDispense);
                DeliveryVal += Dispenser.DispensedValue;
                DispenserReject?.Invoke(Dispenser.RejectData);
                if (Dispenser.CoinsValue <= 0 || Dispenser.CoinsValue > 1900)
                {
                    _rawReturnCoins = "500-0;100-0";
                    FinishDispensation();
                    return;
                }

                SendDispense(Dispenser.CoinsValue.ToString());

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Arduino, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                PeripheralError?.Invoke(ex);

            }
        }



        /// <summary>
        /// Enviar la orden de dispensar al billetero
        /// </summary>
        /// <param name="valueToDispend"></param>
        private async void SendDispense(string valueToDispend)
        {
            try
            {

                EventLogger.SaveLog(EventType.P_Arduino, $"Se enviará dispensación por valor de {valueToDispend}");
                await Task.Delay(1000);
                if (!string.IsNullOrEmpty(ArduinoToken))
                {
                    string message = string.Format("{0}:{1}:{2}", ArduinoCommand.DISPENSER_ON, ArduinoToken, valueToDispend);
                    SendDataArduino(message);
                    return;
                }

                EventLogger.SaveLog(EventType.P_Arduino, $"No hay token. no se realizó ninguna acción.");
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Arduino, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                PeripheralError?.Invoke(ex);
            }
        }

        /// <summary>
        /// Procesa la respuesta de los dispenser M y B
        /// </summary>
        /// <param name="data">respuesta</param>
        /// <param name="isRj">si se fue o no al reject</param>
        private void EvaluateDataDispenser(string data, string typeTO)
        {
            try
            {

                EventLogger.SaveLog(EventType.P_Arduino, $"Evaluando dato del dispensador {data}, Tipo = {typeTO}");

                bool isCoinsReturn = data.Split(':')[0] == "MD";
                string[] values = data.Split(':')[1].Split(';');


                if (typeTO == "OK")
                {
                    _rawReturnCoins += data.Split(":")[1] + ";";
                    foreach (var value in values)
                    {
                        int denominacion = Convert.ToInt32(value.Split('-')[0]);
                        int cantidad = Convert.ToInt32(value.Split('-')[1]);
                        DeliveryVal += denominacion * cantidad;
                    }

                }

                if (isCoinsReturn)
                {


                    FinishDispensation();
                }

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Arduino, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                PeripheralError?.Invoke(ex);
            }
        }

        private void FinishDispensation()
        {
            var details = DeserializeDispenserResponse(_rawReturnCoins);
            foreach (var denom in details.Keys)
            {
                Dispenser.DispensedData.Add(Convert.ToInt32(denom), details[denom]);
            }
            EventLogger.SaveLog(EventType.P_Arduino, $"Valor de devuelta total {DeliveryVal}");
            CashDispensed?.Invoke(DeliveryVal, Dispenser.DispensedData);
            ClearValues();
        }

        #endregion

        #region Aceptance
        /// <summary>
        /// Inicia la operación de billetero aceptance
        /// </summary>
        /// <param name="payValue">valor a pagar</param>
        public void StartAcceptance(decimal payValue)
        {
            try
            {
                _payValue = payValue;
                bool isSuccess = false;
                if (_acceptorDevice == "MEI") isSuccess = _mei.EnableAcceptance();
                else isSuccess = SendDataArduino(ArduinoCommand.JCM_ON);

                if (!isSuccess) throw new Exception("No se pudo iniciar el aceptador. Respuesta negativa");
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Arduino, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                PeripheralError?.Invoke(ex);
            }
        }



        /// <summary>
        /// Para la aceptación de dinero
        /// </summary>
        public async Task StopAceptance()
        {
            CashIn = null;
            if (_acceptorDevice == "MEI") _mei.DisableAcceptance();
            else
            {
                SendDataArduino(ArduinoCommand.JCM_OFF);
                await Task.Delay(300); //Tiempo para que no se envíe ningún comando mientras se apaga
            }
        }
        #endregion

        private Dictionary<string, int> DeserializeDispenserResponse(string res)
        {
            try
            {
                if (string.IsNullOrEmpty(res)) res = "500-0;100-0";
                if (res.Last() == ';') res = res.Substring(0, res.Length - 1);
                var resDenoms = res.Split(";");
                Dictionary<string, int> resDeserialized = new();
                foreach (var denom in resDenoms)
                {
                    var denomValue = denom.Split("-")[0];
                    var quantity = Convert.ToInt32(denom.Split("-")[1]);
                    if (resDeserialized.ContainsKey(denomValue))
                        continue;
                    resDeserialized.Add(denomValue, quantity);

                }
                return resDeserialized;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                Dictionary<string, int> resDeserialized = new()
                {
                    { "500", 0 },
                    { "100", 0 }
                };
                return resDeserialized;

            }
        }


    }

    internal class HandlerDispenserProcess
    {
        public string ValuesOK_DP { get; set; } = string.Empty;
        public string ValuesOK_MD { get; set; } = string.Empty;
        public string ValuesBX_DP { get; set; } = string.Empty;
        public List<int> Denominations { get; set; } = new List<int>();
        public string RealReturn { get; set; } = string.Empty;
        public int ValueToDispend { get; set; } = 0;
        public int RemainingValue { get; set; }

        public string LastError { get; set; } = string.Empty;

        public HandlerDispenserProcess(List<int> denominations)
        {

            Denominations = denominations;
        }
        public HandlerDispenserProcess()
        {
            LastError = string.Empty;
            ValuesBX_DP = string.Empty;
            ValuesOK_DP = string.Empty;
            ValuesOK_MD = string.Empty;
            Denominations = new List<int>();
        }




    }
    internal class HandlerAcceptorProcess
    {
        public string LastError { get; set; } = string.Empty;
        public HandlerAcceptorProcess()
        {
            LastError = string.Empty;
        }

    }
}
