using Domain.UIServices;
using Domain.Variables;
using System.Diagnostics;
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

        /// <summary>Nombre de la cola de la impresora térmica de producción.</summary>
        private const string W80_PRINTER_NAME = "w80";

        /// <summary>Ancho de la tirilla de la w80, en milímetros.</summary>
        private const double ReceiptWidthMm = 80.0;

        /// <summary>Resolución con la que están pensadas las coordenadas de la tirilla.</summary>
        private const double ReceiptDesignDpi = 96.0;

        /// <summary>La misma resolución, en float, para las API de GDI+ que exigen float.</summary>
        private const float ReceiptDesignDpiFloat = (float)ReceiptDesignDpi;

        /// <summary>Espacio inferior de la tirilla, en píxeles del diseño.</summary>
        private const int ReceiptBottomMarginPx = 40;

        /// <summary>Margen lateral de la tirilla, en píxeles del diseño.</summary>
        private const int ReceiptSideMarginPx = 10;

        /// <summary>
        /// Indica que la impresión en curso se está generando como PDF con la impresora de Windows
        /// (solo ocurre en la compilación con NO_PERIPHERALS).
        /// </summary>
        private static bool _printToPdfFile;

        /// <summary>Impresora de Windows usada para generar el PDF cuando se compila con NO_PERIPHERALS.</summary>
        private const string PdfPrinterName = "Microsoft Print to PDF";

        /// <summary>Carpeta donde se guardan los PDF, relativa a la carpeta del ejecutable.</summary>
        private const string PdfOutputFolder = "Receipts";

        /// <summary>Abre el PDF generado para poder revisarlo.</summary>
        private const bool PdfOpenAfterPrint = true;

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
#if NO_PERIPHERALS
                    // Compilación sin periféricos (Debug): la tirilla se genera como PDF con la
                    // impresora de Windows, conservando el tamaño de la w80, para poder revisarla
                    // sin hardware. En Release este camino no se compila y se usa la w80.
                    PrintPdf();
#else
                    _document.Print();
                    var wasSucess = MonitorPrintJobs();
                    if (!wasSucess) CleanPrintQueue();
                    recentImpressionSuccess = wasSucess;
