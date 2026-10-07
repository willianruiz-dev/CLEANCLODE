using Domain;
using Domain.HantleDispenserAPI;
using Domain.Peripherals;
using Domain.UIServices;
using Domain.Variables;
using System;
using System.Windows;
using UI.Modals;
using Presentation.UserControls.Bases;
using Presentation.UserControls.Flows;

namespace UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Height = SystemParameters.PrimaryScreenHeight;
            this.Width = this.Height * 9 / 16;
            this.WindowVB.Height = SystemParameters.PrimaryScreenHeight;
            this.WindowVB.Width = this.Height * 9 / 16;

            // API Connection Test - No direct DB access
            // La aplicación ahora usa API en lugar de conexión directa a BD

            // Navigation creation
            Navigator navigatorSingleton = Navigator.Instance;
            navigatorSingleton.Init(this);
#if NO_PERIPHERALS
#else
            string arduinoPort = AppConfig.Get("arduinoPort");
            string dispenserDenominations = AppConfig.Get("dispenserDenominations");
            bool peripheralsConected = false;
            int retryCount = 0;
            
            while (!peripheralsConected)
            {
                try
                {
                    ArduinoController.Initialize(arduinoPort, dispenserDenominations);
                    peripheralsConected = true;
                    EventLogger.SaveLog(EventType.Info, $"Periféricos conectados correctamente. Puerto Arduino: {arduinoPort}");
                }
                catch (Exception ex)
                {
                    retryCount++;
                    string errorMessage = $"Ocurrió un error en tiempo de ejecución: {ex.Message}";
                    EventLogger.SaveLog(EventType.Error, errorMessage, ex);
                    
                    // Mensaje más detallado que incluye el número de intento y el puerto que está intentando usar
                    string userMessage = Messages.PERIPHERALS_FAILED_CONNECT + 
                        $"\n\nIntento #{retryCount}" +
                        $"\nPuerto Arduino configurado: {arduinoPort}" +
                        "\n\nVerifique que los dispositivos estén conectados correctamente y que los puertos COM configurados sean los correctos.";
                    
                    navigatorSingleton.ShowModal(userMessage, new InfoModal());
                }
            }
            // En la arquitectura actual el PC controla el CDMS directamente por dispenserPort.
            // Verify dispenser load
            var dispenserOk = false;
            int dispenserRetries = 0;
            
            while (!dispenserOk)
            {
                try
                {
                    var dispenserMessage = Dispenser.GetLoadMessage();
                    if (dispenserMessage == string.Empty)
                    {
                        dispenserOk = true;
                        continue;
                    }
                    
                    dispenserRetries++;
                    // Mensaje más detallado para el dispensador
                    string userMessage = dispenserMessage + 
                        $"\n\nIntento #{dispenserRetries}" +
                        "\n\nVerifique que el dispensador esté correctamente conectado y configurado.";
                    
                    navigatorSingleton.ShowModal(userMessage, new InfoModal());
                }
                catch (Exception ex)
                {
                    dispenserRetries++;
                    string errorMessage = $"Error al verificar el dispensador: {ex.Message}";
                    EventLogger.SaveLog(EventType.Error, errorMessage, ex);
                    
                    string userMessage = "Error al verificar el dispensador. " + 
                        $"\n\nIntento #{dispenserRetries}" +
                        $"\nError: {ex.Message}" +
                        "\n\nVerifique que el dispensador esté correctamente conectado y configurado.";
                    
                    navigatorSingleton.ShowModal(userMessage, new InfoModal());
                }
            }
#endif
            //Inicia flujo de la aplicacion
            navigatorSingleton.NavigateTo(new PublicityUC());

        }
    }
}
