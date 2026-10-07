using System.Configuration;

namespace Domain
{
    public static class AppConfig
    {
        public static string Get(string key)
        {
            try
            {
                string? value = string.Empty;
                AppSettingsReader reader = new AppSettingsReader();
                value = reader.GetValue(key, typeof(string)).ToString();
                if (value == null) return string.Empty;
                return value;
            }
            catch(InvalidOperationException ex)
            {
                EventLogger.SaveLog(EventType.Error, $"No se encuentra la clave: {key} en App.Config. {ex.Message}");
                return string.Empty;
            }
            catch(Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                return string.Empty;
            }
            
        }

    }
}
