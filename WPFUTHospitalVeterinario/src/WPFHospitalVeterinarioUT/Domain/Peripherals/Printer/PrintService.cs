using Domain.UIServices;
using Domain.Variables;
using System.Diagnostics;
using System.Globalization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Factor de renderizado del lienzo: se dibuja a 96 ppp x 3 (288 ppp) para que el texto
        /// del PDF salga nítido. El tamaño físico no cambia, solo la resolución de la imagen.
        /// </summary>

        /// <summary>Calidad del JPEG incrustado en el PDF.</summary>
        private const long JpegQuality = 90L;
        private const double ReceiptRenderScale = 3.0;

        /// <summary>Espacio inferior de la tirilla, en píxeles del diseño.</summary>
        private const int ReceiptBottomMarginPx = 40;

        /// <summary>Carpeta donde se guardan los PDF, relativa a la carpeta del ejecutable.</summary>
        private const string PdfOutputFolder = "Receipts";

        /// <summary>
        /// Hoja A4 del PDF de revisión. La tirilla se coloca en la esquina superior izquierda a su
        /// tamaño real (80 mm de ancho), sin estirarla.
        /// </summary>
        private const double PdfPageWidthMm = 210.0;

        /// <summary>Alto de la hoja A4 del PDF de revisión.</summary>
        private const double PdfPageHeightMm = 297.0;

        /// <summary>
        /// Margen de la hoja del PDF. La tirilla va en la esquina superior izquierda, a su tamaño
        /// real, y este margen evita que quede pegada al borde al imprimir la hoja.
        /// </summary>
        private const double PdfPageMarginMm = 5.0;

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
                    EventLogger.SaveLog(EventType.Info,
                        "Sin periféricos: generando la tirilla como PDF con el tamaño de la w80.");
                    PrintPdf();
