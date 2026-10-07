using Domain.UIServices;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;

namespace Domain.Peripherals.Recorder
{
    /// <summary>
    /// Graba el video de una transacción y administra de forma coordinada
    /// la cámara, el escritor y el hilo de captura.
    /// </summary>
    public sealed class VideoRecorder : IDisposable
    {
        private const int MaxConsecutiveReadErrors = 10;
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

        private readonly Transaction _transaction;
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

        private VideoCapture? _capture;
        private VideoWriter? _videoWriter;
        private CancellationTokenSource? _recordingCancellation;
        private Task? _recordingTask;
        private string? _currentVideoPath;
        private DateTime _startTime;
        private volatile bool _isRecording;
        private bool _disposed;
        private int _frameCount;

        // Control de velocidad de reproducción: la cámara no siempre entrega los cuadros por
        // segundo que declara, así que al cerrar se compara lo realmente capturado con el
        // tiempo transcurrido y, si no coinciden, se corrige la velocidad del archivo.
        private const double PlaybackSpeedTolerance = 0.05;
        private readonly Stopwatch _recordingClock = new();
        private int _workingCodec;
        private double _writerFps;
        private double _firstFrameSeconds = -1;
        private double _lastFrameSeconds = -1;

        public VideoRecorder(Transaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public bool IsRecording => _isRecording;
        public bool IsCameraAvailable { get; private set; } = true;

        public async Task<bool> StartAsync(int source = 0)
        {
            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                if (_isRecording)
                    return true;

                CleanupResources();
                PrepareOutputPath();

                var initialized = await Task.Run(() => InitializeCapture(source)).ConfigureAwait(false);
                if (!initialized)
                {
                    IsCameraAvailable = false;
                    CleanupResources();
                    return false;
                }

                IsCameraAvailable = true;
                _frameCount = 0;
                _firstFrameSeconds = -1;
                _lastFrameSeconds = -1;
                _recordingClock.Restart();
                _recordingCancellation = new CancellationTokenSource();
                _isRecording = true;
                _recordingTask = Task.Run(() => RecordingLoop(_recordingCancellation.Token));

                EventLogger.SaveLog(EventType.Info,
                    $"Grabación iniciada con cámara {source}. Archivo: {_currentVideoPath}");
                return true;
            }
            catch (Exception ex)
            {
                _isRecording = false;
                IsCameraAvailable = false;
                CleanupResources();
                EventLogger.SaveLog(EventType.Error, "No fue posible iniciar la grabación de video.", ex);
                return false;
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        public async Task<bool> StopAsync(int source = 0)
        {
            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_recordingTask == null && _capture == null && _videoWriter == null)
                    return true;

                _isRecording = false;
                _recordingCancellation?.Cancel();

                if (_recordingTask != null)
                {
                    var completedTask = await Task.WhenAny(
                        _recordingTask,
                        Task.Delay(StopTimeout)).ConfigureAwait(false);

                    if (completedTask != _recordingTask)
                    {
                        EventLogger.SaveLog(EventType.Error,
                            "La captura de video no finalizó dentro del tiempo máximo; se conservaron los recursos para evitar acceso nativo concurrente.");
                        return false;
                    }

                    // Propaga y registra cualquier error inesperado del worker.
                    await _recordingTask.ConfigureAwait(false);
                }

                CleanupResources();

                // La cámara no siempre entrega la cantidad de cuadros por segundo que declara.
                // Si el archivo quedó declarado a más fps de los realmente capturados, el video
                // se reproduciría acelerado; aquí se ajusta a la velocidad real medida.
                var effectiveFps = await Task.Run(CorrectPlaybackSpeed).ConfigureAwait(false);

                var finalPath = FinalizeVideoName();
                var saved = !string.IsNullOrWhiteSpace(finalPath) && File.Exists(finalPath);
                var duration = effectiveFps > 0 ? _frameCount / effectiveFps : 0;

                EventLogger.SaveLog(
                    saved ? EventType.Info : EventType.Warning,
                    saved
                        ? $"Grabación finalizada. Frames: {_frameCount}. Velocidad: {effectiveFps:0.##} fps. Duración: {duration:0.#} s. Archivo: {finalPath}"
                        : "La grabación finalizó sin generar un archivo de video.");

                return saved;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "No fue posible detener la grabación de video.", ex);
                return false;
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        private void PrepareOutputPath()
        {
            _startTime = DateTime.Now;
            var directory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Videos",
                _startTime.ToString("yyyy"),
                _startTime.ToString("MM"),
                _startTime.ToString("dd"),
                _startTime.ToString("HH"));

            Directory.CreateDirectory(directory);
            _currentVideoPath = Path.Combine(directory, $"Video_{_startTime:HHmmss_fff}.mp4");
        }

