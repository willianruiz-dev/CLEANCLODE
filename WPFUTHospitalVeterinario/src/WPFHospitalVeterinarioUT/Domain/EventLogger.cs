using Domain.UIServices;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace Domain
{
    public static class EventLogger
    {
        private static readonly object FileWriteLock = new();
        /// <summary>
        /// Guarda un evento en el log
        /// </summary>
        public static void SaveLog(EventType type, string msg, object? obj = null,
            [CallerMemberName] string method = "", [CallerFilePath] string callerPath = "")
        {
            var className = Path.GetFileNameWithoutExtension(callerPath);
            if (className.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                className = Path.GetFileNameWithoutExtension(className);

            var timestamp = DateTime.Now;
            
            int idTransaction = 0;
            try
            {
                idTransaction = UIServices.Transaction.Instance.IdTransaccionApi;
            }
            catch { }
            
            var _event = new LogEvent
            {
                Date = timestamp,
                Time = timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                IdTransaction = idTransaction,
                Type = type.ToString(),
                Class = className,
                Method = method,
                Message = msg,
            };
            
            // Si el objeto es una excepción, guardar su stack trace
            if (obj is Exception exception)
            {
                _event.StackTrace = exception.StackTrace;
                _event.ExceptionMessage = exception.Message;
                
                // Si hay una excepción interna, también guardarla
                if (exception.InnerException != null)
                {
                    _event.InnerExceptionMessage = exception.InnerException.Message;
                }
            }
            else if (obj != null)
            {
                try
                {
                    _event.Obj = obj;
                }
                catch
                {
                    _event.Obj = obj.ToString();
                }
            }
            
            string folder;
            if (type.ToString().StartsWith("P", StringComparison.Ordinal))
                folder = "Log_peripherals";
            else if (type == EventType.Integration ||
                     className.StartsWith("Hospital", StringComparison.OrdinalIgnoreCase))
                folder = "Log_integration";
            else
                folder = "Log_application";

            WriteFile(_event, folder);
        }

        /// <summary>
        /// Escribe el evento en el archivo de log
        /// </summary>
        private static string WriteFile(LogEvent evt, string folder)
        {
            try
            {
                var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", folder);
                var filePath = Path.Combine(logDir, $"Log{evt.Date:yyyy-MM-dd}.json");

                // SaveLog puede ejecutarse desde UI, cola API y periféricos al mismo tiempo.
                // Una única sección crítica evita líneas intercaladas y archivos bloqueados.
                lock (FileWriteLock)
                {
                    Directory.CreateDirectory(logDir);
                    File.AppendAllText(filePath, json + Environment.NewLine);
                }

                return filePath;
            }
            catch (Exception logException)
            {
                System.Diagnostics.Debug.WriteLine($"No fue posible escribir el log: {logException}");
                return string.Empty;
            }
        }
       
    }

    /// <summary>
    /// Representa un evento de log con información detallada
    /// </summary>
    public class LogEvent
    {
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public int IdTransaction { get; set; }
        public string Type { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public string Message { get; set; }
        public object? Obj { get; set; }
        public string StackTrace { get; set; }
        public string ExceptionMessage { get; set; }
        public string InnerExceptionMessage { get; set; }
    }


    public enum EventType
    {
        FatalError,
        Error,
        Warning,
        Info,
        P_Acceptor,
        P_Arduino,
        P_Dispenser,
        Integration,

    }


}