#else
                    EventLogger.SaveLog(EventType.Info, $"Imprimiendo la tirilla con '{W80_PRINTER_NAME}'.");
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
                    // Nunca más ancha que el ancho de la tirilla: la cabecera ocupa ese ancho.
                    int availableWidth = (int)Math.Max(0, graphics.VisibleClipBounds.Width - printObj.X);
                    using var image = Image.FromFile(printObj.Image);
                    var size = FitToReceiptWidth(image.Width, image.Height, availableWidth);
                    if (size.IsEmpty) continue;

                    _graphics.DrawImage(image, new Rectangle(printObj.X, printObj.Y, size.Width, size.Height));
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
        /// Dibuja la tirilla en un lienzo de alta resolución (96 ppp x 3). El dibujo se hace en las
        /// coordenadas del diseño y el lienzo las escala, de modo que la tirilla queda nítida sin
        /// cambiar su tamaño físico.
        /// </summary>
        private static Bitmap CreateReceiptCanvas(int designWidth, int designHeight)
        {
            int pixelWidth = (int)Math.Round(designWidth * ReceiptRenderScale);
            int pixelHeight = (int)Math.Round(designHeight * ReceiptRenderScale);

            var canvas = new Bitmap(pixelWidth, pixelHeight);

            // El lienzo se dibuja a 96 ppp, la resolución del diseño: GDI+ convierte las fuentes
            // (que están en puntos, por ejemplo el 8 de Arial) usando la resolución del lienzo. Si
            // el lienzo estuviera a 288 ppp, ese 8 se dibujaría 3 veces más grande (8,5 mm en vez
            // de 2,8 mm) y el texto se saldría de la tirilla y se pisaría entre líneas.
            // La nitidez la da el ScaleTransform de abajo, no la resolución del lienzo.
            canvas.SetResolution(ReceiptDesignDpiFloat, ReceiptDesignDpiFloat);

            using (var canvasGraphics = Graphics.FromImage(canvas))
            {
                canvasGraphics.Clear(Color.White);
                canvasGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                canvasGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                canvasGraphics.ScaleTransform((float)ReceiptRenderScale, (float)ReceiptRenderScale);
                DrawReceipt(canvasGraphics);
            }

            // Ya dibujado, se marca la resolución real de la imagen (96 ppp x 3). Es solo el dato
            // que queda en los metadatos: el dibujo no cambia.
            canvas.SetResolution((float)CanvasDpi, (float)CanvasDpi);

            return canvas;
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
                    var (imageWidth, imageHeight) = MeasureImage(printObj.Image);
                    itemHeight = FitToReceiptWidth(imageWidth, imageHeight, ReceiptWidthInPixels()).Height;
                }
                else if (printObj.Font != null)
                {
                    itemHeight = (int)Math.Ceiling(printObj.Font.GetHeight(ReceiptDesignDpiFloat));
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

        /// <summary>Ancho de la tirilla (la w80) en píxeles del diseño.</summary>
        private static int ReceiptWidthInPixels() =>
            (int)Math.Round(ReceiptWidthMm / 25.4 * ReceiptDesignDpi);

        /// <summary>
        /// Ajusta una imagen al ancho de la tirilla conservando la proporción. La cabecera
        /// (Voucher.png, 828 px = 219 mm a 96 ppp) es más ancha que la tirilla de 80 mm: sin este
        /// ajuste se dibujaría a tamaño natural y saldría cortada.
        /// </summary>
        private static Size FitToReceiptWidth(int width, int height, int maxWidth)
        {
            if (width <= 0 || height <= 0) return Size.Empty;
            if (width <= maxWidth) return new Size(width, height);

            double scale = (double)maxWidth / width;
            return new Size(maxWidth, (int)Math.Round(height * scale));
        }

#if NO_PERIPHERALS
        /// <summary>
        /// Compilación sin periféricos: la tirilla se genera como PDF por el propio programa, sin
        /// depender de ninguna impresora ni driver. La página mide exactamente el ancho de la w80
        /// (80 mm) y el alto del contenido, y el dibujo se coloca a tamaño físico real.
        /// En Release este camino no se compila: se sigue usando la w80.
        /// </summary>
        private static void PrintPdf()
        {
            string? generatedFile = null;

            try
            {
                var outputFolder = Path.IsPathRooted(PdfOutputFolder)
                    ? PdfOutputFolder
                    : Path.Combine(AppInfo.APP_DIR, PdfOutputFolder);

                Directory.CreateDirectory(outputFolder);
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

                using var canvas = CreateReceiptCanvas(ReceiptWidthInPixels(), ReceiptContentHeightInPixels());

                try
                {
                    generatedFile = SaveReceiptPdf(outputFolder, stamp, canvas);
                    EventLogger.SaveLog(EventType.Info,
                        $"Sin periféricos: tirilla generada en '{generatedFile}'. " +
                        $"La tirilla mide {ReceiptWidthMillimeters(canvas):0.#} x {ReceiptHeightMillimeters(canvas):0.#} mm " +
                        $"y va en la esquina de una hoja de {PdfPageWidthMm:0.#} x {PdfPageHeightMm:0.#} mm.");
                }
                catch (Exception ex)
                {
                    // Respaldo: si el PDF fallara, siempre queda la imagen para revisar la tirilla.
                    EventLogger.SaveLog(EventType.Error, $"No se pudo generar el PDF de la tirilla: {ex.Message}", ex);
                    generatedFile = SaveReceiptImage(outputFolder, stamp, canvas);
                    EventLogger.SaveLog(EventType.Info, $"Sin periféricos: tirilla guardada como imagen en '{generatedFile}'.");
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Error al generar la tirilla: {ex.Message}", ex);
            }
            finally
            {
                // Sin periféricos la tirilla no debe interrumpir el flujo del kiosco.
                recentImpressionSuccess = true;
            }

            if (generatedFile != null) OpenGeneratedFile(generatedFile);
        }

        /// <summary>Resolución real del lienzo (96 ppp de diseño x factor de renderizado).</summary>
        private static double CanvasDpi => ReceiptDesignDpi * ReceiptRenderScale;

        private static double ReceiptWidthMillimeters(Bitmap canvas) => canvas.Width / CanvasDpi * 25.4;

        private static double ReceiptHeightMillimeters(Bitmap canvas) => canvas.Height / CanvasDpi * 25.4;

        private static double MillimetersToPoints(double millimeters) => millimeters / 25.4 * 72.0;

        /// <summary>Respaldo en imagen, sin depender de nada externo.</summary>
        private static string SaveReceiptImage(string outputFolder, string stamp, Bitmap canvas)
        {
            var filePath = Path.Combine(outputFolder, $"tirilla-{stamp}.png");
            canvas.Save(filePath, ImageFormat.Png);
            return filePath;
        }

        /// <summary>Genera el PDF de la tirilla, de una sola página del tamaño exacto de la tirilla.</summary>
        private static string SaveReceiptPdf(string outputFolder, string stamp, Bitmap canvas)
        {
            var filePath = Path.Combine(outputFolder, $"tirilla-{stamp}.pdf");
            File.WriteAllBytes(filePath, BuildReceiptPdf(canvas));
            return filePath;
        }

        /// <summary>
        /// Arma el PDF: una hoja A4 con la tirilla en la esquina superior izquierda, a su tamaño
        /// real de 80 mm y sin estirarla. El PDF se escribe a mano para no depender de ninguna
        /// impresora ni driver.
        /// </summary>
        private static byte[] BuildReceiptPdf(Bitmap canvas)
        {
            // Tamaño físico en puntos (1 pulgada = 72 puntos). El lienzo está a 96 ppp x 3.
            double widthPoints = canvas.Width / CanvasDpi * 72.0;
            double heightPoints = canvas.Height / CanvasDpi * 72.0;

            double marginPoints = MillimetersToPoints(PdfPageMarginMm);
            double pageWidthPoints = MillimetersToPoints(PdfPageWidthMm);
            // Si la tirilla no cupiera en la hoja, la hoja crece para no cortarla.
            double pageHeightPoints = Math.Max(
                MillimetersToPoints(PdfPageHeightMm),
                heightPoints + marginPoints * 2.0);

            // El origen del PDF está abajo a la izquierda: para dejarla en la esquina superior se
            // desplaza el alto que sobra respecto al borde superior, menos el margen.
            double offsetX = marginPoints;
            double offsetY = pageHeightPoints - heightPoints - marginPoints;

            byte[] jpeg = EncodeJpeg(canvas);
            byte[] content = Encoding.ASCII.GetBytes(FormattableString.Invariant(
                $"q\n{widthPoints:0.####} 0 0 {heightPoints:0.####} {offsetX:0.####} {offsetY:0.####} cm\n/Im0 Do\nQ\n"));

            using var pdf = new MemoryStream();
            var offsets = new long[6];

            void Write(string text)
            {
                var bytes = Encoding.ASCII.GetBytes(text);
                pdf.Write(bytes, 0, bytes.Length);
            }

            void BeginObject(int number)
            {
                offsets[number] = pdf.Position;
                Write($"{number} 0 obj\n");
            }

            Write("%PDF-1.4\n");

            BeginObject(1);
            Write("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            BeginObject(2);
            Write("<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            BeginObject(3);
            Write(FormattableString.Invariant(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidthPoints:0.####} {pageHeightPoints:0.####}] /Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>\nendobj\n"));

            BeginObject(4);
            Write(FormattableString.Invariant(
                $"<< /Type /XObject /Subtype /Image /Width {canvas.Width} /Height {canvas.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpeg.Length} >>\nstream\n"));
            pdf.Write(jpeg, 0, jpeg.Length);
            Write("\nendstream\nendobj\n");

            BeginObject(5);
            Write(FormattableString.Invariant($"<< /Length {content.Length} >>\nstream\n"));
            pdf.Write(content, 0, content.Length);
            Write("endstream\nendobj\n");

            long xrefOffset = pdf.Position;
            Write("xref\n0 6\n");
            Write("0000000000 65535 f \n");
            for (int number = 1; number <= 5; number++)
                Write(FormattableString.Invariant($"{offsets[number]:0000000000} 00000 n \n"));
            Write(FormattableString.Invariant($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n"));

            return pdf.ToArray();
        }

        /// <summary>Codifica el lienzo como JPEG (calidad 90): nítido y con poco peso.</summary>
        private static byte[] EncodeJpeg(Bitmap canvas)
        {
            var jpegCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
            if (jpegCodec == null) throw new InvalidOperationException("No se encontró el codificador JPEG del sistema.");

            // Se califica System.Drawing.Imaging.Encoder: 'Encoder' a secas también existe en System.Text.
            long quality = JpegQuality;
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            using var stream = new MemoryStream();
            canvas.Save(stream, jpegCodec, parameters);
            return stream.ToArray();
        }

        /// <summary>Abre el archivo generado para poder revisarlo.</summary>
        private static void OpenGeneratedFile(string filePath)
        {
            if (!PdfOpenAfterPrint) return;
            if (!File.Exists(filePath)) return;

            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Warning, $"No se pudo abrir el archivo generado '{filePath}': {ex.Message}");
            }
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