        private bool InitializeCapture(int source)
        {
            // En Windows OpenCV abre las cámaras con MSMF por defecto y con algunas cámaras USB
            // ese backend entrega muy pocos cuadros por segundo (medido: ~5 fps). Se intenta
            // primero DirectShow, que suele ser más constante; si no abre, se usa el backend por
            // defecto, para no quedarse sin grabación por preferir un backend.
            _capture = new VideoCapture(source, VideoCaptureAPIs.DSHOW);
            var backend = "DirectShow";

            if (!_capture.IsOpened())
            {
                _capture.Dispose();
                _capture = new VideoCapture(source);
                backend = "por defecto";
            }

            if (!_capture.IsOpened())
            {
                EventLogger.SaveLog(EventType.Error,
                    $"No se pudo abrir la cámara {source}; puede estar desconectada o en uso.");
                return false;
            }

            // Que el búfer conserve solo el último cuadro: así lo que se escribe es siempre lo
            // más reciente y no se arrastra atraso cuando la cámara va lenta.
            try
            {
                _capture.Set(VideoCaptureProperties.BufferSize, 1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"No se pudo ajustar el búfer de la cámara: {ex.Message}");
            }

            var width = Math.Max(640, (int)_capture.Get(VideoCaptureProperties.FrameWidth));
            var height = Math.Max(480, (int)_capture.Get(VideoCaptureProperties.FrameHeight));
            var reportedFps = _capture.Get(VideoCaptureProperties.Fps);
            var fps = reportedFps;
            if (fps is <= 0 or > 120)
                fps = 30;

            EventLogger.SaveLog(EventType.Info,
                $"Cámara {source} lista (backend {backend}): {width}x{height}. Fps reportados: {reportedFps:0.##}. Fps con que inicia el archivo: {fps:0.##}.");

            _videoWriter = InitializeVideoWriter(width, height, fps);
            return _videoWriter != null;
        }

