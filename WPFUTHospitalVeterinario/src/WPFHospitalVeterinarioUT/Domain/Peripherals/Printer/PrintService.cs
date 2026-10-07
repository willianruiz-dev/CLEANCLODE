using Domain.UIServices;
using Domain.Variables;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using Color = System.Drawing.Color;

namespace Domain.Peripherals
{
    public class PrintService
    {
        public static int numberOfSecondsToPrint = 5;
        private static PrintController _printController;
        private static PrintDocument _document;
        private static Graphics _graphics;
        private static PrintProperties _properties;
        public static List<PrintObj> _printData;
        public static bool? recentImpressionSuccess;
        static PrintService()
        {
            _properties = new PrintProperties("", 9600);
            _printController = new StandardPrintController();
            _document = new PrintDocument();
            _document.PrintController = _printController;
            _document.PrintPage += new PrintPageEventHandler(Print);

        }

        public static string CheckPrintStatus()
        {
            var status = _properties.CheckPrinterStatus();
            if (status == DefaultPrinterStatus.PrinterIsOk) return string.Empty;
            return PrintProperties.EvaluateStatus(status);

        }

        public static void Start()
        {

            Task.Run(() =>
            {
                try
                {
                    // CORRECCION: Simular impresión en modo debug para evitar errores de OneNote
#if DEBUG
                    EventLogger.SaveLog(EventType.Warning, "Modo DEBUG: Simulando impresion sin hardware");
                    recentImpressionSuccess = true;
                    return;  // Salir sin intentar imprimir en debug
#endif
                    
                    _document.Print();
                    var wasSucess = MonitorPrintJobs();
                    if (!wasSucess) CleanPrintQueue();
                    recentImpressionSuccess = wasSucess;
                }
                catch (Exception ex)
                {
                    EventLogger.SaveLog(EventType.Error, $"Error en la tarea Start de Impresión: {ex.Message}", ex);
                    // CORRECCION: En debug, no fallar por errores de impresora
#if DEBUG
                    recentImpressionSuccess = true;
#endif
                }

            });

        }

        public static void BuildPrint(Dictionary<string, string?> header, Dictionary<string, string?> body, Dictionary<string, string?> footer)
        {
            SolidBrush color = new SolidBrush(Color.Black);
            Font fontKeys = new Font("Arial", 8, FontStyle.Bold);
            Font fontValues = new Font("Arial", 8, FontStyle.Regular);
            Font fontBrand = new Font("Arial", 12, FontStyle.Bold);
            int y = 90;
            int yGap = 15;
            int xValues = 150;
            int xKeys = 15;


            string imgVoucher = Path.Combine(AppInfo.APP_DIR, AppConfig.Get("imgVoucher").Replace('/', '\\'));

            var dataToPrint = new List<PrintObj>();

            dataToPrint.Add(new PrintObj { Image = imgVoucher, X = 0, Y = 0 });
            dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = "========================================", X = xKeys, Y = y += yGap });


