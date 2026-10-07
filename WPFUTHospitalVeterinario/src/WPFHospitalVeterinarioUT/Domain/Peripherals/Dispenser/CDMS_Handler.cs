using System.Text;

namespace Domain.HantleDispenserAPI
{
    public class CDMS_Handler
    {
        private int port;
        public List<int> cassetesValues { get; }
        public CDMS_Handler(string portname, string denominations) //10000;2000
        {
            port = Convert.ToInt32(portname.ToUpper().Replace("COM", ""));

            cassetesValues = new List<int>();
            var denomsSplited = denominations.Split(';');
            foreach (var denom in denomsSplited)
            {
                cassetesValues.Add(Convert.ToInt32(denom));
            }

        }

        private string BytesToString(byte[] bytes, int length)
        {
            string result = string.Empty;
            for (int i = 0; i < length; i++)
            {

                result += (char)bytes[i];

            }
            return result;
        }

        private bool[] ByteToBoolArray(byte _byte)
        {
            var result = new bool[8];
            for (int i = 0; i < 8; i++)
            {
                result[i] = Convert.ToBoolean((_byte & (1 << i)));
            }
            return result;
        }

        public bool Connect()
        {

            int ret = CDMS_Api.ConnectCDMS(this.port);
            if (ret == 0)
                return true;
            return false;

        }

        public bool Disconnect()
        {
            int ret = CDMS_Api.DisConnectCDMS();
            if (ret == 0)
                return true;
            return false;
        }

        private CDMS_Response SetParameters()
        {
            try
            {
                Byte[] RspBuffer = new Byte[12000];
                int RspLength = 0;
                Array.Clear(RspBuffer, 0, RspBuffer.Length);

                //Default Value: 500
                int speed = 500;
                int frontstart = 12;
                int rearstart = 8; //40
                int frontend = 10;
                int rearend = 10;
                //

                int ret = CDMS_Api.SetParameterCDMS(speed, frontstart
                    , rearstart, frontend, rearend, RspBuffer, ref RspLength);

                String errorCode = Encoding.Default.GetString(RspBuffer, 0, 2);
                ErrorCDMS errorCodeInt;
                try
                {
                    errorCodeInt = (ErrorCDMS)Convert.ToInt32(errorCode);
                }
                catch (Exception ex)
                {
                    errorCodeInt = ErrorCDMS.Unknown;
                }

                var res = new CDMS_Response();

                res.ErrorCode = errorCodeInt;
                res.Stream = BytesToString(RspBuffer, RspLength);

                if (ret == 0)
                    res.isSuccess = true;
                else
                    res.isSuccess = false;

                return res;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Dispenser, $"FATAL: Ocurrió error de comunicación con el dispensador {ex.Message} ", ex);
                return new CDMS_Response
                {
                    isSuccess = false,
                    ErrorCode = ErrorCDMS.RuntimeError,
                    Stream = string.Empty
                };
            }

        }