        private VideoWriter? InitializeVideoWriter(int width, int height, double fps)
        {
            var codecs = new[]
            {
                VideoWriter.FourCC('m', 'p', '4', 'v'),
                VideoWriter.FourCC('X', 'V', 'I', 'D'),
                VideoWriter.FourCC('M', 'J', 'P', 'G'),
                VideoWriter.FourCC('H', '2', '6', '4')
            };

            foreach (var codec in codecs)
            {
                VideoWriter? writer = null;
                try
                {
                    writer = new VideoWriter(_currentVideoPath, codec, fps, new Size(width, height));
                    if (writer.IsOpened())
                    {
                        _workingCodec = codec;
                        _writerFps = fps;
                        return writer;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Codec de video no disponible: {ex.Message}");
                }

                writer?.Dispose();
            }

            return null;
        }

        private void RecordingLoop(CancellationToken cancellationToken)
        {
            using var frame = new Mat();
            var consecutiveErrors = 0;

            try
            {
                while (!cancellationToken.IsCancellationRequested &&
                       consecutiveErrors < MaxConsecutiveReadErrors)
                {
                    if (_capture?.Read(frame) == true && !frame.Empty())
                    {
                        _videoWriter?.Write(frame);
                        var seconds = _recordingClock.Elapsed.TotalSeconds;
                        if (_firstFrameSeconds < 0)
                            _firstFrameSeconds = seconds;
                        _lastFrameSeconds = seconds;
                        Interlocked.Increment(ref _frameCount);
                        consecutiveErrors = 0;
                    }
                    else
                    {
                        consecutiveErrors++;
                    }

                    if (cancellationToken.WaitHandle.WaitOne(33))
                        break;
                }

                if (consecutiveErrors >= MaxConsecutiveReadErrors)
                {
                    EventLogger.SaveLog(EventType.Error,
                        "La grabación se detuvo por errores consecutivos leyendo la cámara.");
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "Error en el hilo de captura de video.", ex);
            }
            finally
            {
                _isRecording = false;
            }
        }

        /// <summary>
        /// Ajusta la velocidad de reproducción del archivo a la velocidad real de captura.
        /// Devuelve la velocidad con la que queda el video.
        /// </summary>
        private double CorrectPlaybackSpeed()
        {
            var frames = _frameCount;
            var spanSeconds = _lastFrameSeconds - _firstFrameSeconds;

            if (frames < 2 || spanSeconds <= 0 || _writerFps <= 0)
                return _writerFps;

            var measuredFps = Math.Clamp((frames - 1) / spanSeconds, 1, 120);
            if (Math.Abs(measuredFps - _writerFps) / _writerFps <= PlaybackSpeedTolerance)
                return _writerFps;

            if (TryRewriteAtFps(measuredFps))
            {
                EventLogger.SaveLog(EventType.Info,
                    $"Velocidad del video corregida: {_writerFps:0.##} fps declarados -> {measuredFps:0.##} fps reales ({frames} frames en {spanSeconds:0.#} s).");
                return measuredFps;
            }

            EventLogger.SaveLog(EventType.Warning,
                $"No se pudo corregir la velocidad del video ({_writerFps:0.##} fps declarados, {measuredFps:0.##} fps reales); el archivo conserva la velocidad original.");

            return _writerFps;
        }

        /// <summary>
        /// Reescribe el archivo recién grabado a la velocidad indicada. Se trabaja sobre una
        /// copia temporal y solo se reemplaza el original si la reescritura termina bien.
        /// </summary>
        private bool TryRewriteAtFps(double fps)
        {
            var tempPath = _currentVideoPath + ".fixed.mp4";
            try
            {
                using var source = new VideoCapture(_currentVideoPath);
                if (!source.IsOpened())
                    return false;

                var width = (int)source.Get(VideoCaptureProperties.FrameWidth);
                var height = (int)source.Get(VideoCaptureProperties.FrameHeight);
                if (width <= 0 || height <= 0)
                    return false;

                var copiedFrames = 0;
                using (var target = new VideoWriter(tempPath, _workingCodec, fps, new Size(width, height)))
                {
                    if (!target.IsOpened())
                        return false;

                    using var frame = new Mat();
                    while (source.Read(frame) && !frame.Empty())
                    {
                        target.Write(frame);
                        copiedFrames++;
                    }

                    target.Release();
                }

                // Se libera el archivo de origen antes de reemplazar, para no dejarlo bloqueado.
                source.Release();

                if (copiedFrames == 0)
                    return false;

                if (File.Exists(_currentVideoPath))
                    File.Replace(tempPath, _currentVideoPath, null);
                else
                    File.Move(tempPath, _currentVideoPath);

                return true;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Warning, $"Error corrigiendo la velocidad del video: {ex.Message}");
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // Limpieza best-effort: si queda el temporal, no afecta la grabación.
                }
            }
        }

        private string FinalizeVideoName()
        {
            if (string.IsNullOrWhiteSpace(_currentVideoPath) || !File.Exists(_currentVideoPath))
                return string.Empty;

            var transactionId = _transaction.IdTransaccionApi;
            if (transactionId <= 0)
                return _currentVideoPath;

            var directory = Path.GetDirectoryName(_currentVideoPath) ?? string.Empty;
            var finalPath = Path.Combine(directory, $"{transactionId}.mp4");
            if (string.Equals(_currentVideoPath, finalPath, StringComparison.OrdinalIgnoreCase))
                return finalPath;

            if (File.Exists(finalPath))
            {
                EventLogger.SaveLog(EventType.Warning,
                    $"Ya existe un video para la transacción {transactionId}; se conserva el archivo temporal {_currentVideoPath}.");
                return _currentVideoPath;
            }

            File.Move(_currentVideoPath, finalPath);
            _currentVideoPath = finalPath;
            return finalPath;
        }

        private void CleanupResources()
        {
            _recordingCancellation?.Dispose();
            _recordingCancellation = null;
            _recordingTask = null;

            _videoWriter?.Release();
            _videoWriter?.Dispose();
            _videoWriter = null;

            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VideoRecorder));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            var stoppedSafely = false;
            try
            {
                stoppedSafely = StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "Error liberando el grabador de video.", ex);
            }

            _disposed = true;
            if (stoppedSafely)
            {
                CleanupResources();
                _lifecycleGate.Dispose();
            }
            else
            {
                EventLogger.SaveLog(EventType.Warning,
                    "El grabador no liberó recursos nativos porque el hilo de captura aún podía estar activo.");
            }
            GC.SuppressFinalize(this);
        }
    }
}
