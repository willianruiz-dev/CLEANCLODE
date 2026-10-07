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
                var finalPath = FinalizeVideoName();
                var saved = !string.IsNullOrWhiteSpace(finalPath) && File.Exists(finalPath);

                EventLogger.SaveLog(
                    saved ? EventType.Info : EventType.Warning,
                    saved
                        ? $"Grabación finalizada. Frames: {_frameCount}. Archivo: {finalPath}"
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
            _capture = new VideoCapture(source);
            if (!_capture.IsOpened())
            {
                EventLogger.SaveLog(EventType.Error,
                    $"No se pudo abrir la cámara {source}; puede estar desconectada o en uso.");
                return false;
            }

            var width = Math.Max(640, (int)_capture.Get(VideoCaptureProperties.FrameWidth));
            var height = Math.Max(480, (int)_capture.Get(VideoCaptureProperties.FrameHeight));
            var fps = _capture.Get(VideoCaptureProperties.Fps);
            if (fps is <= 0 or > 120)
                fps = 30;

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
                        return writer;
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