        public CDMS_Response Initialize()
        {
            SetParameters();
            try
            {
                Byte[] RspBuffer = new Byte[12000];
                int RspLength = 0;
                Array.Clear(RspBuffer, 0, RspBuffer.Length);

                char OCRunreadCanCount = '0';
                char country = '1'; //Always 1 china
                char count = Convert.ToChar(cassetesValues.Count.ToString()); // Total number of cassetes
                char irCheck = '1'; // Always 1
                string indexData = string.Empty.PadLeft(cassetesValues.Count, '4'); //Always 44 or 444
                int ret = CDMS_Api.InitializeDispenserCDMS(OCRunreadCanCount, country, count
                    , irCheck, indexData, RspBuffer, ref RspLength);

                String errorCode = Encoding.Default.GetString(RspBuffer, 0, 2);
                ErrorCDMS errorCodeInt;
                try
                {
                    errorCodeInt = (ErrorCDMS)Convert.ToInt32(errorCode);
                }
                catch (Exception ex)
                {
                    errorCodeInt = ErrorCDMS.Unknown;
                }

                var res = new CDMS_Response();

                res.ErrorCode = errorCodeInt;
                res.Stream = BytesToString(RspBuffer, RspLength);

                if (ret == 0)
                    res.isSuccess = true;
                else
                    res.isSuccess = false;

                return res;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Dispenser, $"FATAL: Ocurrió error de comunicación con el dispensador {ex.Message} ", ex);
                return new CDMS_Response
                {
                    isSuccess = false,
                    ErrorCode = ErrorCDMS.RuntimeError,
                    Stream = string.Empty
                };
            }

        }


        // Quantities to dispense from each cassette in order of cassettes
        public CDMS_Response Dispense(int[] quantities)
        {
            try
            {
                Byte[] RspBuffer = new Byte[12000];
                int RspLength = 0;
                Array.Clear(RspBuffer, 0, RspBuffer.Length);

                int cassetteCountInt = 0;
                string dispenseData = string.Empty;
                for (int i = 0; i < quantities.Length; i++)
                {
                    if (quantities[i] == 0)
                    {
                        continue;
                    }
                    dispenseData += $"{i}{quantities[i].ToString().PadLeft(3, '0')}";
                    cassetteCountInt++;
                }

                char cassetteCount = Convert.ToChar(cassetteCountInt.ToString());
                int ret = CDMS_Api.DispenseCDMS(cassetteCount, dispenseData, RspBuffer, ref RspLength);

                String errorCode = Encoding.Default.GetString(RspBuffer, 0, 2);
                ErrorCDMS errorCodeInt;
                try
                {
                    errorCodeInt = (ErrorCDMS)Convert.ToInt32(errorCode);
                }
                catch (Exception ex)
                {
                    errorCodeInt = ErrorCDMS.Unknown;
                }

                var res = new CDMS_Response();

                res.ErrorCode = errorCodeInt;
                res.Stream = BytesToString(RspBuffer, RspLength);

                if (ret != 0 && !(ret == 9 && errorCode == "10"))
                    res.isSuccess = false;
                else
                    res.isSuccess = true;

                // Dispense Info
                res.DispenseData = new List<CDMS_DenomInfo>();

                for (int i = 0; i < quantities.Length; i++)
                {
                    CDMS_DenomInfo denomInfo = new();
                    denomInfo.Denomination = cassetesValues[i];
                    if (quantities[i] == 0)
                    {
                        denomInfo.RequestedQuantity = 0;
                        denomInfo.OutOfCassetteQuantity = 0;
                        denomInfo.DispensedQuantity = 0;
                        denomInfo.RejectQuantity = 0;
                        res.DispenseData.Add(denomInfo);
                        continue;
                    }

                    int offset = (33 * i);
                    int pos = 3;
                    denomInfo.RequestedQuantity = Convert.ToInt32(Encoding.Default.GetString(RspBuffer, pos + offset, 3));
                    pos += 3;
                    denomInfo.OutOfCassetteQuantity = Convert.ToInt32(Encoding.Default.GetString(RspBuffer, pos + offset, 3));
                    pos += 3;
                    denomInfo.DispensedQuantity = Convert.ToInt32(Encoding.Default.GetString(RspBuffer, pos + offset, 3));
                    pos += 3;
                    denomInfo.RejectQuantity = Convert.ToInt32(Encoding.Default.GetString(RspBuffer, pos + offset, 3));

                    res.DispenseData.Add(denomInfo);
                }

                return res;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Dispenser, $"FATAL: Ocurrió error de comunicación con el dispensador {ex.Message} ", ex);
                return new CDMS_Response
                {
                    isSuccess = false,
                    ErrorCode = ErrorCDMS.RuntimeError,
                    Stream = string.Empty
                };
            }

        }

        public CDMS_Response Eject()
        {
            try
            {
                Byte[] RspBuffer = new Byte[12000];
                int RspLength = 0;
                Array.Clear(RspBuffer, 0, RspBuffer.Length);
                char backFeedRetry = '1';
                int ret = CDMS_Api.EjectCDMS(backFeedRetry, RspBuffer, ref RspLength);
                String errorCode = Encoding.Default.GetString(RspBuffer, 0, 2);
                ErrorCDMS errorCodeInt;
                try
                {
                    errorCodeInt = (ErrorCDMS)Convert.ToInt32(errorCode);
                }
                catch (Exception ex)
                {
                    errorCodeInt = ErrorCDMS.Unknown;
                }

                var res = new CDMS_Response();

                res.ErrorCode = errorCodeInt;
                res.Stream = BytesToString(RspBuffer, RspLength);
                res.EjectInfo = new CDMS_EjectInfo
                {
                    RejectCount = Convert.ToInt32(Encoding.Default.GetString(RspBuffer, 2, 1))
                };
                if (ret == 0)
                    res.isSuccess = true;
                else
                    res.isSuccess = false;

                return res;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Dispenser, $"FATAL: Ocurrió error de comunicación con el dispensador {ex.Message} ", ex);
                return new CDMS_Response
                {
                    isSuccess = false,
                    ErrorCode = ErrorCDMS.RuntimeError,
                    Stream = string.Empty
                };
            }


        }

        public CDMS_Response GetSensor()
        {
            try
            {
                Byte[] RspBuffer = new Byte[12000];
                int RspLength = 0;
                Array.Clear(RspBuffer, 0, RspBuffer.Length);
                int ret = CDMS_Api.DispenserCDMS(CommandCDMS.GETSENSOR, RspBuffer, ref RspLength);
                String errorCode = Encoding.Default.GetString(RspBuffer, 0, 2);
                ErrorCDMS errorCodeInt;
                try
                {
                    errorCodeInt = (ErrorCDMS)Convert.ToInt32(errorCode);
                }
                catch (Exception ex)
                {
                    errorCodeInt = ErrorCDMS.Unknown;
                }

                var res = new CDMS_Response();

                res.ErrorCode = errorCodeInt;
                res.Stream = BytesToString(RspBuffer, RspLength);

                if (ret == 0)
                    res.isSuccess = true;
                else
                    res.isSuccess = false;

                res.SensorInfo = new();
                res.SensorInfo.NearEnd = ByteToBoolArray(RspBuffer[2]);
                res.SensorInfo.CassetteConnected = ByteToBoolArray(RspBuffer[3]);
                res.SensorInfo.CassetteDismounted = ByteToBoolArray(RspBuffer[4]);
                res.SensorInfo.CassetteSkew1 = ByteToBoolArray(RspBuffer[5]);
                res.SensorInfo.CassetteSkew2 = ByteToBoolArray(RspBuffer[6]);

                res.SensorInfo.ScanStart = ((char)RspBuffer[7]) == '1';
                res.SensorInfo.Gate1 = ((char)RspBuffer[8]) == '1';
                res.SensorInfo.Gate2 = ((char)RspBuffer[9]) == '1';
                res.SensorInfo.SolenoidDirection = ((char)RspBuffer[10]) == '1';
                res.SensorInfo.Exit = ((char)RspBuffer[11]) == '1';
                res.SensorInfo.RejectIn = ((char)RspBuffer[12]) == '1';
                res.SensorInfo.RejectBoxOpen = ((char)RspBuffer[13]) == '1';
                res.SensorInfo.CisOpen = ((char)RspBuffer[14]) == '1';

                return res;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.P_Dispenser, $"FATAL: Ocurrió error de comunicación con el dispensador {ex.Message} ", ex);
                return new CDMS_Response
                {
                    isSuccess = false,
                    ErrorCode = ErrorCDMS.RuntimeError,
                    Stream = string.Empty
                };
            }

        }


    }

    public class CDMS_Response
    {
        public bool isSuccess { get; set; }
        private ErrorCDMS _errorCode;
        public ErrorCDMS ErrorCode
        {
            get
            {
                return _errorCode;
            }
            set
            {
                ErrorDescription = value.ToString();
                _errorCode = value;
            }
        }
        public string ErrorDescription { get; set; } = string.Empty;

        public string? Stream { get; set; }
        public List<CDMS_DenomInfo>? DispenseData { get; set; }
        public CDMS_EjectInfo? EjectInfo { get; set; }
        public CDMS_SensorInfo? SensorInfo { get; set; }

    }

    public class CDMS_DenomInfo
    {

        public int Denomination { get; set; }
        public int RequestedQuantity { get; set; }
        public int OutOfCassetteQuantity { get; set; }
        public int DispensedQuantity { get; set; }
        public int RejectQuantity { get; set; }
    }

    public class CDMS_EjectInfo
    {
        public int RejectCount { get; set; }
    }

    public class CDMS_SensorInfo
    {
        public bool[] NearEnd { get; set; } // false: Lack of bills
        public bool[] CassetteConnected { get; set; } //true: Cassette Already connected
        public bool[] CassetteDismounted { get; set; } // true: Cassette Dismounted
        public bool[] CassetteSkew1 { get; set; } //true: jam in Skew
        public bool[] CassetteSkew2 { get; set; } // true: jam in Skew
        public bool ScanStart { get; set; } // true: jam in Scanner
        public bool Gate1 { get; set; } // true: jam in gate sensor
        public bool Gate2 { get; set; } // true: jam in gate sensor
        public bool SolenoidDirection { get; set; } // true: going to reject
        public bool Exit { get; set; } // true: jam in Exit sensor
        public bool RejectIn { get; set; } // true: jam in Reject in sensor
        public bool RejectBoxOpen { get; set; } // true: reject box open
        public bool CisOpen { get; set; } // true: cis Open
    }
}
