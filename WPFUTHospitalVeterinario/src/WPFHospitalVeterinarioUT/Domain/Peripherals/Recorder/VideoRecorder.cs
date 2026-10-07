using Domain;
using Domain.UIServices;
using OpenCvSharp;
using System.IO;

namespace Domain.Peripherals.Recorder
{
    /// <summary>
    /// Grabador de video para transacciones del Hospital Veterinario UT
    /// Graba videos localmente con timestamp automático
    /// </summary>
    public class VideoRecorder : IDisposable
    {
        #region Campos privados
        private bool _isRecording;
        private bool _lastOperationFailed = false;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private DateTime _startTime;

        // Variables para grabación
        private VideoCapture? _capture;
        private VideoWriter? _videoWriter;
        private Task? _recordingTask;
        private string? _currentVideoPath;
        private int _frameCount = 0;

        // Referencia a la transacción para obtener el ID al finalizar
        private readonly Transaction _transaction;
        #endregion

        #region Constructor
        public VideoRecorder(Transaction transaction)
        {
            _transaction = transaction;
        }
        #endregion

        #region Métodos públicos
        /// <summary>
        /// Inicia la grabación de video
        /// </summary>
        /// <param name="source">Índice de la cámara (0 por defecto)</param>
        /// <returns>True si la grabación se inició correctamente</returns>
        public async Task<bool> StartAsync(int source = 0)
        {
            if (_isRecording || _lastOperationFailed)
            {
                EventLogger.SaveLog(EventType.Warning, $"[VideoRecorder] StartAsync bloqueado - _isRecording={_isRecording}, _lastOperationFailed={_lastOperationFailed}");
                return _isRecording;
            }

            await _semaphore.WaitAsync();

            try
            {
                if (_isRecording)
                {
                    return true;
                }

                _startTime = DateTime.Now;


                var timestamp = _startTime.ToString("HHmmss");
                var year = _startTime.Year.ToString();
                var month = _startTime.Month.ToString("00");
                var day = _startTime.Day.ToString("00");
                var hour = _startTime.Hour.ToString("00");

                var filename = $"Video_{timestamp}.mp4";
                _currentVideoPath = Path.Combine("Videos", year, month, day, hour, filename);



                var directory = Path.GetDirectoryName(_currentVideoPath);
                Directory.CreateDirectory(directory);

                // Iniciar grabación
                bool result = await StartRecordingAsync(source);

                if (result)
                {
                    _lastOperationFailed = false;
                    return true;
                }
                else
                {
                    _lastOperationFailed = true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"[VideoRecorder] Error en StartAsync: {ex.Message}", ex);
                _lastOperationFailed = true;
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Detiene la grabación de video
        /// </summary>
        /// <param name="source">Índice de la cámara (no utilizado, mantenido por compatibilidad)</param>
        /// <returns>True si la grabación se detuvo correctamente</returns>
        public async Task<bool> StopAsync(int source = 0)
        {
            if (!_isRecording)
            {
                return true;
            }

            await _semaphore.WaitAsync();

            try
            {
                if (!_isRecording)
                {
                    return true;
                }

                _isRecording = false;
                bool result = await StopRecordingAsync();

                return result;
            }
            catch (Exception ex)
            {
                _isRecording = false;
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        #endregion

        #region Métodos privados
        private async Task<bool> StartRecordingAsync(int source)
        {
            try
            {
                EventLogger.SaveLog(EventType.Info, $"[VideoRecorder] Iniciando grabación con cámara {source}");

                CleanupResources();


                _capture = new VideoCapture(source);
                if (!_capture.IsOpened())
                {
                    EventLogger.SaveLog(EventType.Error, $"[VideoRecorder] No se pudo abrir la cámara {source}. Verificar que la cámara esté conectada y no esté en uso por otra aplicación.");
                    throw new Exception($"No se pudo abrir la cámara {source}");
                }
                EventLogger.SaveLog(EventType.Info, $"[VideoRecorder] Cámara {source} abierta correctamente");

                int width = (int)_capture.Get(VideoCaptureProperties.FrameWidth);
                int height = (int)_capture.Get(VideoCaptureProperties.FrameHeight);
                double fps = _capture.Get(VideoCaptureProperties.Fps);


                if (width <= 0) width = 640;
                if (height <= 0) height = 480;
                if (fps <= 0) fps = 30;


                EventLogger.SaveLog(EventType.Info, $"[VideoRecorder] Configuración de cámara: {width}x{height} @ {fps}fps");

                _videoWriter = InitializeVideoWriter(width, height, fps);
                if (_videoWriter == null)
                {
                    EventLogger.SaveLog(EventType.Error, "[VideoRecorder] No se pudo inicializar el VideoWriter. Ningún codec disponible.");
                    throw new Exception("No se pudo inicializar el VideoWriter");
                }
                EventLogger.SaveLog(EventType.Info, $"[VideoRecorder] VideoWriter inicializado. Guardando en: {_currentVideoPath}");

                _isRecording = true;
                _frameCount = 0;


                _recordingTask = Task.Run(RecordingLoop);

                return true;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"[VideoRecorder] Error en StartRecordingAsync: {ex.Message}", ex);
                CleanupResources();
                return false;
            }
        }

        private VideoWriter? InitializeVideoWriter(int width, int height, double fps)
        {
            var codecs = new[]
            {
                VideoWriter.FourCC('X', 'V', 'I', 'D'), // XVID
                VideoWriter.FourCC('M', 'J', 'P', 'G'), // MJPEG
                VideoWriter.FourCC('m', 'p', '4', 'v'), // MP4V
                VideoWriter.FourCC('H', '2', '6', '4')  // H264
            };

            foreach (var codec in codecs)
            {
                try
                {
                    var writer = new VideoWriter(_currentVideoPath, codec, fps, new Size(width, height));
                    if (writer.IsOpened())
                    {
                        return writer;
                    }
                    writer?.Dispose();
                }
                catch
                {

                }
            }
            return null;
        }

        private async Task<bool> StopRecordingAsync()
        {
            try
            {
                if (_recordingTask != null && !_recordingTask.IsCompleted)
                {
                    await Task.WhenAny(_recordingTask, Task.Delay(3000));
                }

                await Task.Delay(500);

                CleanupResources();

                if (!string.IsNullOrEmpty(_currentVideoPath) && File.Exists(_currentVideoPath))
                {
                    // Renombrar el archivo con el ID de la transacción si está disponible
                    string finalPath = RenameVideoWithTransactionId();

                    var fileInfo = new FileInfo(finalPath);
                    var duration = DateTime.Now - _startTime;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                CleanupResources();
                return false;
            }
        }

        /// <summary>
        /// Renombra el archivo de video incluyendo el ID de la transacción y documento
        /// </summary>
        private string RenameVideoWithTransactionId()
        {
            try
            {
                EventLogger.SaveLog(EventType.Info, $"[VideoRecorder] Iniciando RenameVideoWithTransactionId");

                if (string.IsNullOrEmpty(_currentVideoPath) || !File.Exists(_currentVideoPath))
                {
                    EventLogger.SaveLog(EventType.Warning, $"[VideoRecorder] No se puede renombrar: archivo no existe o path vacío. Path: {_currentVideoPath}");
                    return _currentVideoPath ?? string.Empty;
                }

                // Usar Transaction.Instance directamente para obtener los datos más actualizados
                var ts = Transaction.Instance;
                int idTransaccion = ts?.IdTransaccionApi ?? 0;


                EventLogger.SaveLog(EventType.Info, $"[VideoRecorder] Datos para renombrar - IdTransaccion: {idTransaccion}");

                if (idTransaccion == 0)
                {
                    // Si no hay ID de transacción, mantener el nombre original
                    EventLogger.SaveLog(EventType.Warning, $"[VideoRecorder] IdTransaccion es 0, manteniendo nombre original: {_currentVideoPath}");
                    return _currentVideoPath;
                }

                // Construir nuevo nombre: solo IdTransaccion.mp4
                var directory = Path.GetDirectoryName(_currentVideoPath);
                var newFilename = $"{idTransaccion}.mp4";
                var newPath = Path.Combine(directory ?? "", newFilename);

                EventLogger.SaveLog(EventType.Info, $"[VideoRecorder] Renombrando video de '{_currentVideoPath}' a '{newPath}'");

                // Renombrar el archivo
                if (_currentVideoPath != newPath)
                {
                    File.Move(_currentVideoPath, newPath);
                    _currentVideoPath = newPath;
                    EventLogger.SaveLog(EventType.Info, $"[VideoRecorder] Video renombrado exitosamente: {newPath}");
                }

                return newPath;
            }
            catch (Exception ex)
            {
                // Si falla el renombrado, mantener el archivo original
                EventLogger.SaveLog(EventType.Error, $"[VideoRecorder] Error al renombrar video: {ex.Message}", ex);
                return _currentVideoPath ?? string.Empty;
            }
        }

        private void RecordingLoop()
        {
            try
            {
                using var frame = new Mat();
                int errorCount = 0;
                const int maxErrors = 10;
                const int frameInterval = 30;

                while (_isRecording && errorCount < maxErrors)
                {
                    try
                    {
                        if (_capture?.Read(frame) == true && !frame.Empty())
                        {
                            _videoWriter?.Write(frame);
                            _frameCount++;


                            errorCount = 0;
                        }
                        else
                        {
                            errorCount++;
                        }

                        Thread.Sleep(33);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        Thread.Sleep(100);
                    }
                }

            }
            catch (Exception ex)
            {
            }
        }

        private void CleanupResources()
        {
            try
            {
                _videoWriter?.Release();
                _videoWriter?.Dispose();
                _videoWriter = null;

                _capture?.Release();
                _capture?.Dispose();
                _capture = null;

                _recordingTask = null;
            }
            catch (Exception ex)
            {
            }
        }
        #endregion

        #region Propiedades públicas
        /// <summary>
        /// Indica si la grabación está activa
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// Indica si la cámara está disponible
        /// </summary>
        public bool IsCameraAvailable => !_lastOperationFailed;
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_isRecording)
            {
                try
                {
                    StopAsync().Wait(3000);
                }
                catch (Exception ex)
                {
                    _isRecording = false;
                }
            }

            CleanupResources();
        }
        #endregion
    }
}
