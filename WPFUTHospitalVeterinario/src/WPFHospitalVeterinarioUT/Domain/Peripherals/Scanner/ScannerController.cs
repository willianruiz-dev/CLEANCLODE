using System.IO.Ports;

namespace Domain.Peripherals
{
    public delegate void ScannerDataReceivedHandler(string data);
    public delegate void ScannerErrorHandler(string error);
    public static class ScannerController
    {
        private static SerialPort? _scannerReader;
        private static bool _cancelFurtherDataReceived;
        public static event ScannerDataReceivedHandler? ScannerDataReceived;
        public static event ScannerErrorHandler? ScannerError;

        static ScannerController()
        {
            _scannerReader = new SerialPort();
        }

        public static void Start()
        {
            if (_scannerReader == null)
            {
                _scannerReader = new SerialPort();
            }

            try
            {
                InitPort();
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                ScannerError?.Invoke("Ocurrió un error iniciando el puerto serial");
            }

        }

        private static void InitPort()
        {

            if (_scannerReader != null && !_scannerReader.IsOpen)
            {
                _scannerReader.PortName = AppConfig.Get("ScannerPort");
                _scannerReader.BaudRate = 9600;
                _scannerReader.ReadTimeout = 200;
                _scannerReader.DataReceived += new SerialDataReceivedEventHandler(Scanner_DataReceived);
                _scannerReader.Open();
            }
        }

        public static void Stop()
        {
            _cancelFurtherDataReceived = false;
            if (_scannerReader != null && _scannerReader.IsOpen)
            {
                _scannerReader.Close();
                _scannerReader.Dispose();
                _scannerReader = null;
            }
        }

        public static void ReactivateDataReceiver()
        {
            _cancelFurtherDataReceived = false;
        }

        private static void Scanner_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_scannerReader == null) return;
            if (_cancelFurtherDataReceived) return;
            var dataReceived = _scannerReader.ReadExisting();
            if (string.IsNullOrEmpty(dataReceived)) return;

            //Para que se ejecute una sola vez
            _cancelFurtherDataReceived = true;
            ScannerDataReceived?.Invoke(dataReceived);


        }


    }
}
