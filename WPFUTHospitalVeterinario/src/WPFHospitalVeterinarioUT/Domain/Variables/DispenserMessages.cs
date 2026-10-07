namespace Domain.Variables
{
    public static class DispenserMessages
    {
        public static string REJECT_BOX_OPEN { get { return "La caja de rechazo está mal colocada o mal puesta, Ingrese adecuadamente la caja de rechazo y asegurese de cerrarla correctamente con la llave."; } }
        public static string JAM { get { return "Hay un atasco en el dispensador o algo está obstaculizado el paso de billetes. Revise que no halla obstaculos o polvo excesivo en el recorreido de los billetes."; } }
        public static string CASSETTE_DISMOUNTED { get { return "El baúl {0} está desmontado o mal colocado, Ingrese adecuadamente el baúl"; } }
        public static string CASSETTE_DISCONNECTED { get { return "El baúl {0} está desconectado, Por favor comuníquese con servicio técnico de E-city."; } }
        public static string CASSETTE_BAD_LOAD { get { return "El baúl {0} está mal cargado, Asegurese de que no hay ningún billete mal posicionado a la salida del baúl"; } }
        public static string CIS_OPEN { get { return "El compartimiento de la parte posterior del dispensador está abierto, Por favor asegurese de que está bien cerrado."; } }
        public static string PHYSICAL_CONN_LOST { get { return "Se ha perdido la conexíón fisica con el puerto COM del dispensador, Por favor comuníquese con servicio técnico de E-city."; } }
        public static string COM_NOT_AVAILABLE { get { return "El puerto de COM del dispensador es incorrecto o está siendo ocupado por otra aplicación."; } }

    }
}
