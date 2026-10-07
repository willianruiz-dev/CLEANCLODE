using System.IO;
using System.Reflection;

namespace Domain.Variables
{
    public static class Messages
    {
        public static string LOGIN_IN { get { return "Iniciando Sesión..."; } }
        public static string VALIDATING_PAYPLUS { get { return "Validando estado del Pay+..."; } }
        public static string NO_SERVICE { get { return "El kiosco está temporalmente fuera de servicio."; } }
        public static string VALIDATING_INFO { get { return "Validando información. Por favor espere."; } }
        public static string INCOMPLETE_MONEY { get { return "No se entregó completamente el dinero"; } }
        public static string CANCEL_TRANSACTION { get { return "¿Está seguro que desea cancelar la transacción?"; } }
        public static string PERIPHERALS_FAILED_CONNECT { get { return "Falló la conexión con los periféricos"; } }
        public static string PERIPHERALS_FAILED_VALIDATE { get { return "¡No se lograron iniciar los dispositivos periféricos!"; } }
        public static string VALIDATING_PERIPHERALS { get { return "Validando estado dispositivos..."; } }
    }

    public static class AppInfo
    {
        public static string APP_NAME { get { return "Confa"; } }
        public static string APP_DIR { get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty; } }
    }
}