#endif
                }
                catch (Exception ex)
                {
                    EventLogger.SaveLog(EventType.Error, $"Error en la tarea Start de Impresión: {ex.Message}", ex);
                    // Sin periféricos no hay impresora que reporte el resultado: no se interrumpe el flujo.
#if NO_PERIPHERALS
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

                if (_printToPdfFile)
                {
                    PrintReceiptToPdfPage(e);
                    return;
                }

                DrawReceipt(e.Graphics);
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }

        /// <summary>Dibuja la tirilla con las coordenadas originales del diseño.</summary>
        private static void DrawReceipt(Graphics graphics)
        {
            var printData = _printData;
            if (printData == null) return;

            foreach (var printObj in printData)
            {
                _graphics = graphics;

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

        /// <summary>
        /// Dibuja la tirilla en un lienzo de 96 ppp (la resolución con la que están pensadas sus
        /// coordenadas) y lo estira al ancho de la página. De ese modo el PDF conserva el ancho y
        /// la escala de la tirilla de la w80, sin importar la resolución de la impresora de Windows.
        /// </summary>
        private static void PrintReceiptToPdfPage(PrintPageEventArgs e)
        {
            int pageWidthHundredthsInch = e.PageSettings.PaperSize?.Width ?? MillimetersToHundredthsInch(ReceiptWidthMm);
            int canvasWidth = ReceiptCanvasWidthInPixels();
            int canvasHeight = ReceiptContentHeightInPixels();

            using var canvas = new Bitmap(canvasWidth, canvasHeight);
            canvas.SetResolution(ReceiptDesignDpiFloat, ReceiptDesignDpiFloat);
            using (var canvasGraphics = Graphics.FromImage(canvas))
            {
                canvasGraphics.Clear(Color.White);
                DrawReceipt(canvasGraphics);
            }

            // El lienzo ocupa exactamente el ancho de la página: nada se recorta y nada se deforma.
            float pageWidthPx = (float)(e.Graphics.DpiX * pageWidthHundredthsInch / 100.0);
            float scale = pageWidthPx / canvasWidth;
            e.Graphics.DrawImage(canvas, 0f, 0f, pageWidthPx, canvasHeight * scale);
        }

        /// <summary>
        /// Ancho del lienzo de la tirilla, en píxeles del diseño: el ancho de la w80 y, si el
        /// contenido fuera más ancho, lo que necesite el contenido para no perder nada.
        /// </summary>
        private static int ReceiptCanvasWidthInPixels()
        {
            int w80WidthPixels = (int)Math.Round(ResolveW80WidthInHundredthsInch() / 100.0 * ReceiptDesignDpi);
            return Math.Max(w80WidthPixels, MeasureReceiptContentWidth() + ReceiptSideMarginPx);
        }

        /// <summary>Ancho que ocupa el contenido de la tirilla, en píxeles del diseño.</summary>
        private static int MeasureReceiptContentWidth()
        {
            int maxWidth = 0;

            using var probe = new Bitmap(1, 1);
            probe.SetResolution(ReceiptDesignDpiFloat, ReceiptDesignDpiFloat);
            using var probeGraphics = Graphics.FromImage(probe);

            var printData = _printData;
            if (printData == null) return 0;

            foreach (var printObj in printData)
            {
                int itemWidth = 0;

                if (printObj.QR != null)
                {
                    itemWidth = printObj.QR.Width;
                }
                else if (!string.IsNullOrEmpty(printObj.Image))
                {
                    itemWidth = MeasureImage(printObj.Image).width;
                }
                else if (!string.IsNullOrEmpty(printObj.Text) && printObj.Font != null)
                {
                    itemWidth = (int)Math.Ceiling(probeGraphics.MeasureString(printObj.Text, printObj.Font).Width);
                }

                maxWidth = Math.Max(maxWidth, printObj.X + itemWidth);
            }

            return maxWidth;
        }

        /// <summary>Alto que ocupa el contenido de la tirilla, en píxeles del diseño.</summary>
        private static int ReceiptContentHeightInPixels()
        {
            int maxHeight = 0;

            var printData = _printData;
            if (printData == null) return ReceiptBottomMarginPx;

            foreach (var printObj in printData)
            {
                int itemHeight = 0;

                if (printObj.QR != null)
                {
                    itemHeight = printObj.QR.Height;
                }
                else if (!string.IsNullOrEmpty(printObj.Image))
                {
                    itemHeight = MeasureImage(printObj.Image).height;
                }
                else if (printObj.Font != null)
                {
                    itemHeight = (int)Math.Ceiling(printObj.Font.GetHeight(ReceiptDesignDpiFloat / 72f));
                }

                maxHeight = Math.Max(maxHeight, printObj.Y + itemHeight);
            }

            return maxHeight + ReceiptBottomMarginPx;
        }

        /// <summary>Devuelve el tamaño de la imagen de la tirilla sin dejarla abierta.</summary>
        private static (int width, int height) MeasureImage(string path)
        {
            try
            {
                using var image = Image.FromFile(path);
                return (image.Width, image.Height);
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Warning, $"No se pudo medir la imagen de la tirilla '{path}': {ex.Message}");
                return (0, 0);
            }
        }

        private static int MillimetersToHundredthsInch(double millimeters) =>
            (int)Math.Round(millimeters / 25.4 * 100.0);

        /// <summary>Ancho de la w80 tomado de su propia cola de impresión, si está instalada.</summary>
        private static int ResolveW80WidthInHundredthsInch()
        {
            try
            {
                using var w80 = new PrintDocument();
                w80.PrinterSettings.PrinterName = W80_PRINTER_NAME;

                if (w80.PrinterSettings.IsValid)
                {
                    var paperSize = w80.DefaultPageSettings.PaperSize;
                    if (paperSize != null && paperSize.Width > 0)
                        return paperSize.Width;
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Warning,
                    $"No se pudo leer el tamaño de la w80, se usa {ReceiptWidthMm} mm de ancho: {ex.Message}");
            }

            return MillimetersToHundredthsInch(ReceiptWidthMm);
        }

#if NO_PERIPHERALS
        /// <summary>
        /// Compilación sin periféricos: la tirilla se manda a la impresora de Windows
        /// (Microsoft Print to PDF) y se guarda como PDF, conservando el ancho y la escala de la
        /// tirilla de la w80. En Release este camino no se compila: se sigue usando la w80.
        /// </summary>
        private static void PrintPdf()
        {
            try
            {
                _document.PrinterSettings.PrinterName = PdfPrinterName;
                if (!_document.PrinterSettings.IsValid)
                {
                    EventLogger.SaveLog(EventType.Warning,
                        $"Sin periféricos: la impresora '{PdfPrinterName}' no está instalada, se omite la impresión de la tirilla.");
                    recentImpressionSuccess = true;
                    return;
                }

                var outputFolder = Path.IsPathRooted(PdfOutputFolder)
                    ? PdfOutputFolder
                    : Path.Combine(AppInfo.APP_DIR, PdfOutputFolder);

                Directory.CreateDirectory(outputFolder);
                var filePath = Path.Combine(outputFolder, $"tirilla-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

                var paperSize = BuildReceiptPaperSize();
                _document.DefaultPageSettings.PaperSize = paperSize;
                _document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                _document.PrinterSettings.PrintToFile = true;
                _document.PrinterSettings.PrintFileName = filePath;
                _printToPdfFile = true;

                _document.Print();

                EventLogger.SaveLog(EventType.Info,
                    $"Sin periféricos: tirilla generada en '{filePath}' con el tamaño de la w80 " +
                    $"({paperSize.Width / 100.0:0.##} x {paperSize.Height / 100.0:0.##} pulgadas).");

                OpenPdf(filePath);
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Error al generar la tirilla en PDF: {ex.Message}", ex);
            }
            finally
            {
                // Sin periféricos la tirilla no debe interrumpir el flujo del kiosco.
                recentImpressionSuccess = true;
            }
        }

        /// <summary>Abre el PDF generado para poder revisarlo.</summary>
        private static void OpenPdf(string filePath)
        {
            if (!PdfOpenAfterPrint) return;
            if (!File.Exists(filePath)) return;

            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Warning, $"No se pudo abrir el PDF generado: {ex.Message}");
            }
        }

        /// <summary>
        /// Tamaño de la tirilla: el ancho real de la w80 y el alto que ocupa el contenido, para
        /// que el PDF salga con la misma forma y la misma escala que la tirilla impresa.
        /// </summary>
        private static PaperSize BuildReceiptPaperSize()
        {
            // La página mide lo mismo que el lienzo para que la tirilla salga a escala real:
            // el ancho de la w80 y el alto que ocupa el contenido.
            int width = (int)Math.Round(ReceiptCanvasWidthInPixels() / ReceiptDesignDpi * 100.0);
            int height = (int)Math.Round(ReceiptContentHeightInPixels() / ReceiptDesignDpi * 100.0);

            return new PaperSize("Tirilla w80", width, height);
        }

#endif

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
                    EventLogger.SaveLog(EventType.Info, $"Impresión cancelada de la cola: {printJob.Name}");
                    printJob.Cancel();
                }
                catch (Exception ex)
                {
                    EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                }
            }
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
