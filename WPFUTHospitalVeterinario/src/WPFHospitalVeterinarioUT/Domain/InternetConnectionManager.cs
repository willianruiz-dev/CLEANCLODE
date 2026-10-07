using Domain.UIServices;
using Domain.Variables;
using System.Net.NetworkInformation;
using UI.Modals;

namespace Domain
{
    public static class InternetConnectionManager
    {
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        public static async Task<bool> IsConnected()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string host = "8.8.8.8";

                    Ping p = new Ping();

                    PingReply reply = p.Send(host, 3000);

                    if (reply.Status == IPStatus.Success)
                    {
                        return true;
                    }
                }
                catch { }

                return false;
            });
        }

        public static async Task StartTestingConnection()
        {
            var navigator = Navigator.Instance;
            ModalWindow? modal = null;
            _cts = new CancellationTokenSource();

            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(500);
                if (!await IsConnected())
                {
                    if (modal != null) continue;
                    modal = navigator.ShowLoadModal(Messages.NO_SERVICE+" Se ha perdido la conexión a internet");
                    continue;
                }
                
                if (modal == null) continue;
                modal.Close();
                modal = null;

            }
        }

        public static void StopVerifyConnection()
        {
            _cts.Cancel();
        }



    }
}
