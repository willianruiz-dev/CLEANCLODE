using Domain;
using Domain.Peripherals;
using Domain.UIServices;
using Presentation.UserControls.Flows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UI.Bases;

namespace Presentation.UserControls.Bases
{
    /// <summary>
    /// Lógica de interacción para WelcomeUC.xaml
    /// </summary>
    public partial class WelcomeUC : AppUserControl
    {
        private ImageSliderViewModel _sliderViewModel;
        private Task _testingConnection;

        public WelcomeUC()
        {
            InitializeComponent();
            Transaction.Reset();
            Init();
            this.Unloaded += OnUnLoaded;
        }
        private async void Init()
        {
#if NO_PERIPHERALS
#else
            //_testingConnection = InternetConnectionManager.StartTestingConnection();
            
            // Detener cualquier grabación activa cuando se muestra la pantalla de bienvenida
            await StopActiveRecording();
#endif
        }
        
        private async Task StopActiveRecording()
        {
            try
            {
                // Obtener la instancia de Transaction
                var ts = Transaction.Instance;
                
                // Verificar si hay un grabador activo
                if (ts.videoRecorder != null)
                {
                    EventLogger.SaveLog(EventType.Info, "Deteniendo grabación activa en WelcomeUC...");
                    
                    try
                    {
                        // Intentar detener la grabación
                        bool stopResult = await ts.videoRecorder.StopAsync();
                        EventLogger.SaveLog(EventType.Info, $"Resultado de detener grabación en WelcomeUC: {(stopResult ? "Exitoso" : "Fallido")}");
                        
                        if (!stopResult)
                        {
                            // Intentar nuevamente si falla
                            await Task.Delay(500);
                            stopResult = await ts.videoRecorder.StopAsync();
                            EventLogger.SaveLog(EventType.Info, $"Segundo intento de detener grabación en WelcomeUC: {(stopResult ? "Exitoso" : "Fallido")}");
                        }
                    }
                    catch (Exception ex)
                    {
                        EventLogger.SaveLog(EventType.Error, $"Error al detener grabación en WelcomeUC: {ex.Message}", ex);
                    }
                    finally
                    {
                        // Liberar recursos y limpiar referencia
                        try
                        {
                            ts.videoRecorder.Dispose();
                            ts.videoRecorder = null;
                        }
                        catch (Exception ex)
                        {
                            EventLogger.SaveLog(EventType.Error, $"Error al liberar recursos de grabación en WelcomeUC: {ex.Message}", ex);
                        }
                    }
                }
                else
                {
                    EventLogger.SaveLog(EventType.Info, "No hay grabación activa para detener en WelcomeUC.");
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Error general al intentar detener grabación en WelcomeUC: {ex.Message}", ex);
            }
        }

        private async void Continuar_Touch(object sender, EventArgs e)
        {
#if NO_PERIPHERALS
#else
            //InternetConnectionManager.StopVerifyConnection();
            
            try
            {
                await Task.Delay(50);
                //_testingConnection.Dispose();
            }
            catch { }
#endif

            Dispatcher.Invoke(() => GoTo(new FormUC()));
        }
        private async void OnUnLoaded(object sender, RoutedEventArgs e)
        {
            GC.Collect();
        }

       
    }
}