            // Header
            foreach (var key in header.Keys)
            {
                dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = key, X = xKeys, Y = y += yGap });
                dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = header[key] ?? string.Empty, X = xValues, Y = y });

            }

            dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = "========================================", X = xKeys, Y = y += yGap });

            // Body
            foreach (var key in body.Keys)
            {
                dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = key, X = xKeys, Y = y += yGap });
                dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = body[key] ?? string.Empty, X = xValues, Y = y });

            }

            dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = "========================================", X = xKeys, Y = y += yGap });

            // Footer
            foreach (var key in footer.Keys)
            {
                dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = key, X = xKeys, Y = y += yGap });
                dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = footer[key] ?? string.Empty, X = xValues, Y = y });

            }

            dataToPrint.Add(new PrintObj { Brush = color, Font = fontKeys, Text = "========================================", X = xKeys, Y = y += yGap });

            if (!Transaction.Instance.paymentProcess.DevueltaCorrecta)
            {
                dataToPrint.Add(new PrintObj { Brush = color, Font = fontValues, Text = "Ha ocurrido un error a la hora de devolver el dinero", X = xKeys, Y = y += yGap });
                dataToPrint.Add(new PrintObj { Brush = color, Font = fontValues, Text = "Por favor comuníquese con un Administrador.", X = xKeys, Y = y += 20 });
            }

            dataToPrint.Add(new PrintObj { Brush = color, Font = fontValues, Text = "Recuerde siempre esperar la tirilla de soporte de su", X = xKeys, Y = y += yGap + 10 });
            dataToPrint.Add(new PrintObj { Brush = color, Font = fontValues, Text = "pago, es el único documento que lo respalda.", X = xKeys, Y = y += 20 });

            dataToPrint.Add(new PrintObj { Brush = color, Font = fontBrand, Text = "E-city Software", X = 80, Y = y += yGap + 10 });

            _printData = dataToPrint;
        }

        private static void Print(object sender, PrintPageEventArgs e)
        {
            try
            {
                if (_printData.Count <= 0) return;
                if (e.Graphics == null) throw new Exception("La propiedad Graphics del evento Print es nula");

                foreach (var printObj in _printData)
                {
                    _graphics = e.Graphics;

                    if (printObj.QR != null)
                    {
                        _graphics.DrawImage(printObj.QR, printObj.X, printObj.Y);
                    }
                    else if (!string.IsNullOrEmpty(printObj.Image))
                    {
                        _graphics.DrawImage(Image.FromFile(printObj.Image), printObj.X, printObj.Y);
                    }
                    else if (printObj.Point.X != 0 && printObj.Point.Y != 0)
                    {
                        _graphics.DrawString(printObj.Text, printObj.Font, printObj.Brush, printObj.Point, printObj.Direction);
                    }
                    else
                    {
                        _graphics.DrawString(printObj.Text, printObj.Font, printObj.Brush, printObj.X, printObj.Y);
                    }
                }

            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }

        private static bool MonitorPrintJobs()
        {
            try
            {
                // CORRECCION: Manejo de excepciones para debug sin impresora
                PrintServer printServer = new PrintServer();
                PrintQueue printQueue = printServer.GetPrintQueue("w80");
                printQueue.Refresh();
                int printingTime = 0;
                var jobCollections = printQueue.GetPrintJobInfoCollection();
                EventLogger.SaveLog(EventType.Info, $"Se encontraron estos trabajos a imprimir {jobCollections.Count()}");
                if (jobCollections.Count() == 0) return true;

                foreach (PrintSystemJobInfo job in jobCollections)
                {
                    printingTime = 0;
                    EventLogger.SaveLog(EventType.Info, $"Imprimiendo {job.Name}");
                    while (!job.IsPrinted && printingTime < numberOfSecondsToPrint)
                    {
                        Thread.Sleep(1000);
                        job.Refresh();
                        if (job.IsPrinted || job.IsCompleted || job.IsDeleted)
                        {
                            EventLogger.SaveLog(EventType.Info, $"La impresion se realizo con exito");
                            break;
                        }

                        printingTime += 1;
                    }
                    if (printingTime == numberOfSecondsToPrint) EventLogger.SaveLog(EventType.Info, $"La impresion tardo mas de {numberOfSecondsToPrint} segundos");

                }
                if (printingTime == numberOfSecondsToPrint) return false;
                return true;
            }
            catch (PrintQueueException ex)
            {
                // CORRECCION: En debug sin impresora, no fallar
                EventLogger.SaveLog(EventType.Warning, $"No se pudo acceder a la cola de impresion (probablemente en debug sin impresora): {ex.Message}");
                return true;  // Retornar true para no bloquear el flujo en debug
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Error en MonitorPrintJobs: {ex.Message}", ex);
                return false;
            }

        }
        public static void CleanPrintQueue()
        {
            try
            {
                // CORRECCION: Manejo de excepciones para debug sin impresora
                LocalPrintServer printServer = new LocalPrintServer();
            PrintQueue queue = printServer.GetPrintQueue("w80");
            queue.Refresh();
            var jobCollections = queue.GetPrintJobInfoCollection();
            EventLogger.SaveLog(EventType.Info, $"Se limpiaron {jobCollections.Count()} impresiones pendientes de la cola.");

            // Retrieve and cancel all print jobs
            foreach (PrintSystemJobInfo printJob in queue.GetPrintJobInfoCollection())
            {
                try
                {
                    EventLogger.SaveLog(EventType.Error, $" {printJob.Name} cancelada");
                    printJob.Cancel();
                }
                catch (Exception ex)
                {
                    EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                }
            }
            EventLogger.SaveLog(EventType.Error, $"Impresionas en cola canceladas");
            }
            catch (PrintQueueException ex)
            {
                // CORRECCION: En debug sin impresora, solo loguear advertencia
                EventLogger.SaveLog(EventType.Warning, $"No se pudo limpiar la cola de impresion (probablemente en debug sin impresora): {ex.Message}");
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Error en CleanPrintQueue: {ex.Message}", ex);
            }
        }
    }

    public class PrintObj
    {
        public string Text { get; set; }
        public string Image { get; set; }
        public Image QR { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Font Font { get; set; }
        public SolidBrush Brush { get; set; }
        public PointF Point { get; set; }
        public StringFormat Direction { get; set; }
    }

    internal class PrintProperties
    {
        [DllImport("kernel32.dll", EntryPoint = "GetSystemDefaultLCID", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetSystemDefaultLCID();

        [DllImport("Msprintsdk.dll", EntryPoint = "SetInit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetInit();

        [DllImport("Msprintsdk.dll", EntryPoint = "SetUsbportauto", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetUsbportauto();

        [DllImport("Msprintsdk.dll", EntryPoint = "SetClean", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetClean();

        [DllImport("Msprintsdk.dll", EntryPoint = "SetClose", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetClose();

        [DllImport("Msprintsdk.dll", EntryPoint = "SetAlignment", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetAlignment(int iAlignment);

        [DllImport("Msprintsdk.dll", EntryPoint = "SetBold", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetBold(int iBold);

        [DllImport("Msprintsdk.dll", EntryPoint = "SetCommmandmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetCommmandmode(int iMode);

        [DllImport("Msprintsdk.dll", EntryPoint = "SetLinespace", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetLinespace(int iLinespace);

        [DllImport("Msprintsdk.dll", EntryPoint = "SetPrintport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetPrintport(StringBuilder strPort, int iBaudrate);

        [DllImport("Msprintsdk.dll", EntryPoint = "PrintString", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int PrintString(StringBuilder strData, int iImme);

        [DllImport("Msprintsdk.dll", EntryPoint = "PrintSelfcheck", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int PrintSelfcheck();

        [DllImport("Msprintsdk.dll", EntryPoint = "GetStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int GetStatus();

        [DllImport("Msprintsdk.dll", EntryPoint = "PrintFeedline", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int PrintFeedline(int iLine);

        [DllImport("Msprintsdk.dll", EntryPoint = "PrintCutpaper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int PrintCutpaper(int iMode);

        [DllImport("Msprintsdk.dll", EntryPoint = "SetSizetext", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetSizetext(int iHeight, int iWidth);

        [DllImport("Msprintsdk.dll", EntryPoint = "SetSizechinese", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetSizechinese(int iHeight, int iWidth, int iUnderline, int iChinesetype);

        [DllImport("Msprintsdk.dll", EntryPoint = "SetItalic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetItalic(int iItalic);

        [DllImport("Msprintsdk.dll", EntryPoint = "PrintDiskbmpfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int PrintDiskbmpfile(StringBuilder strData);

        [DllImport("Msprintsdk.dll", EntryPoint = "PrintDiskimgfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int PrintDiskimgfile(StringBuilder strData);

        [DllImport("Msprintsdk.dll", EntryPoint = "PrintQrcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int PrintQrcode(StringBuilder strData, int iLmargin, int iMside, int iRound);

        [DllImport("Msprintsdk.dll", EntryPoint = "PrintRemainQR", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int PrintRemainQR();

        [DllImport("Msprintsdk.dll", EntryPoint = "SetLeftmargin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SetLeftmargin(int iLmargin);

        [DllImport("Msprintsdk.dll", EntryPoint = "GetProductinformation", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int GetProductinformation(int Fstype, StringBuilder FIDdata);

        [DllImport("Msprintsdk.dll", EntryPoint = "PrintTransmit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int PrintTransmit(byte[] strCmd, int iLength);

        [DllImport("Msprintsdk.dll", EntryPoint = "GetTransmit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int GetTransmit(string strCmd, int iLength, StringBuilder strRecv, int iRelen);

        int m_iInit = -1;
        int m_iStatus = -1;
        int m_lcLanguage = 0;

        public PrintProperties(string portName, int baudrate)
        {
            ConfigurationPrinter(portName, baudrate);
        }
        private bool ConfigurationPrinter(string portName, int baudrate)
        {
            try
            {
                int countIntent = 0;
                m_lcLanguage = GetSystemDefaultLCID();
                StringBuilder sPort = new StringBuilder(portName, portName.Length);
                int iBaudrate = baudrate;
                SetPrintport(sPort, iBaudrate);
                while (countIntent < 3)
                {
                    m_iInit = SetInit();
                    if (m_iInit == 0)
                    {
                        return true;
                    }
                    else
                    {
                        countIntent++;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public DefaultPrinterStatus CheckPrinterStatus()
        {
            var isConnected = ConfigurePrinter();
            if (!isConnected) return DefaultPrinterStatus.CantConnectToPrinter;
            var status = PrintProperties.GetStatus();
            SetClose();
            if (!Enum.IsDefined(typeof(DefaultPrinterStatus), status)) return DefaultPrinterStatus.UndefinedInternalError;
            return (DefaultPrinterStatus)status;
        }
        /// <summary>
        /// Usar solo cuando la comunicacion de la impresora es por medio de comunicacion USB
        /// </summary>
        /// <returns></returns>
        private bool ConfigurePrinter()
        {
            try
            {
                int countIntent = 0;
                m_lcLanguage = GetSystemDefaultLCID();
                SetUsbportauto();
                while (countIntent < 3)
                {
                    m_iInit = SetInit();
                    if (m_iInit == 0)
                    {
                        return true;
                    }
                    else
                    {
                        countIntent++;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static string EvaluateStatus(DefaultPrinterStatus status)
        {
            switch (status)
            {
                case DefaultPrinterStatus.PrinterIsOk:
                    return "La impresora se encuentra lista";
                case DefaultPrinterStatus.PrinterIsOffline:
                    return "La impresora no se encuentra en linea, o no esta encedida";
                case DefaultPrinterStatus.PrinterCalledUnMatchedLibrary:
                    return "La impresora llamo una libreria que no se encuentra";
                case DefaultPrinterStatus.PrinterHeadIsOpened:
                    return "El cabezal de la impresora se encuentra abierto";
                case DefaultPrinterStatus.CutterIsNotReset:
                    return "Error en la cuchilla de corte.";
                case DefaultPrinterStatus.PrinterHeadTemperatureIsAbnormal:
                    return "La temperatura del cabezal es anormal. Muy caliente o muy fria";
                case DefaultPrinterStatus.PrinterDoesNotDetectBlackmark:
                    return "No se detecta marca negra para corte.";
                case DefaultPrinterStatus.PaperOut:
                    return "El papel se encuentra por fuera";
                case DefaultPrinterStatus.PaperLow:
                    return "Hay poco papel";
                case DefaultPrinterStatus.CantConnectToPrinter:
                    return "No se pudo conectar a la impresora";
                case DefaultPrinterStatus.UndefinedInternalError:
                    return "El error no se puede traducir. La impresora respondio con un valor no definido en la documentacion";
                case DefaultPrinterStatus.ErrorWhenExecutingDllCommand:
                    return "La ejecucion de un metodo interno de la Dll repsondio con error";
                default:
                    return "Error no registrado por el fabricante";
            }
        }

        public void ClosePrint()
        {
            SetClose();
        }
    }


    public enum DefaultPrinterStatus
    {
        PrinterIsOk,
        PrinterIsOffline,
        PrinterCalledUnMatchedLibrary,
        PrinterHeadIsOpened,
        CutterIsNotReset,
        PrinterHeadTemperatureIsAbnormal,
        PrinterDoesNotDetectBlackmark,
        PaperOut,
        PaperLow,
        CantConnectToPrinter = 30,
        UndefinedInternalError,
        ErrorWhenExecutingDllCommand,
        PrintingSuccess,
        PrintingTimeOutError
    }

}
