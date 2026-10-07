using Domain.UIServices;
using Domain.Variables;
using System.Net.NetworkInformation;
using UI.Modals;

namespace Domain
{
    public static class InternetConnectionManager
    {
        private static readonly object CancellationLock = new();
        private static CancellationTokenSource? _cts;

        public static async Task<bool> IsConnected()
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 3000).ConfigureAwait(false);
                return reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static async Task StartTestingConnection()
        {
            CancellationToken token;
            lock (CancellationLock)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
                token = _cts.Token;
            }

            var navigator = Navigator.Instance;
            ModalWindow? modal = null;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), token);
                    var isConnected = await IsConnected();

                    if (!isConnected && modal == null)
                    {
                        modal = navigator.ShowLoadModal(Messages.NO_SERVICE + " Se ha perdido la conexión a internet");
                    }
                    else if (isConnected && modal != null)
                    {
                        modal.Close();
                        modal = null;
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Finalización normal solicitada por StopVerifyConnection.
            }
            finally
            {
                modal?.Close();
            }
        }

        public static void StopVerifyConnection()
        {
            lock (CancellationLock)
            {
                _cts?.Cancel();
            }
        }
    }
}
