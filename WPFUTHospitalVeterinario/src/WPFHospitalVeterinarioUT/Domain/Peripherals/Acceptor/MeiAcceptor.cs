using MPOST;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Peripherals
{
    public enum MeiMessages
    {
        MeiConnected,
        MeiBillEscrow,
        MeiBillStacked,
        MeiBillRejected,
        MeiCashBoxRemoved,
        MeiCashBoxAttached,
        MeiJamDetected,
        MeiJamResolved,
        MeiCheated,
        MeiDisconnected,
        MeiPauseDetected,
        MeiPauseResolved,
        MeiPowerUp,
        MeiPowerUpComplete,
        MeiBillInEscrowOnPowerUp,
        MeiStackerFull,
        MeiStackerFullResolved,
        MeiStallDetected,
        MeiStallResolved
    }

    public delegate void BillAcceptedHandler(decimal value);
    public delegate void MeiErrorHandler(Exception ex);
    public class MeiAcceptor
    {
        public event BillAcceptedHandler BillAccepted;
        public event MeiErrorHandler AcceptorError;

        private Acceptor _meiAcceptor;

        private bool _isConnected;
        public bool IsConnected
        {
            get
            {
                if (_meiAcceptor != null)
                    _isConnected = _meiAcceptor.Connected;
                else
                    _isConnected = false;
                return _isConnected;
            }
        }

        private bool _isOpenned;
        public bool IsOpenned
        {
            get
            {
                if (_meiAcceptor != null)
                    _isOpenned = _meiAcceptor.EnableAcceptance;
                else
                    _isOpenned = false;
                return _isOpenned;
            }
        }

        private bool _isAvailable;
        public bool IsAvailable
        {
            get
            {
                return _isAvailable;
            }
        }

        private bool _isStackerFull;
        public bool IsStackerFull
        {
            get
            {
                return _isStackerFull;
            }
        }

        private bool _isCashBoxRemoved;
        public bool IsCashBoxRetired
        {
            get
            {
                return _isCashBoxRemoved;
            }
        }


        public Dictionary<MeiMessages, string> MeiMessagesHomologate = new Dictionary<MeiMessages, string>
        {
            {MeiMessages.MeiConnected ,"Aceptador conectado" },
            {MeiMessages.MeiBillEscrow,"Billete ingresado" },
            {MeiMessages.MeiBillStacked,"Billete almacenado" },
            {MeiMessages.MeiBillRejected,"Billete rechazado" },
            {MeiMessages.MeiCashBoxRemoved,"El baúl del billetero aceptador está retirado" },
            {MeiMessages.MeiCashBoxAttached,"El baúl del billetero aceptador ha sido reincorporado" },
            {MeiMessages.MeiJamDetected,"Atasco detectado en el aceptador" },
            {MeiMessages.MeiJamResolved,"Atasco resuelto en el aceptador" },
            {MeiMessages.MeiCheated,"Engaño detectado en el aceptador" },
            {MeiMessages.MeiDisconnected,"Aceptador Desconectado" },
            {MeiMessages.MeiPauseDetected,"Aceptador Pausado" },
            {MeiMessages.MeiPauseResolved,"Aceptador Despausado" },
            {MeiMessages.MeiPowerUp,"Aceptador inicia encendido" },
            {MeiMessages.MeiPowerUpComplete,"Aceptador encendido completado" },
            {MeiMessages.MeiBillInEscrowOnPowerUp,"Billete en proceso de ingreso en encendido" },
            {MeiMessages.MeiStackerFull,"Baúl del aceptador Lleno" },
            {MeiMessages.MeiStackerFullResolved,"ya hay espacio disponible en el baúl" },
            {MeiMessages.MeiStallDetected,"Aceptación detenida" },
            {MeiMessages.MeiStallResolved,"Aceptación reactivada" }
        };

        public MeiAcceptor()
        {

            if (_meiAcceptor == null)
            {
                _meiAcceptor = new Acceptor();
            }

            _meiAcceptor.OnConnected += new ConnectedEventHandler(MeiConnected);
            _meiAcceptor.OnEscrow += new EscrowEventHandler(MeiBillEscrow);
            _meiAcceptor.OnStackedWithDocInfo += new StackedWithDocInfoEventHandler(MeiBillStacked);
            _meiAcceptor.OnRejected += new RejectedEventHandler(MeiBillRejected);
            _meiAcceptor.OnCashBoxRemoved += new CashBoxRemovedEventHandler(MeiCashBoxRemoved);
            _meiAcceptor.OnCashBoxAttached += new CashBoxAttachedEventHandler(MeiCashBoxAttached);
            _meiAcceptor.OnJamDetected += new JamDetectedEventHandler(MeiJamDetected);
            _meiAcceptor.OnJamCleared += new JamClearedEventHandler(MeiJamResolved);
            _meiAcceptor.OnCheated += new CheatedEventHandler(MeiCheated);
            _meiAcceptor.OnDisconnected += new DisconnectedEventHandler(MeiDisconnected);
            _meiAcceptor.OnPauseDetected += new PauseDetectedEventHandler(MeiPauseDetected);
            _meiAcceptor.OnPauseCleared += new PauseClearedEventHandler(MeiPauseResolved);
            _meiAcceptor.OnPowerUp += new PowerUpEventHandler(MeiPowerUp);
            _meiAcceptor.OnPowerUpComplete += new PowerUpCompleteEventHandler(MeiPowerUpComplete);
            _meiAcceptor.OnPUPEscrow += new PUPEscrowEventHandler(MeiBillInEscrowOnPowerUp);
            _meiAcceptor.OnStackerFull += new StackerFullEventHandler(MeiStackerFull);
            _meiAcceptor.OnStackerFullCleared += new StackerFullClearedEventHandler(MeiStackerFullResolved);
            _meiAcceptor.OnStallDetected += new StallDetectedEventHandler(MeiStallDetected);
            _meiAcceptor.OnStallCleared += new StallClearedEventHandler(MeiStallResolved);

        }

        /// <summary>
        /// La parada detectada se resolvió
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiStallResolved(object sender, EventArgs e)
        {
            _isAvailable = true;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiStallResolved]}");
        }

        /// <summary>
        /// Se detectó una parada en el billetero
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiStallDetected(object sender, EventArgs e)
        {
            _isAvailable = false;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiStallDetected]}");
        }

        /// <summary>
        /// Se retiraron billetes del baul luego de estar lleno
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiStackerFullResolved(object sender, EventArgs e)
        {
            _isAvailable = true;
            _isStackerFull = false;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiStackerFullResolved]}");
        }

        /// <summary>
        /// Se llenó el baul del billetero
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiStackerFull(object sender, EventArgs e)
        {
            //TODO: Funcionalidad o aviso en caso de que se llene el baúl del aceptador
            _isAvailable = false;
            _isStackerFull = true;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiStackerFull]}");
        }

        /// <summary>
        /// Billete en el scrow mientras se inicia el billetero
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiBillInEscrowOnPowerUp(object sender, EventArgs e)
        {
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiBillInEscrowOnPowerUp]}");
        }

        /// <summary>
        /// Billetero inicia
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiPowerUpComplete(object sender, EventArgs e)
        {
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiPowerUpComplete]}");
        }

        /// <summary>
        /// Iniciando el billetero
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiPowerUp(object sender, EventArgs e)
        {
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiPowerUp]}");
        }

        /// <summary>
        /// Billetero sale de modo pausa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiPauseResolved(object sender, EventArgs e)
        {
            _isAvailable = true;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiPauseResolved]}");
        }

        /// <summary>
        /// Billetero entra en modo pausa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiPauseDetected(object sender, EventArgs e)
        {
            _isAvailable = false;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiPauseDetected]}");
        }

        /// <summary>
        /// Billetero desconectado
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiDisconnected(object sender, EventArgs e)
        {
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiDisconnected]}");
        }

        /// <summary>
        /// Engaño detectado
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiCheated(object sender, EventArgs e)
        {
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiCheated]}");
        }

        /// <summary>
        /// Atasco resuelto
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiJamResolved(object sender, EventArgs e)
        {
            _isAvailable = true;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiJamResolved]}");
        }

        /// <summary>
        /// Atasco Detectado
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiJamDetected(object sender, EventArgs e)
        {
            _isAvailable = false;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiJamDetected]}");
        }

        /// <summary>
        /// Baul ingresado
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiCashBoxAttached(object sender, EventArgs e)
        {

            _isCashBoxRemoved = false;
            _isAvailable = !_isCashBoxRemoved;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiCashBoxAttached]}");
        }

        /// <summary>
        /// Baul retirado
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiCashBoxRemoved(object sender, EventArgs e)
        {
            _isCashBoxRemoved = true;
            _isAvailable = !_isCashBoxRemoved;
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiCashBoxRemoved]}");
        }

        /// <summary>
        /// Billete Rechazado
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiBillRejected(object sender, EventArgs e)
        {
            EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiBillRejected]}");
        }

        /// <summary>
        /// Billete Guardado en el baul
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiBillStacked(object sender, StackedEventArgs e)
        {
            try
            {
                var acep = (Acceptor)sender;

                if (acep.Bill == null)
                {
                    return; // Cuando hay un rechazo llega null
                }

                // CORRECCION VULNERABILIDAD: Validar que la denominacion sea valida
                // IMPORTANTE: Esta lista debe coincidir con las denominaciones que el aceptador MEI puede recibir
                // CONFIGURACION ACTUAL DE LA MAQUINA:
                // - Acepta billetes: 2000, 5000, 10000, 20000, 50000, 100000
                // - NO acepta billetes de 1000 (ya no existen en circulacion)
                // - NO acepta monedas
                // - Devuelve cambio: Billetes (2000, 10000) + Monedas (500, 100)
                int[] allowedDenominations = { 2000, 5000, 10000, 20000, 50000, 100000 };
                bool isValidDenomination = false;
                foreach (var denom in allowedDenominations)
                {
                    if (acep.Bill.Value == denom)
                    {
                        isValidDenomination = true;
                        break;
                    }
                }

                if (!isValidDenomination)
                {
                    EventLogger.SaveLog(EventType.Error, $"Aceptador: Billete de denominacion invalida o desconocida: {acep.Bill.Value}");
                    return;
                }

                EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiBillStacked]}", acep.Bill);

                // CORRECCION VULNERABILIDAD: Usar null-conditional operator para evitar crash
                BillAccepted?.Invoke(Convert.ToDecimal(acep.Bill.Value));
            }
            catch (Exception ex)
            {
                // CORRECCION VULNERABILIDAD: Verificar suscriptores antes de invocar
                AcceptorError?.Invoke(ex);
                EventLogger.SaveLog(EventType.P_Acceptor, $"Error Aceptador: Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Billete Ingresado (en escrow)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiBillEscrow(object sender, EventArgs e)
        {
            // CORRECCION VULNERABILIDAD: Implementar logging para auditoria
            try
            {
                EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiBillEscrow]}");
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Error en MeiBillEscrow: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Puerto abierto y listo para aceptar billetes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeiConnected(object sender, EventArgs e)
        {
            try
            {
                _meiAcceptor.AutoStack = true;
                EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: {MeiMessagesHomologate[MeiMessages.MeiConnected]}");
            }
            catch (Exception ex)
            {
                AcceptorError?.Invoke(ex);
                EventLogger.SaveLog(EventType.P_Acceptor, $"Error Aceptador: Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }

        #region Methods
        public async Task<bool> OpenAcceptor(string meiPort)
        {
            try
            {
                if (_meiAcceptor.Connected) throw new Exception("El puerto del aceptador está ocupado");

                _meiAcceptor.Open(meiPort);
                int tries = 3;
                while (tries > 0)
                {
                    tries--;
                    await Task.Delay(1000);
                    if (_meiAcceptor.Connected)
                    {
                        return true;
                    }



                }
                throw new Exception("El aceptador no pudo establecer la conexión");
            }
            catch (Exception ex)
            {
                AcceptorError?.Invoke(ex);
                EventLogger.SaveLog(EventType.P_Acceptor, $"Error Aceptador: Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                return false;
            }
        }

        public bool EnableAcceptance()
        {
            try
            {
                if (!_meiAcceptor.Connected) throw new Exception("Se ha perdido la conexión con el aceptador");

                _meiAcceptor.EnableAcceptance = true;

                EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: Se ha iniciado la aceptación de Billetes");
                return true;
            }
            catch (Exception ex)
            {
                AcceptorError?.Invoke(ex);
                EventLogger.SaveLog(EventType.P_Acceptor, $"Error Aceptador: Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
                return false;
            }
        }

        public void DisableAcceptance()
        {
            try
            {
                if (!_meiAcceptor.Connected) throw new Exception("Se ha perdido la conexión con el aceptador");

                _meiAcceptor.EnableAcceptance = false;
                EventLogger.SaveLog(EventType.P_Acceptor, $"Aceptador: Se ha detenido la aceptación de Billetes");
            }
            catch (Exception ex)
            {
                AcceptorError?.Invoke(ex);
                EventLogger.SaveLog(EventType.P_Acceptor, $"Error Aceptador: Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }

        public void CloseAcceptor()
        {
            try
            {
                if (!_meiAcceptor.Connected) return;

                _meiAcceptor.Close();

            }
            catch (Exception ex)
            {
                AcceptorError?.Invoke(ex);
                EventLogger.SaveLog(EventType.P_Acceptor, $"Error Aceptador: Ocurrió un error en tiempo de ejecución: {ex.Message}", ex);
            }
        }
        #endregion
    }
}
