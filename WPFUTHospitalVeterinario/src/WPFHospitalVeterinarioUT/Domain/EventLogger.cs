using Domain.UIServices;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Domain
{
    public static class EventLogger
    {
        /// <summary>
        /// Guarda un evento en el log
        /// </summary>
        public static void SaveLog(EventType type, string msg, object? obj = null,
            [CallerMemberName] string method = "", [CallerFilePath] string callerPath = "")
        {
            var _class = Path.GetFileNameWithoutExtension(callerPath);
            
            int idTransaction = 0;
            try
            {
                idTransaction = UIServices.Transaction.Instance.IdTransaccionApi;
            }
            catch { }
            
            var _event = new LogEvent
            {
                Date = DateTime.Now,
                Time = DateTime.Now.ToString("hh:mm:ss.fff tt"),
                IdTransaction = idTransaction,
                Type = type.ToString(),
                Class = $"{_class}",
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
            if (type.ToString().StartsWith("P"))
                folder = "Log_peripherals";
            else if (type.ToString().Contains("Integration"))
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
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                var fileName = "Log" + DateTime.Now.ToString("yyyy-MM-dd") + ".json";
                var filePath = Path.Combine(logDir, fileName);

                if (!File.Exists(filePath))
                {
                    var archivo = File.CreateText(filePath);
                    archivo.Close();
                }

                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine(json);
                }
                
                return filePath;
            }
            catch (Exception logException)
            {
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
