using System.Runtime.InteropServices;

namespace Domain.HantleDispenserAPI
{
    public class CDMS_Api
    {
        private const string LibraryFile = "CDMS_CDU.dll";
        //Serial
        [DllImport(LibraryFile)]
        public static extern int ConnectCDMS(int port);

        [DllImport(LibraryFile)]
        public static extern int DisConnectCDMS();

        [DllImport(LibraryFile)]
        public static extern int SetProgramCDMS(char index, Byte[] RspBuffer, ref int RspLength);//Program Set

        [DllImport(LibraryFile)]
        public static extern int SetParameterCDMS(int speed, int frontScanStartGap, int rearScanStartGap
                                    , int frontScanEndGap, int rearScanEndGap, Byte[] RspBuffer, ref int RspLength);//Parameter Set

        [DllImport(LibraryFile)]
        public static extern int InitializeDispenserCDMS(char OCRunreadCanCount
            , char countryType, char count, char irCheck, string index, Byte[] RspBuffer, ref int RspLength);


        [DllImport(LibraryFile)]
        public static extern int DispenserCDMS(Byte cmd, Byte[] RspBuffer, ref int RepLength);//Control

        [DllImport(LibraryFile, CharSet = CharSet.Ansi)]
        public static extern int DispenseCDMS(char count, string data, Byte[] RspBuffer, ref int RspLength);//Dispense

        [DllImport(LibraryFile)]
        public static extern int EjectCDMS(char BackFeedRetry, byte[] RspBuffer, ref int RspLength);//Reject


    }

    public enum ErrorCDMS
    {
        OK,
        Unknown,
        RejectMaxCount, //Continous Reject up to 5 sheets
        RejectMaxTotal, // Up to 10 rejects
        PickoutRetryOver,
        PickoutTimeout,
        NearEndSensor = 10, // Cassete near to be empty
        ID1Switch,
        ID2Switch,
        Skew1Sensor,
        Skew2Sensor,
        ScanStartSensor,
        Gate1PathSensor,
        Gate2PathSensor,
        SolenoidSensor,
        Exit1PathSensor,
        RejectInSensor,
        RejectBoxSwitch,
        CISOpenSwitch,
        SolenoidExitTurnabout,
        SolenoidRejectTurnabout,
        SolenoidExitDirectionError = 27,
        SolenoidRejectDirectionError = 28,
        MainFeedMotorJam = 30,
        SkewSensorJam,
        ScanSensorJam,
        GateSensorJam,
        ExitSensorJam,
        RejectInSensorJam,
        AbnormalScanStartSensor,
        AbnormalGateSensor,
        ValidatorSetting = 40,
        ValidatorResponseNothing,
        ValidatorResponseOrder,
        ValidatorResponseBCC,
        ValidatorCISCalibration,
        ValidatorSelectFirmware,
        ValidatorTimeout,
        ValidatorImage,
        ValidatorInputDispenseSpeed = 50,
        InputDispenseCasseteCount,
        FirmwareDownloadTimeout,
        FirmwareDownloadData,
        FirmwareDownloadSize,
        CasseteMounting,
        DirectionReturnTimeout,
        DoublePickout = 60,
        Cassete1JamOrEmpty,
        Cassete2JamOrEmpty,
        Cassete3JamOrEmpty,
        Cassete4JamOrEmpty,
        Cassete5JamOrEmpty,
        Cassete6JamOrEmpty,
        PaperShortGap,
        PaperLongLength,
        PaperShortLength,
        RuntimeError
    }

    public static class CommandCDMS
    {
        public static readonly Byte INITIALIZE = 0x54;
        public static readonly Byte GETSENSOR = 0x53;
        public static readonly Byte GETVERSION = 0x56;
        public static readonly Byte SETPARAMETER = 0x50;
        public static readonly Byte SETPROGRAM = 0x4D;
        public static readonly Byte DISPENSE = 0x44;
    }
}
