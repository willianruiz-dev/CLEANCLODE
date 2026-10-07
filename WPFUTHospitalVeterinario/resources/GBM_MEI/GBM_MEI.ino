
/*
   This code is to use all machines of bill (acceptance and dispensers)
   The idea is to create an integrated software to operate all in one board


   Made by Mateo Velez - Metavix
   Email: metavixmv@gmail.com
*/

//#include "MemoryFree.h"
#include <avr/pgmspace.h>
#include <SoftwareSerial.h> //Libreria para comunicacion Serial por Software
//#define HEXA_COMMANDS  // This is for coin acceptor of HEXA



//#define DEBUG_METAVIX
bool debugFlag = false;

#define BAUDRATE_PC 57600  // Speed of connection to the pc
#define BAUDRATE_DISPENSER 9600  // Speed of communication of the pc
#define BAUDRATE_ACCEPTANCE 9600  // Speed of communication of acceptance

/*
   ----------------------------------------------------------
   -------------- Variable information of bills -------------
   ----------------------------------------------------------
*/

#define NUMBER_OF_CASSETTES 2
#define NOTE_UP 10000
#define NOTE_MIDDLE 2000
#define NOTE_BOTTOM 2000  // Set equal to middle if there are 2 boxes
/*
   -----------------------------------------------------------
   ------------- Variable information of coins ---------------
   -----------------------------------------------------------
*/
#define NUMBER_OF_HOPPERS 3  // Quantity of hoppers that are installed
#define HOPPER_ONE 500   //5  // Value of currency of hopper one
#define HOPPER_TWO 200   //2  // Value of currency of hopper two
#define HOPPER_THREE 100   //1  // Value of currency of hopper three

#define RELAY_ONE_PIN_NAME A4 //A1 //8   // 500
#define RELAY_TWO_PIN_NAME A3 //A0 //9 // 200
#define RELAY_THREE_PIN_NAME A2 //A5  //10 // 100

#define SENSOR_ONE_PIN_NAME 4  // 500
#define SENSOR_TWO_PIN_NAME 3  // 200
#define SENSOR_THREE_PIN_NAME 2  // 100

#if defined(HEXA_COMMANDS)
SoftwareSerial mySerial(12, 3);
#else
#define PULSE_COMMANDS
#define COUNTER_PIN_NAME 7  // Pin to counter each pulse
#endif

#define ACTIVATOR_PIN_NAME 6  // Pin to activate or deactivate the coin acceptance

#define MOTOR_TIMEOUT 3000  // Timeout to turn of each motor

/*
   -----------------------------------------------------------
   ------------------------ END ------------------------------
   -----------------------------------------------------------
*/

#define RESPONSE_SIZE 254  // 
#define MESSAGE_SIZE 254  // 
#define COMMANDS_SIZE 50  // 
#define STATE_ACCEPTANCE_STACK 1

#define TIMEOUT 10000

#define STATE_ACCEPTANCE_STATUS 0
#define STATE_ACCEPTANCE_SEND_ACK 2
#define STATE_ACCEPTANCE_GET_DATA 3
#define STATE_ACCEPTANCE_INITIALIZE 4
#define STATE_ACCEPTANCE_RESET 5
#define STATE_ACCEPTANCE_FATAL_ERROR 6
#define STATE_ACCEPTANCE_VEND_VALID 7

#define STATE_WAITTING_FOR_COMMAND 8
#define STATE_ENCRYPT 9
#define STATE_PARSE_PC_COMMAND 10

#define STATE_ACCEPTANCE 11
#define STATE_DISPENSE 12

/*
   -----------------------------------------------------------------------
   ----------------------- States of dispenser ---------------------------
   -----------------------------------------------------------------------
*/

#define STATE_DISPENSER_COUNT_NOTES 13
#define STATE_DISPENSER_SEND_COMMAND 14
#define STATE_DISPENSER_GETTING_COMMAND 15
#define STATE_DISPENSER_ANSWER_PC 16
#define STATE_DISPENSER_INITIALIZE 17
#define STATE_DISPENSER_PARSE_INFORMATION 18
#define STATE_DISPENSER_PARSE_RESPONSE 19

/*
   ------------------------------------------------------------------------
   ---------------------- STATES OF COINS ---------------------------------
   ------------------------------------------------------------------------
*/

#define STATE_COUNT_MONEY_FOR_RETURN 20  // State to set quantity to return money
#define STATE_READING_COIN 21  // State to turn on or off the coin acceptor
#define STATE_SENDING_MESSAGE_TO_PC 22  // State to return message to PC
#define STATE_TURN_ON_MOTORS 23  // State to turn on each motor


/*
   -----------------------------------------------------------------------
   ------------------------ Dispenser definitions ------------------------
   -----------------------------------------------------------------------
*/

#define ASCII_ENQ 0x05  // The ASCII command to do a enquiry
#define ASCII_ACK 0x06  // The ASCII command to do an aknowledgement
#define NAK 0x15  // The ASCII command to do a negative acknowledgement
#define EOT 0x04  // The ASCII command to do an end of thanssaction
#define CAN 0x18  // The ASCII command to do a cancel
#define STX 0x02  // The ASCII command to specify the begin of text
#define ETX 0x03  // The ASCII command to specify the end of text
#define US 0x1F  // The ASCII command to do an unit separator


/*
   ----------------------------------------------------------------------
   ---------------------- Commands of dispenser -------------------------
   ----------------------------------------------------------------------
*/
// Commands

#define DISPENSER_INITIALIZE 0x041  // Command to initialize the CDU
#define DISPENSER_STATUS 0x22  // Command to request the status of CDU
#define DISPENSER_INFORMATION_SET 0x24  // Command to set an information to CDU
#define DISPENSER_DISPENSE 0x42  // Command to dispense the quantity in the boxes

// Cassetes

#define CASETTE_1 0x31  // Command to identify the box 1
#define CASETTE_2 0x32  // Command to identify the box 2
#define CASETTE_3 0x33  // Command to identify the box 3
#define CASETTE_4 0x34  // Command to identify the box 4






/*
   -----------------------------------------------------------------------
   ------------------ Responses to sendig STATUS command -----------------
   -----------------------------------------------------------------------
*/
#define ENABLE 0x11
#define ACCEPTING 0x12
#define ESCROW 0x13
#define STACKING 0x14
#define VEND_VALID 0x15
#define STACKED 0x16
#define REJECTING 0x17
#define RETURNING 0x18
#define HOLDING 0x19
#define DISABLE 0x1A
#define INITIALIZE 0x1B

/*
   -----------------------------------------------------------------------
   ---------------- Responses to sendig POWER UP command -----------------
   -----------------------------------------------------------------------
*/

#define POWER_UP 0x40
#define POWER_UP_WITH_BILL_IN_ACCEPTOR 0x41
#define ENABLE_UP_WITH_BILL_IN_STACKER 0x42

/*
   -----------------------------------------------------------------------
   ------------ Responses to sendig ERROR STATUS command -----------------
   -----------------------------------------------------------------------
*/

#define STACKER_FULL 0x43
#define STACKER_OPEN 0x44
#define JAM_IN_ACCEPTOR 0x45
#define JAM_IN_STACKER 0x46
#define PAUSE 0x47
#define CHEATED 0x48
#define FAILURE 0x49
#define COMMUNICATION_ERROR 0x4A

/*
   -----------------------------------------------------------------------
   ------------ Responses to sendig POLL REQUEST command -----------------
   -----------------------------------------------------------------------
*/

#define ENQ 0x05

/*
   -----------------------------------------------------------------------
   --------------- Responses to sendig OPERATION command -----------------
   -----------------------------------------------------------------------
*/

#define ACK 0x50
#define INVALID_COMMAND 0x48

/*
   -----------------------------------------------------------------------
   ------------ Responses to sendig SETTING command ----------------------
   -----------------------------------------------------------------------
*/

#define ENABLE_DENOMINATION 0xC0
#define SECURITY 0xC1
#define COMMUNICATION_MODE 0xC2
#define INHIBIT 0xC3
#define DIRECTION 0xC4
#define OPTIONAL_FUNCTION 0xC5

/*
   -----------------------------------------------------------------------
   ---------- Responses to sendig SETTING STATUS command -----------------
   -----------------------------------------------------------------------
*/

#define SET_ENABLE_DENOMINATION 0x80
#define SET_SECURITY 0x81
#define SET_COMMUNICATION_MODE 0x82
#define SET_INHIBIT 0x83
#define SET_DIRECTION 0x84
#define SET_OPTIONAL_FUNCTION 0x85
#define SET_VERSION_INFORMATION 0x88
#define SET_BOOT_VERSION_INFORMATION 0x89
#define SET_DENOMINATION_DATA 0x8A

/*
   -----------------------------------------------------------------------
   ------------------------ REJECT DATA TABLE ----------------------------
   -----------------------------------------------------------------------
*/

#define INSERTION_ERROR 0x71
#define MUG_ERROR 0x72
#define RETURN_ACTION_RESIDUAL_BILLS_ACCEPTOR 0x73
#define CALIBRATION_MAGNIFICATION_ERROR 0x74
#define CONVEYING_ERROR 0x75
#define DISCRIMINATION_ERROR_FOR_BILL_DENOMINATION 0x76
#define PHOTO_PATTERN_ERROR 0x77
#define PHOTO_LEVEL_ERROR 0x78
#define DIRECTION_DENOMINATION_NO_COMMAND_SENT_ANSWERING 0x79
#define NO_LIFE_NO_COMMAND 0x7A
#define OPERATION_ERROR 0x7B
#define RETURN_ACTION_RESIDUAL_BILLS_STACKER 0x7C
#define LENGTH_ERROR 0x7D
#define PHOTO_PHATERN_ERROR_2 0x7E
#define TRUE_BILL_FEATURE_ERROR 0x7F

/*
   -----------------------------------------------------------------------
   ------------------------ FAILURE DATA TABLE ---------------------------
   -----------------------------------------------------------------------
*/

#define STACK_MOTOR_FAILURE 0xA2
#define TRANSPORT_MOTOR_SPEED_FAILURE 0xA5
#define TRANSPORT_MOTOR_FAILURE 0xA6
#define SOLENOID_FAILURE 0xA8
#define PB_UNIT_FAILURE 0xA9
#define CASHBOX_NOT_READY 0xAB
#define VALIDATOR_HEAD_REMOVE 0xAF
#define BOOT_ROM_FAILURE 0xB0
#define EXTERNAL_ROM_FAILURE 0xB1
#define RAM_FAILURE 0xB2
#define EXTERNAL_ROM_WRITING_FAILURE 0xB3

/*
   -----------------------------------------------------------------------
   ------------------------- Commands to send ----------------------------
   -----------------------------------------------------------------------
                      Controller -----> Acceptor
*/

#define SET_STATUS 0x11
#define SET_RESET 0x40
#define SET_STACK_1 0x41
#define SET_STACK_2 0x42
#define SET_RETURN 0x43
#define SET_HOLD 0x44
#define SET_WAIT 0x45




/*
   ------------------------------------------------------------------------------------------
   ---------------------- STATE MACHINE CREATION AND DECLARATION ----------------------------
   ------------------------------------------------------------------------------------------

  #define STATE_ACCEPTANCE_STATUS 0
  #define STATE_ACCEPTANCE_STACK 1
  #define STATE_ACCEPTANCE_SEND_ACK 2
  #define STATE_ACCEPTANCE_GET_DATA 3
  #define STATE_ACCEPTANCE_INITIALIZE 4
  #define STATE_ACCEPTANCE_RESET 5
  #define STATE_ACCEPTANCE_FATAL_ERROR 6
  #define STATE_ACCEPTANCE_VEND_VALID 7

  #define STATE_WAITTING_FOR_COMMAND 8
  #define STATE_ENCRYPT 9
  #define STATE_PARSE_PC_COMMAND 10

  #define STATE_ACCEPTANCE 11
  #define STATE_DISPENSER 12

  #define STATE_DISPENSER_COUNT_NOTES 13
  #define STATE_DISPENSER_SEND_COMMAND 14
  #define STATE_DISPENSER_GETTING_COMMAND 15
  #define STATE_DISPENSER_ANSWER_PC 16
  #define STATE_DISPENSER_INITIALIZE 17
  #define STATE_DISPENSER_PARSE_INFORMATION 18
  #define STATE_DISPENSER_PARSE_RESPONSE 19


  #define STATE_COUNT_MONEY_FOR_RETURN 20  // State to set quantity to return money
  #define STATE_CONFIGURATION 21  // State to configure the quantity of motors and predefined coins
  #define STATE_READING_COIN 22  // State to turn on or off the coin acceptor
  #define STATE_SENDING_MESSAGE_TO_PC 23  // State to return message to PC
  #define STATE_TURN_ON_MOTORS 24  // State to turn on each motor
  #define STATE_COUNT_PULSES 25 // State to count each pulse


*/
int state = STATE_WAITTING_FOR_COMMAND;  // This is to initialize the state machine

typedef void (*STATE_MACHINE)();  // Define STATE_MACHINE as a pointer function
STATE_MACHINE *machine = (STATE_MACHINE *)malloc(sizeof(STATE_MACHINE *) * 24);  // Allocation of space of state machine

void stateAcceptanceStatus();
void stateAcceptanceStack();
void stateAcceptanceSendAck();
void stateAcceptanceGetData();
void stateAcceptanceInitialize();
void stateAcceptanceReset();
void stateAcceptanceFatalError();
void stateAcceptanceVendValid();

void stateWaittingForCommand();
void stateEncrypt();
void stateParsePcCommand();

void stateAcceptance();
void stateDispenser();

void stateDispenserCountNotes();
void stateDispenserSendCommand();
void stateDispenserGettingCommand();
void stateDispenserAnswerPc();
void stateDispenserInitialize();
void stateDispenserParseInformation();
void stateDispenserParseResponse();

void stateCountMoneyForReturn();
void stateReadingCoin();
void stateSendingMessageToPc();
void stateTurnOnMotors();



/* ----------------------------------------------------------------------------
   ------------------------- General variables --------------------------------
   ----------------------------------------------------------------------------
*/

char commands[COMMANDS_SIZE];
char message[MESSAGE_SIZE];
char response[RESPONSE_SIZE];
String messageSplited[7];
bool flagForPowerUp = true;

/* ----------------------------------------------------------------------------
   ------------------------ Acceptor variables --------------------------------
   ----------------------------------------------------------------------------
*/

String errorMessage[33];
String orders[4];
int ordersState[4];
int flagForResponseAction[62];
uint32_t values[7];
uint32_t unMoney;
char statusResponseTable[69];
unsigned short flagBills = 0;

/* --------------------------------------------------------------------------
   --------------------- Dispenser variables --------------------------------
   --------------------------------------------------------------------------
*/

uint32_t quantity = 0;
uint32_t quantityCoins = 0;
unsigned long noteCassette[4];
uint8_t quantityBox[4];
uint8_t tryCatchDispense = 0;
char errorsDispenser[50];  // This variable is to keep all errors codes to compare it
int lastState = STATE_WAITTING_FOR_COMMAND;
const String errorsDispenserResponse[50];
char XN[65];
int valuesOfCassettes[6];
bool flagCassetteDispense[3];
unsigned short flagBillsDisp = 0;
int szDispCmd = 0;

/*
   ------------------------------------------------------------------------------
   ----------------------------- Coins Variables --------------------------------
   ------------------------------------------------------------------------------
*/

uint32_t hopperQuantity[3];  // This is the value of each hopper to dispense
uint8_t motorFlag;  // This flag is to know the last motor that was turned on
int pinRelayNames[3];  // This is to save the pin names of each relay
int pinSensorNames[3];  // This is to save the pin names of each sensor
int lastSensorState[3];  // This is to check the last values of each motor sensor

int8_t errorFlag;  // This is to know what motor are disabled ++
String coinErrorMessage[8];  // This variable is to save all error message
uint32_t staticCoinValues[3];  // This is the values of each box of coins
uint32_t coinsToReturn[3];  // This variable is to know the quantity of each coin to return
uint32_t coinsReturned[3];  // This variable is to know the quantity of each coin to return

/*
   ------------------------------------------------------------------------------------------
   ------------------------------------  END ------------------------------------------------
   ------------------------------------------------------------------------------------------
*/

extern volatile unsigned long timer0_millis;
unsigned long new_value = 0;

void setMillis(unsigned long new_millis) {
  uint8_t oldSREG = SREG;
  cli();
  timer0_millis = new_millis;
  SREG = oldSREG;
}


void debugMetavixln(String msg, int temp = 0);
void debugMetavix(String msg, int temp = 0);




// shit to erase 

int timePulse = 0;
  int pulseState = 0;
  int pulseStateBefore = 0;
  volatile int pulseCounter = 0;
static const unsigned short CRC_CCITT_TABLE[256] = {
  0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50A5, 0x60C6, 0x70E7,
  0x8108, 0x9129, 0xA14A, 0xB16B, 0xC18C, 0xD1AD, 0xE1CE, 0xF1EF,
  0x1231, 0x0210, 0x3273, 0x2252, 0x52B5, 0x4294, 0x72F7, 0x62D6,
  0x9339, 0x8318, 0xB37B, 0xA35A, 0xD3BD, 0xC39C, 0xF3FF, 0xE3DE,
  0x2462, 0x3443, 0x0420, 0x1401, 0x64E6, 0x74C7, 0x44A4, 0x5485,
  0xA56A, 0xB54B, 0x8528, 0x9509, 0xE5EE, 0xF5CF, 0xC5AC, 0xD58D,
  0x3653, 0x2672, 0x1611, 0x0630, 0x76D7, 0x66F6, 0x5695, 0x46B4,
  0xB75B, 0xA77A, 0x9719, 0x8738, 0xF7DF, 0xE7FE, 0xD79D, 0xC7BC,
  0x48C4, 0x58E5, 0x6886, 0x78A7, 0x0840, 0x1861, 0x2802, 0x3823,
  0xC9CC, 0xD9ED, 0xE98E, 0xF9AF, 0x8948, 0x9969, 0xA90A, 0xB92B,
  0x5AF5, 0x4AD4, 0x7AB7, 0x6A96, 0x1A71, 0x0A50, 0x3A33, 0x2A12,
  0xDBFD, 0xCBDC, 0xFBBF, 0xEB9E, 0x9B79, 0x8B58, 0xBB3B, 0xAB1A,
  0x6CA6, 0x7C87, 0x4CE4, 0x5CC5, 0x2C22, 0x3C03, 0x0C60, 0x1C41,
  0xEDAE, 0xFD8F, 0xCDEC, 0xDDCD, 0xAD2A, 0xBD0B, 0x8D68, 0x9D49,
  0x7E97, 0x6EB6, 0x5ED5, 0x4EF4, 0x3E13, 0x2E32, 0x1E51, 0x0E70,
  0xFF9F, 0xEFBE, 0xDFDD, 0xCFFC, 0xBF1B, 0xAF3A, 0x9F59, 0x8F78,
  0x9188, 0x81A9, 0xB1CA, 0xA1EB, 0xD10C, 0xC12D, 0xF14E, 0xE16F,
  0x1080, 0x00A1, 0x30C2, 0x20E3, 0x5004, 0x4025, 0x7046, 0x6067,
  0x83B9, 0x9398, 0xA3FB, 0xB3DA, 0xC33D, 0xD31C, 0xE37F, 0xF35E,
  0x02B1, 0x1290, 0x22F3, 0x32D2, 0x4235, 0x5214, 0x6277, 0x7256,
  0xB5EA, 0xA5CB, 0x95A8, 0x8589, 0xF56E, 0xE54F, 0xD52C, 0xC50D,
  0x34E2, 0x24C3, 0x14A0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
  0xA7DB, 0xB7FA, 0x8799, 0x97B8, 0xE75F, 0xF77E, 0xC71D, 0xD73C,
  0x26D3, 0x36F2, 0x0691, 0x16B0, 0x6657, 0x7676, 0x4615, 0x5634,
  0xD94C, 0xC96D, 0xF90E, 0xE92F, 0x99C8, 0x89E9, 0xB98A, 0xA9AB,
  0x5844, 0x4865, 0x7806, 0x6827, 0x18C0, 0x08E1, 0x3882, 0x28A3,
  0xCB7D, 0xDB5C, 0xEB3F, 0xFB1E, 0x8BF9, 0x9BD8, 0xABBB, 0xBB9A,
  0x4A75, 0x5A54, 0x6A37, 0x7A16, 0x0AF1, 0x1AD0, 0x2AB3, 0x3A92,
  0xFD2E, 0xED0F, 0xDD6C, 0xCD4D, 0xBDAA, 0xAD8B, 0x9DE8, 0x8DC9,
  0x7C26, 0x6C07, 0x5C64, 0x4C45, 0x3CA2, 0x2C83, 0x1CE0, 0x0CC1,
  0xEF1F, 0xFF3E, 0xCF5D, 0xDF7C, 0xAF9B, 0xBFBA, 0x8FD9, 0x9FF8,
  0x6E17, 0x7E36, 0x4E55, 0x5E74, 0x2E93, 0x3EB2, 0x0ED1, 0x1EF0
};

bool acceptFlag =false;

void setup() {
  Serial.begin(BAUDRATE_PC);
  Serial2.begin(BAUDRATE_DISPENSER);
  Serial3.begin(BAUDRATE_ACCEPTANCE, SERIAL_8E1);

  setPrdefinedConfiguration();
  fillErrorArray();
  clearCoinsToReturnAndReturned();

  statusCheckerFilling();

  machine[0] = (STATE_MACHINE)&stateAcceptanceStatus;
  machine[1] = (STATE_MACHINE)&stateAcceptanceStack;
  machine[2] = (STATE_MACHINE)&stateAcceptanceSendAck;
  machine[3] = (STATE_MACHINE)&stateAcceptanceGetData;
  machine[4] = (STATE_MACHINE)&stateAcceptanceInitialize;
  machine[5] = (STATE_MACHINE)&stateAcceptanceReset;
  machine[6] = (STATE_MACHINE)&stateAcceptanceFatalError;
  machine[7] = (STATE_MACHINE)&stateAcceptanceVendValid;
  machine[8] = (STATE_MACHINE)&stateWaittingForCommand;
  machine[9] = (STATE_MACHINE)&stateEncrypt;
  machine[10] = (STATE_MACHINE)&stateParsePcCommand;
  machine[11] = (STATE_MACHINE)&stateAcceptance;
  machine[12] = (STATE_MACHINE)&stateDispenser;
  machine[13] = (STATE_MACHINE)&stateDispenserCountNotes;
  machine[14] = (STATE_MACHINE)&stateDispenserSendCommand;
  machine[15] = (STATE_MACHINE)&stateDispenserGettingCommand;
  machine[16] = (STATE_MACHINE)&stateDispenserAnswerPc;
  machine[17] = (STATE_MACHINE)&stateDispenserInitialize;
  machine[18] = (STATE_MACHINE)&stateDispenserParseInformation;
  machine[19] = (STATE_MACHINE)&stateDispenserParseResponse;
  machine[20] = (STATE_MACHINE)&stateCountMoneyForReturn;
  machine[21] = (STATE_MACHINE)&stateReadingCoin;
  machine[22] = (STATE_MACHINE)&stateSendingMessageToPc;
  machine[23] = (STATE_MACHINE)&stateTurnOnMotors;

  clearResponse();
  xorTable();
  state = STATE_WAITTING_FOR_COMMAND;
  debugMetavixln("Setup");
  delay(1000);
}

void loop() {
  machine[state]();
  checkMessage();
  if(acceptFlag)
    stateCountPulses();  
}

/*
   This function is to send the command to the devices

*/

void sendCmd(char* cmd, int len) {
  Serial3.write(cmd, len);
}


void statusCheckerFilling() {
  statusResponseTable[0] = ENABLE;                                             flagForResponseAction[0] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[1] = ACCEPTING;                                          flagForResponseAction[1] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[2] = ESCROW;                                             flagForResponseAction[2] = STATE_ACCEPTANCE_STACK;
  statusResponseTable[3] = STACKING;                                           flagForResponseAction[3] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[4] = VEND_VALID;                                         flagForResponseAction[4] = STATE_ACCEPTANCE_VEND_VALID;
  statusResponseTable[5] = STACKED;                                            flagForResponseAction[5] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[6] = REJECTING;                                          flagForResponseAction[6] = STATE_ACCEPTANCE_GET_DATA;
  statusResponseTable[7] = RETURNING;                                          flagForResponseAction[7] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[8] = HOLDING;                                            flagForResponseAction[8] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[9] = DISABLE;                                            flagForResponseAction[9] = STATE_WAITTING_FOR_COMMAND;
  statusResponseTable[10] = INITIALIZE;                                        flagForResponseAction[10] = STATE_ACCEPTANCE_INITIALIZE;

  statusResponseTable[11] = POWER_UP;                                          flagForResponseAction[11] = STATE_ACCEPTANCE_RESET;
  statusResponseTable[12] = POWER_UP_WITH_BILL_IN_ACCEPTOR;                    flagForResponseAction[12] = STATE_ACCEPTANCE_RESET;
  statusResponseTable[13] = ENABLE_UP_WITH_BILL_IN_STACKER;                    flagForResponseAction[13] = STATE_ACCEPTANCE_RESET;

  statusResponseTable[14] = STACKER_FULL;                                      flagForResponseAction[14] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[15] = STACKER_OPEN;                                      flagForResponseAction[15] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[16] = JAM_IN_ACCEPTOR;                                   flagForResponseAction[16] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[17] = JAM_IN_STACKER;                                    flagForResponseAction[17] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[18] = PAUSE;                                             flagForResponseAction[18] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[19] = CHEATED;                                           flagForResponseAction[19] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[20] = FAILURE;                                           flagForResponseAction[20] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[21] = COMMUNICATION_ERROR;                               flagForResponseAction[21] = STATE_ACCEPTANCE_STATUS;
  //-------------------------------------------------------------------
  statusResponseTable[22] = ENQ;                                               flagForResponseAction[22] = STATE_ACCEPTANCE_STATUS;

  statusResponseTable[23] = ACK;                                               flagForResponseAction[23] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[24] = INVALID_COMMAND;                                   flagForResponseAction[24] = STATE_ACCEPTANCE_FATAL_ERROR;

  statusResponseTable[25] = ENABLE_DENOMINATION;                               flagForResponseAction[25] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[26] = SECURITY;                                          flagForResponseAction[26] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[27] = COMMUNICATION_MODE;                                flagForResponseAction[27] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[28] = INHIBIT;                                           flagForResponseAction[28] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[29] = DIRECTION;                                         flagForResponseAction[29] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[30] = OPTIONAL_FUNCTION;                                 flagForResponseAction[30] = STATE_ACCEPTANCE_STATUS;

  statusResponseTable[31] = SET_ENABLE_DENOMINATION;                           flagForResponseAction[31] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[32] = SET_SECURITY;                                      flagForResponseAction[32] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[33] = SET_COMMUNICATION_MODE;                            flagForResponseAction[33] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[34] = SET_INHIBIT;                                       flagForResponseAction[34] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[35] = SET_DIRECTION;                                     flagForResponseAction[35] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[36] = SET_OPTIONAL_FUNCTION;                             flagForResponseAction[36] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[37] = SET_VERSION_INFORMATION;                           flagForResponseAction[37] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[38] = SET_BOOT_VERSION_INFORMATION;                      flagForResponseAction[38] = STATE_ACCEPTANCE_STATUS;
  statusResponseTable[39] = SET_DENOMINATION_DATA;                             flagForResponseAction[39] = STATE_ACCEPTANCE_STATUS;

  statusResponseTable[40] = INSERTION_ERROR;                                   flagForResponseAction[40] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[41] = MUG_ERROR;                                         flagForResponseAction[41] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[42] = RETURN_ACTION_RESIDUAL_BILLS_ACCEPTOR;             flagForResponseAction[42] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[43] = CALIBRATION_MAGNIFICATION_ERROR;                   flagForResponseAction[43] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[44] = CONVEYING_ERROR;                                   flagForResponseAction[44] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[45] = DISCRIMINATION_ERROR_FOR_BILL_DENOMINATION;        flagForResponseAction[45] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[46] = PHOTO_PATTERN_ERROR;                               flagForResponseAction[46] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[47] = DIRECTION_DENOMINATION_NO_COMMAND_SENT_ANSWERING;  flagForResponseAction[47] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[48] = RETURN_ACTION_RESIDUAL_BILLS_STACKER;              flagForResponseAction[48] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[49] = LENGTH_ERROR;                                      flagForResponseAction[49] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[50] = PHOTO_PHATERN_ERROR_2;                             flagForResponseAction[50] = STATE_ACCEPTANCE_STATUS;  // +
  statusResponseTable[51] = TRUE_BILL_FEATURE_ERROR;                           flagForResponseAction[51] = STATE_ACCEPTANCE_STATUS;  // +

  statusResponseTable[52] = STACK_MOTOR_FAILURE;                               flagForResponseAction[52] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[53] = TRANSPORT_MOTOR_SPEED_FAILURE;                     flagForResponseAction[53] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[54] = TRANSPORT_MOTOR_FAILURE;                           flagForResponseAction[54] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[55] = SOLENOID_FAILURE;                                  flagForResponseAction[55] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[56] = CASHBOX_NOT_READY;                                 flagForResponseAction[56] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[57] = VALIDATOR_HEAD_REMOVE;                             flagForResponseAction[57] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[58] = BOOT_ROM_FAILURE;                                  flagForResponseAction[58] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[59] = EXTERNAL_ROM_FAILURE;                              flagForResponseAction[59] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[60] = RAM_FAILURE;                                       flagForResponseAction[60] = STATE_ACCEPTANCE_FATAL_ERROR;
  statusResponseTable[61] = EXTERNAL_ROM_WRITING_FAILURE;                      flagForResponseAction[61] = STATE_ACCEPTANCE_FATAL_ERROR;

  statusResponseTable[62] = 0x61; // $1
  statusResponseTable[63] = 0x62; // $2
  statusResponseTable[64] = 0x63; // $5
  statusResponseTable[65] = 0x64; // $10
  statusResponseTable[66] = 0x65; // $20
  statusResponseTable[67] = 0x66; // $50
  statusResponseTable[68] = 0x67; // $100

  values[0] = 1000;
  values[1] = 2000;
  values[2] = 5000;
  values[3] = 10000;
  values[4] = 20000;
  values[5] = 50000;
  values[6] = 100000;

  orders[0] = "START";           ordersState[0] = STATE_ENCRYPT;
  orders[1] = "AP";              ordersState[1] = STATE_ACCEPTANCE;
  orders[2] = "DP";              ordersState[2] = STATE_PARSE_PC_COMMAND;
  orders[3] = String(millis());  ordersState[3] = STATE_WAITTING_FOR_COMMAND;
  debugMetavixln("The header is: " + orders[3]);


  errorMessage[0] = F("POWER_UP");                               // Status
  errorMessage[1] = F("POWER_UP_WITH_BILL_IN_ACCEPTOR");         // Stack
  errorMessage[2] = F("ENABLE_UP_WITH_BILL_IN_STACKER");         // Stack

  errorMessage[3] = F("STACKER_FULL");                           // Fatal error
  errorMessage[4] = F("STACKER_OPEN");                           // Fatal error
  errorMessage[5] = F("JAM_IN_ACCEPTOR");                        // Fatal error
  errorMessage[6] = F("JAM_IN_STACKER");                         // Fatal error
  errorMessage[7] = F("PAUSE");                                  // Fatal error
  errorMessage[8] = F("CHEATED");                                // Fatal error
  errorMessage[9] = F("FAILURE");                                // Fatal error
  errorMessage[10] = F("COMMUNICATION_ERROR");                   // Fatal error

  errorMessage[11] = F("INSERTION_ERROR");
  errorMessage[12] = F("MUG_ERROR");
  errorMessage[13] = F("RETURN_ACTION_RESIDUAL_BILLS_ACCEPTOR");
  errorMessage[14] = F("CALIBRATION_MAGNIFICATION_ERROR");
  errorMessage[15] = F("CONVEYING_ERROR");
  errorMessage[16] = F("DISCRIMINATION_ERROR_FOR_BILL_DENOMINATION");
  errorMessage[17] = F("PHOTO_PATTERN_ERROR");
  errorMessage[18] = F("DIRECTION_DENOMINATION_NO_COMMAND_SENT_ANSWERING");
  errorMessage[19] = F("RETURN_ACTION_RESIDUAL_BILLS_STACKER");
  errorMessage[20] = F("LENGTH_ERROR");
  errorMessage[21] = F("PHOTO_PHATERN_ERROR_2");
  errorMessage[22] = F("TRUE_BILL_FEATURE_ERROR");

  errorMessage[23] = F("STACK_MOTOR_FAILURE");
  errorMessage[24] = F("TRANSPORT_MOTOR_SPEED_FAILURE");
  errorMessage[25] = F("TRANSPORT_MOTOR_FAILURE");
  errorMessage[26] = F("SOLENOID_FAILURE");
  errorMessage[27] = F("CASHBOX_NOT_READY");
  errorMessage[28] = F("VALIDATOR_HEAD_REMOVE");
  errorMessage[29] = F("BOOT_ROM_FAILURE");
  errorMessage[30] = F("EXTERNAL_ROM_FAILURE");
  errorMessage[31] = F("RAM_FAILURE");
  errorMessage[32] = F("EXTERNAL_ROM_WRITING_FAILURE");

  /*
     Dispenser variables
  */

  quantityBox[0] = 0x00; quantityBox[1] = 0x00; quantityBox[2] = 0x00; quantityBox[3] = 0x00;

  noteCassette[0] = NOTE_UP;
  noteCassette[1] = NOTE_MIDDLE;
  noteCassette[2] = NOTE_BOTTOM;

  errorsDispenser[0] = 0x00;  errorsDispenser[10] = 0x1C;  errorsDispenser[20] = 0x27;  errorsDispenser[30] = 0x32;  errorsDispenser[40] = 0x4F;
  errorsDispenser[1] = 0x07;  errorsDispenser[11] = 0x1D;  errorsDispenser[21] = 0x28;  errorsDispenser[31] = 0x33;  errorsDispenser[41] = 0x51;
  errorsDispenser[2] = 0x11;  errorsDispenser[12] = 0x1E;  errorsDispenser[22] = 0x2A;  errorsDispenser[32] = 0x38;  errorsDispenser[42] = 0x55;
  errorsDispenser[3] = 0x12;  errorsDispenser[13] = 0x1F;  errorsDispenser[23] = 0x2B;  errorsDispenser[33] = 0x39;  errorsDispenser[43] = 0x70;
  errorsDispenser[4] = 0x13;  errorsDispenser[14] = 0x21;  errorsDispenser[24] = 0x2C;  errorsDispenser[34] = 0x3A;  errorsDispenser[44] = 0x71;
  errorsDispenser[5] = 0x15;  errorsDispenser[15] = 0x22;  errorsDispenser[25] = 0x2D;  errorsDispenser[35] = 0x4A;  errorsDispenser[45] = 0x72;
  errorsDispenser[6] = 0x16;  errorsDispenser[16] = 0x23;  errorsDispenser[26] = 0x2E;  errorsDispenser[36] = 0x4B;  errorsDispenser[46] = 0x73;
  errorsDispenser[7] = 0x17;  errorsDispenser[17] = 0x24;  errorsDispenser[27] = 0x2F;  errorsDispenser[37] = 0x4C;  errorsDispenser[47] = 0x74;
  errorsDispenser[8] = 0x1A;  errorsDispenser[18] = 0x25;  errorsDispenser[28] = 0x30;  errorsDispenser[38] = 0x4D;  errorsDispenser[48] = 0x88;
  errorsDispenser[9] = 0x1B;  errorsDispenser[19] = 0x26;  errorsDispenser[29] = 0x31;  errorsDispenser[39] = 0x4E;

  errorsDispenserResponse[0] = F("Normal operation");
  errorsDispenserResponse[1] = F("Failed to create default detect Double");
  errorsDispenserResponse[2] = F("Cassette # 1 failed to pick up the Bills");
  errorsDispenserResponse[3] = F("Cassette # 2 failed to pick up the Bills");
  errorsDispenserResponse[4] = F("Cassette # 3 failed to pick up the Bills");
  errorsDispenserResponse[5] = F("Cassette # 1 failed to pick up the Bills. ( If you are not checking by Near_end )");
  errorsDispenserResponse[6] = F("Cassette # 2 failed to pick up the Bills. ( If you are not checking by Near_end )");
  errorsDispenserResponse[7] = F("Cassette # 3 failed to pick up the Bills. ( If you are not checking by Near_end )");
  errorsDispenserResponse[8] = F("The Bills failed to clear the sensor S1");
  errorsDispenserResponse[9] = F("The Bills failed to clear the sensor S2");
  errorsDispenserResponse[10] = F("The Bills failed to clear the sensor S4");
  errorsDispenserResponse[11] = F("The Bills failed to clear the sensor S5");
  errorsDispenserResponse[12] = F("The Bills failed to clear the sensor S7");
  errorsDispenserResponse[13] = F("The Bills failed to clear the sensor S9");
  errorsDispenserResponse[14] = F("S6-S5 sensor JAM occurs in bills");
  errorsDispenserResponse[15] = F("S8-S7 sensor JAM occurs in bills");
  errorsDispenserResponse[16] = F("S10-S9 sensor JAM occurs in bills");
  errorsDispenserResponse[17] = F("S5-S4 sensor JAM occurs in bills");
  errorsDispenserResponse[18] = F("S4~S3,S2 sensor JAM occurs in bills");
  errorsDispenserResponse[19] = F("S2~S1 sensor JAM occurs in bills");
  errorsDispenserResponse[20] = F("The final count of dispensing error");
  errorsDispenserResponse[21] = F("The strange path of banknote checking");
  errorsDispenserResponse[22] = F("Will reject bills as normal dispensing");
  errorsDispenserResponse[23] = F("Solenoids no action");
  errorsDispenserResponse[24] = F("The Bills are a single thickness value is smallest");
  errorsDispenserResponse[25] = F("Solenoids are ON");
  errorsDispenserResponse[26] = F("S7~S5 sensor JAM occurs in bills");
  errorsDispenserResponse[27] = F("S9~S7 sensor JAM occurs in bills");
  errorsDispenserResponse[28] = F("Reject bills from more than 10note ( In the 1 transactions)");
  errorsDispenserResponse[29] = F("S6 Note JAM");
  errorsDispenserResponse[30] = F("S8 Note JAM");
  errorsDispenserResponse[31] = F("S10 Note JAM");
  errorsDispenserResponse[32] = F("Failed to OPEN the shutter (Option)");
  errorsDispenserResponse[33] = F("Failed to CLOSE the shutter (Option)");
  errorsDispenserResponse[34] = F("Failed to receive the Bills inside the shutter (shutter close behind the level Checking) (Option)");
  errorsDispenserResponse[35] = F("Retract operation failed. (The position of the shutter is bad) (Option)");
  errorsDispenserResponse[36] = F("Retract operation failed. (The Tray down operation fails) (Option)");
  errorsDispenserResponse[37] = F("Retract operation failed. (The Tray up operation fails) (Option)");
  errorsDispenserResponse[38] = F("Retract operation failed. (Action remains behind in bills on the Tray) (Option)");
  errorsDispenserResponse[39] = F("Replace the shutter sensor is bad or the shutter is not");
  errorsDispenserResponse[40] = F("Not the normal command type");
  errorsDispenserResponse[41] = F("Not the motor of the INITIAL action");
  errorsDispenserResponse[42] = F("Top of the Bill failed to clear the path taken");
  errorsDispenserResponse[43] = F("INITIAL behavior is not dispensing the command received");
  errorsDispenserResponse[44] = F("ID of the required dispensing cassette fails to pick up the Bill. None of the Bills inside the Cassette");
  errorsDispenserResponse[45] = F("Dispensing is not fitted with cassettes of ID required");
  errorsDispenserResponse[46] = F("Dispensing is not equipped with the required floor cassettes");
  errorsDispenserResponse[47] = F("INITIAL command the kind of bills setting error");
  errorsDispenserResponse[48] = F("Strange behavior of the program");
  errorsDispenserResponse[49] = F("");
  errorsDispenserResponse[50] = F("");
  /*valueOfCassettes[0] = ; valueOfCassettes[0] = ;
  valueOfCassettes[0] = ; valueOfCassettes[0] = ;
  valueOfCassettes[0] = ; valueOfCassettes[0] = ;
  */
}



/*
   This function is to convert all decimals to hexadecimals,
   because the acceptance just receive hexa values
*/

void decimalToHexa(uint16_t decimal, int len) {
  unsigned char high_byte = decimal >> 8;
  unsigned char low_byte = decimal & 0xFF;
  commands[len] = low_byte;
  commands[len + 1] = high_byte;
  delay(5);
}

/*
   CRC-16 calculator
*/

uint16_t crcCalcMain(uint16_t crc, uint8_t ch) {
  uint8_t quo;
  quo = (crc ^ ch) & 15;
  crc = (crc >> 4) ^ (quo * 4225);
  quo = (uint8_t)((crc ^ (ch >> 4)) & 15);
  crc = (crc >> 4) ^ (quo * 4225);
  return (crc);
}

/*
   This function is to set the crc of the data-frame
   @arg msg, is the byte that you will crc'ing
   @arg len, is the length of the message
*/

uint16_t crcCalc(byte* msg, int len) {
  uint16_t crc = 0;
  uint8_t ch;
  int i = 0;
  while (len-- > 0 ) {
    ch = (uint8_t)msg[i];
    crc = crcCalcMain(crc, ch);
    i++;
  }
  return (crc);
}


void commandStatus() {
  commands[0] = 0xFC;  // This commands be the same in all settings
  commands[1] = 0x05; commands[2] = SET_STATUS;
  decimalToHexa(crcCalc(commands, 3), 3);
}
void commandReset() {
  commands[0] = 0xFC;  // This commands be the same in all settings
  commands[1] = 0x05; commands[2] = SET_RESET;
  decimalToHexa(crcCalc(commands, 3), 3);
}
void commandAck() {
  commands[0] = 0xFC;  // This commands be the same in all settings
  commands[1] = 0x05; commands[2] = ACK;
  decimalToHexa(crcCalc(commands, 3), 3);
}
void commandEnable() {
  commands[0] = 0xFC;  // This commands be the same in all settings
  commands[1] = 0x07; commands[2] = ENABLE_DENOMINATION; commands[3] = 0x00; commands[4] = 0x00;
  decimalToHexa(crcCalc(commands, 5), 5);
}
void commandSecurity() {
  commands[0] = 0xFC;  // This commands be the same in all settings
  commands[1] = 0x07; commands[2] = SECURITY; commands[3] = 0x00; commands[4] = 0x00;
  decimalToHexa(crcCalc(commands, 5), 5);
}
void commandOptionaFunction() {
  commands[0] = 0xFC;  // This commands be the same in all settings
  commands[1] = 0x07; commands[2] = OPTIONAL_FUNCTION; commands[3] = 0x00; commands[4] = 0x00;
  decimalToHexa(crcCalc(commands, 5), 5);
}
void commandSetInhibit(bool enableFlag) {
  commands[0] = 0xFC;  // This commands be the same in all settings
  commands[1] = 0x06; commands[2] = INHIBIT;
  if (enableFlag == true) {
    commands[3] = 0x00;
  } else {
    commands[3] = 0x01;
  }
  decimalToHexa(crcCalc(commands, 4), 4);
}
void commandStack1() {
  commands[0] = 0xFC;  // This commands be the same in all settings
  commands[1] = 0x05; commands[2] = SET_STACK_1;
  decimalToHexa(crcCalc(commands, 3), 3);
}

//--------------------------------------------------------------------------------------

bool setInhibit(bool enable = false) {
  commandSetInhibit(enable);
  sendCmd(commands, (int)commands[1]); // change
  delay(200);
  readResponse();
  return getResponse(commands[2], ACK); // change
}

/*
   This function is to read the response of bill dispenser
*/

void readResponse() {
  int i = 0;
  int inByte = 0;
  clearResponse();
 
  if (Serial3.available()) {
    while (Serial3.available()) {
      inByte = Serial3.read();
      response[i] = inByte;
      delay(5);
     // #define DEBUG_METAVIX
#ifdef DEBUG_METAVIX
      Serial.print(response[i], HEX); Serial.print(' ');
#endif
      i++;
    } return;
  }
}


void readResponseDispenser() {
  debugMetavixln(F("Reading dispenser information"));
  int i = 0;
  int inByte = 0;
  clearResponse();
  delay(15);
  Serial2.flush();
  if (Serial2.available() > 0) {
    debugMetavixln("Response of dispenser: ");
    while (Serial2.available() > 0) {
      inByte = Serial2.read();
      response[i] = inByte;
      delay(5);
#ifdef DEBUG_METAVIX
      Serial.print(response[i], HEX);
#endif
      i++;
    } debugMetavixln(" :END"); 
    return;
  } else {
    delay(50);
    return;
  }
}




/*
   This function is to get the response of the bill acceptor
   and is to possible to compare the response with a selected message
*/

bool getResponse(int comparer , int comparer2) {
  int comparer1 = comparer;
  clearCommands();
#ifdef DEBUG_METAVIX
  Serial.print("Comparer: ");
  Serial.print(comparer, HEX);
  Serial.println();
  Serial.print("Comparer2: ");
  Serial.print(comparer2, HEX);
  Serial.println();
#endif
  if (comparer2 == comparer1)
    return true;
  return false;
}

void clearMessageSplited() {
  int i = 0;
  for (i = 0; i < 5; i++) {
    //debugMetavix(String(i));
    messageSplited[i] = " ";
  }
}


/*
   This function is to reset response array
*/

void clearResponse() {
  for (uint8_t i = 0; i < RESPONSE_SIZE; i++) {
    //debugMetavix(String(i));
    response[i] = 0x00;
  }
}

/*
   This function is to reset message array
*/

void clearMessage() {
  int i = 0;
  for (i = 0; i < MESSAGE_SIZE; i++) {
    //debugMetavix(String(i));
    message[i] = '\0';
  }
}


/*
   This function is to reset commands array
*/

void clearCommands() {
  for (uint8_t i = 1; i < COMMANDS_SIZE; i++) {
    //debugMetavix(String(i));
    commands[i] = 0x00;
  }
}





/*
   ----------------------------------------------------------------------------------------------------
   -------------------------------------- States implementation ---------------------------------------
   ----------------------------------------------------------------------------------------------------
*/

/*
   This state is to initialize the acceptance, there is a unique acceptance
   TBV-1000
*/
/*
   ------------------------------------------------------------------------------------
   ------------------------------- ACEPTANCE FUNCTIONS --------------------------------
   ------------------------------------------------------------------------------------
*/

void stateAcceptanceSendAck() {  // STATE_ACCEPTANCE_SEND_ACK
  debugMetavixln(F("Inside state send ACK"));
  commandAck();
  sendCmd(commands, (int)commands[1]);
  delay(100);
  state = STATE_ACCEPTANCE_STATUS;
}

void stateAcceptanceFatalError() {  // STATE_ACCEPTANCE_FATAL_ERROR
  int i = 0;
  for (i = 52; i < 62; ) {
    if (response[3] == statusResponseTable[i]) {
      Serial.print(F("ER:AP:FATAL:"));
      Serial.println(errorMessage[i - 29]);
    }
  }
  state = STATE_ACCEPTANCE_STATUS;
}


void stateAcceptanceReset() {  // STATE_ACCEPTANCE_RESET
  debugMetavixln(F("Inside Reset"));
  commandReset();
  sendCmd(commands, (int)commands[1]);
  delay(100);
  state = STATE_ACCEPTANCE_STATUS;
  return;
}


void stateAcceptanceStatus() {  // STATE_ACCEPTANCE_STATUS
  debugMetavixln(F("Inside status"));
  int k = 0;
  commandStatus();
  sendCmd(commands, (int)commands[1]);
  //while(!Serial3.available());
  //readResponse();
//  for (k = 0; k < 22; k++) {
//    if (response[2] == statusResponseTable[k]) {
//      break;
//    }
//  }
//  if (k > 10) {
//    Serial.print(F("ER:AP:"));
//    Serial.println(errorMessage[k - 11]);
//  }
//  debugMetavixln("The state is: " + String(k));
//  state = flagForResponseAction[k];
//  if (flagForPowerUp == true && response[2] == DISABLE) {
//    state = STATE_DISPENSER_INITIALIZE;
//    flagForPowerUp = true;
//  }
  state = STATE_DISPENSER_INITIALIZE;
  flagForPowerUp = true;
  return;
}



void stateAcceptanceStack() {  // STATE_ACCEPTANCE_STACK
  debugMetavixln(F("Inside stack"));
  volatile int i = 0;
  for (i = 62; i < 69; i++) {
    if (response[3] == statusResponseTable[i]) {
      unMoney = values[i - 62];
      delay(5);
    }
  }
  commandStack1();
  sendCmd(commands, (int)commands[1]);
  delay(100);
  readResponse();
  state = STATE_ACCEPTANCE_STATUS;
  return;
}

void stateAcceptanceGetData() {  // STATE_ACCEPTANCE_GET_DATA
  debugMetavixln(F("Inside get data"));
  volatile int i = 0;
  for (i = 41; i < 52; i++) {
    if (response[3] == statusResponseTable[i]) {
      Serial.print(F("ER:AP:"));
      Serial.println(errorMessage[i - 29]);
    }
  }
  state = STATE_ACCEPTANCE_STATUS;
  return;
}

void stateAcceptanceInitialize() {  // STATE_ACCEPTANCE_INITIALIZE
  debugMetavixln(F("Inside initialize"));
  commandEnable();
  sendCmd(commands, (int)commands[1]);
  delay(150);
  readResponse();
  if (!getResponse(commands[2], response[2]))
    return;
  commandSecurity();
  sendCmd(commands, (int)commands[1]);
  delay(100);
  readResponse();
  if (!getResponse(commands[2], response[2]))
    return;
  state = STATE_ACCEPTANCE_STATUS;
  return;
}

void stateAcceptanceVendValid() {  // STATE_ACCEPTANCE_VEND_VALID
  debugMetavixln(F("Inside state acceptance vend valid"));
  Serial.print("UN:AP:");
  Serial.println(String(unMoney));
  unMoney = 0;
  state = STATE_ACCEPTANCE_SEND_ACK;
}




//--------------------------------------------------------------------------------------

void stateWaittingForCommand() {  // STATE_WAITTING_FOR_COMMAND
  debugMetavixln(F("Inside state waiting for command"));
  clearMessageSplited();
  int i = 0;
  lastState = state;
  if (message[0] == '\0') {
    usbReceiver();
    //debugMetavix(message);
    delay(5);
    return;
  }
  char *p = message;
  char *str;
  while ((str = strtok_r(p, ":", &p)) != NULL) {
    messageSplited[i] = String(str);
    debugMetavixln(messageSplited[i]);
    i++;
  }
  if (messageSplited[1] == "START") {
    debugMetavixln(F("Order is START"));
    flagBills = 1;
    setInhibit(false);
    i = 0;
  } else {
    for (i = 1; i < 3; i++) {
      if (messageSplited[2] == orders[i])
        break;
    }
  }
  clearMessage();
  state = ordersState[i];
  return;
}


void stateEncrypt() {  // STATE_ENCRYPT
  flagForPowerUp = true;
  Serial.println(F("RC:OK:AP:WAIT"));
  state = STATE_ACCEPTANCE_STATUS;
  //state = STATE_DISPENSER_INITIALIZE;
  return;
}

void stateParsePcCommand() {  // STATE_PARSE_PC_CPMMAND
  if (messageSplited[3] == orders[3]) {
    Serial.println(F("RC:OK:DP:WAIT"));
    quantity = messageSplited[4].toInt();
    lastState = state;
    state = STATE_DISPENSER_COUNT_NOTES;
    return;
  }
}


void stateAcceptance() {  // STATE_ACCEPTANCE
  if (messageSplited[1] == F("ON")) {
    acceptFlag =true;
    Serial.println(F("RC:ON:AP"));
    //state = STATE_ACCEPTANCE_STATUS;
    clearMessageSplited();
    setInhibit(true);

    return;
  }
  if (messageSplited[1] == "OFF") {
    Serial.println(F("RC:OFF:AP"));
    clearMessageSplited();
    setInhibit(false);
    state = STATE_WAITTING_FOR_COMMAND;
    digitalWrite(ACTIVATOR_PIN_NAME, LOW);  // Activate coins
    acceptFlag =false;
    flagBills = 0;
    return;
  }
}

void stateDispenser() {

}

//--------------------------------------------------------------------------------------


void checkMessage() {
  if (Serial.available()) {
    state = STATE_WAITTING_FOR_COMMAND;
    return;
  }
}

void usbReceiver() {
  int i = 0;
  int inByte = 0;
  if (Serial.available() > 0) {
    while (Serial.available() > 0) {
      inByte = Serial.read();
      message[i] = inByte;
      delay(5);
      i++;
    }
  } else {
    delay(5);
    return;
  }
}


/*
   ----------------------------------------------------------------------
   ---------------------- DISPENSER FUNCTIONS ---------------------------
   ----------------------------------------------------------------------
*/






// Commands

/*
 *  This function is to initialize the cash dispenser and 
 *  remove all remain bills
 *
 */

void initialize() {
  commands[0] = STX;
  commands[1] = 0x09; commands[2] = 0x00; commands[3] = 0x00; commands[4] = DISPENSER_INITIALIZE;
  // LI                      LI                XN                CMD
  int i = 0;
  for (i = 5; i < 8; i++) {
    commands[i] = 0x44;
  }
  for (i = 8; i < 11; i++) {
    commands[i] = 0x41;
  }
  commands[i] = 0x0A;
  commands[i + 1] = ETX;
}
  
/*
 * This function is to dispense the requested notes, moves the cash to exit
 * and responds with the result
 * @arg response
 * @arg quantityBox1
 * @arg quantityBox2
 * @arg quantityBox3
 * @arg quantityBox4
 * @arg lenBox1
 * @arg lenBox2
 * @arg lenBox3
 * @arg lenBox4
 * 
 */

void dispenseCommand() {
  int i = 0;
  commands[0] = STX;
  commands[1] = 0x05; commands[2] = 0x00; commands[3] = 0x00; commands[4] = DISPENSER_DISPENSE;
  for (i = 5; i < 8; i++) {
    commands[i] = char(quantityBox[i - 5]);
  }
  commands[8] = ETX;
}

/*
   -------------------------------------------------------------------
   ----------------- STATE MACHINE DISPENSER -------------------------
   -------------------------------------------------------------------

   STATE_DISPENSER_COUNT_NOTES
   STATE_DISPENSER_SEND_COMMAND
   STATE_DISPENSER_GETTING_COMMAND
   STATE_DISPENSER_ANSWER_PC
   STATE_DISPENSER_INITIALIZE
   STATE_DISPENSER_PARSE_INFORMATION
   STATE_DISPENSER_PARSE_RESPONSE
*/

void stateDispenserCountNotes() {  // STATE_DISPENSER_COUNT_NOTES
  debugMetavixln(F("State counting money to dispense it"), 500);
  quantityBox[0] = 0; quantityBox[1] = 0; quantityBox[2] = 0;
  if (quantity < NOTE_BOTTOM) {
    quantityCoins = quantity;
    state = STATE_COUNT_MONEY_FOR_RETURN;
    return;
  }
  for (uint8_t i = 0; i < NUMBER_OF_CASSETTES; i++) {
    while (quantity >= noteCassette[i]) {
      quantity = quantity - noteCassette[i];
      quantityBox[i]++;
      delay(10);
    }
  }
  quantityCoins = quantity;  // para saber que monedas retornar
#ifdef DEBUG_METAVIX
  Serial.print(F("Quantity of box 1: ")); Serial.println(quantityBox[0]);
  Serial.print(F("Quantity of box 2: ")); Serial.println(quantityBox[1]);
  Serial.print(F("Quantity of box 3: ")); Serial.println(quantityBox[2]);
  delay(1000);
#endif
  lastState = state;
  state = STATE_DISPENSER_SEND_COMMAND;
  return;
}

void stateDispenserSendCommand() {  // STATE_DISPENSER_SEND_COMMAND
  debugMetavix(F("Inside dispenser send command"));
  delay(500);
  unsigned char c = 0x00;
  int i = 0;
  clearCommands();
  dispenseCommand();
  xorTable();
  delay(100);
  for (i = 4; i < 8; i++) {
    commands[i] = commands[i] ^ XN[i - 4];
  }
  for (i = 1; i < 9; i++) {
    c ^= commands[i];
  }
  commands[9] = c;
  Serial2.write(ENQ);
  delay(200);
  Serial2.write(commands, 10);
  delay(500);
  Serial2.write(commands, 10);
  delay(500);
  lastState = state;
  state = STATE_DISPENSER_GETTING_COMMAND;
  clearCommands();
  return;
}

void stateDispenserGettingCommand() {  // STATE_DISPENSER_GETTING_COMMAND
  debugMetavix(F("Inside getting command"));
  int i = 0;
  delay(500);
  readResponseDispenser();
  delay(250);
  if (response[0] == ASCII_ACK || response[0] == 0x00) {
    delay(200);
    sendEnq();
    delay(200);
    return;
  } else {
    xorTable();
    lastState = state;
    state = STATE_DISPENSER_PARSE_RESPONSE;
    delay(200);
    return;
  }
  return;
}

void sendEnq() {
  delay(200);
  Serial2.write(ASCII_ENQ);
  debugMetavix(F("Send ENQ"));
}

void stateDispenserAnswerPc() {  // STATE_DISPENSER_ANSWER_PC
  debugMetavix(F("Inside dispenser anser pc"));
  delay(500);
  int i = 0;
  Serial.print(F("TO:OK:DP:10000-")); Serial.print((int)response[21]);
  Serial.print(F(";2000-")); Serial.print((int)response[22]);
  Serial.print(F(";2000-")); Serial.println((int)response[23]);
  Serial.print(F("TO:BX:DP:10000-")); Serial.print((int)response[24]);
  Serial.print(F(";2000-")); Serial.print((int)response[25]);
  Serial.print(F(";2000-")); Serial.println((int)response[26]);
  for (i = 0; i < NUMBER_OF_CASSETTES; i++) {
    quantityBox[i] = 0;
  }
  lastState = state;
  state = STATE_COUNT_MONEY_FOR_RETURN; // wWaiting
  return;
}

void stateDispenserInitialize() {  // STATE_DISPENSER_INITIALIZE
  debugMetavix(F("Inside state dispenser initialize"));
  delay(500);
  unsigned char c = 0x00;
  int i = 0;
  initialize();
  delay(10);
  for (i = 4; i < 12; i++) {
    commands[i] = commands[i] ^ XN[i - 4];
  }
  for (i = 1; i < 13; i++) {
    c ^= commands[i];
  } commands[i] = c;
  Serial2.write(commands, 14);
  delay(250);
  lastState = state;
  Serial.println(F("RC:OK:DP:WAIT"));
  state = STATE_DISPENSER_GETTING_COMMAND;
  return;
}

void stateDispenserParseInformation() {  // STATE_DISPENSER_PARSE_INFORMATION
  debugMetavix(F("Inside parse information"));
  delay(500);
  int notesInformation[52];
  int i = 0;
  int rest[4];
  rest[0] = 0; rest[1] = 0; rest[2] = 0; rest[3] = 0;
  int desXoring = 0;
  desXoring = (int)response[3];
  delay(10);
  desXoring = desXoring - 1;
  for (i = 0; i < 48; i++) {
    if (desXoring + 1 > 63) {
      desXoring = 0;
    } else {
      desXoring = desXoring + 1;
    }
    response[i + 4] = response[i + 4] ^ XN[desXoring];
  }

  for (i = 0; i < 36; i++) {
    notesInformation[i] = (int)response[16 + i];
    delay(10);
  }

#ifdef DEBBUG_METAVIX
  Serial.print(F("Cassette dispensing quotes: 1: ")); Serial.print(notesInformation[5]); Serial.print(F(" 2: ")); Serial.print(notesInformation[6]); Serial.print(F(" 3: ")); Serial.println(notesInformation[7]);
  Serial.print(F("Cassette pick up quotes: 1: ")); Serial.print(notesInformation[8]); Serial.print(F(" 2: ")); Serial.print(notesInformation[9]); Serial.print(F(" 3: ")); Serial.println(notesInformation[10]);
  Serial.print(F("Cassette reject quotes: 1: ")); Serial.print(notesInformation[11]); Serial.print(F(" 2: ")); Serial.print(notesInformation[12]); Serial.print(F(" 3: ")); Serial.println(notesInformation[13]);
  Serial.print(F("Cassette ID Dispensing quotes: 1: ")); Serial.print(notesInformation[14]); Serial.print(F(" 2: ")); Serial.print(notesInformation[15]); Serial.print(F(" 3: ")); Serial.println(notesInformation[16]);
  Serial.print(F("Notes rejected because cutting: 1: ")); Serial.print(notesInformation[9]); Serial.print(F(" 2: ")); Serial.print(notesInformation[22]); Serial.print(F(" 3: ")); Serial.println(notesInformation[35]);
#endif
  rest[0] = (quantityBox[0] - notesInformation[5]);
  rest[1] = (quantityBox[1] - notesInformation[6]);
  rest[2] = (quantityBox[2] - notesInformation[7]);

  if (rest[0] == 0 && rest[1] == 0 && rest[2] == 0 && rest[3] == 0) {
    state = STATE_DISPENSER_ANSWER_PC;
    tryCatchDispense = 0;
    return;
  } else {
    tryCatchDispense++;
    if (tryCatchDispense < 1) {
      quantityBox[0] = rest[0];
      quantityBox[1] = rest[1];
      quantityBox[2] = rest[2];
      quantityBox[3] = rest[3];
      state = STATE_DISPENSER_SEND_COMMAND;
      return;
    } else {
      lastState = STATE_WAITTING_FOR_COMMAND;
      state = STATE_DISPENSER_ANSWER_PC;
      tryCatchDispense = 0;
      return;
    }
  }
}

void stateDispenserParseResponse() {  // STATE_DISPENSER_PARSE_RESPONSE
  debugMetavix(F("Inside of parse dispenser response"));
  delay(500);
  int i = 0;
  int desXoring = 0;
  desXoring = (int)response[3];
  delay(10);
  desXoring = desXoring - 1;
#ifdef DEBUG_METAVIX
  Serial.print("desxored message is: ");
#endif
  for (i = 0; i < 13; i++) {
    if (desXoring + 1 > 63) {
      desXoring = 0;
    } else {
      desXoring = desXoring + 1;
    }
    response[i + 4] = response[i + 4] ^ XN[desXoring];
#ifdef DEBUG_METAVIX
    Serial.print(response[i + 4], HEX);
    Serial.print(" ");
#endif
  }
  for (i = 0; i < 50; i++) {
#ifdef DEBUG_METAVIX
    Serial.print(F("The error from CDU is: "));
    Serial.println(response[5], HEX);
    Serial.print(F("The to parse is: "));
    Serial.println(errorsDispenser[i], HEX);
#endif
    if (response[5] == errorsDispenser[i])
      break;
  }
#ifdef DEBUG_METAVIX
  Serial.print(F("The number of error I is: "));
  Serial.println(i);
#endif
  lastState = state;
  state = STATE_DISPENSER_PARSE_INFORMATION;  // Go to get dispense information
  if (i > 0) {
    Serial.print(F("ER:DP:FATAL:"));
    Serial.println(errorsDispenserResponse[i]);
    return;
  } else {
    if (flagForPowerUp == true) {
      Serial.print(F("RC:OK:DP:HD:"));
      orders[3] = String(millis());
      Serial.println(orders[3]);
      state = STATE_WAITTING_FOR_COMMAND;
      flagForPowerUp = false;
      return;
    }
    if (i == 0) {
      state = STATE_DISPENSER_PARSE_INFORMATION;
      return;
    }
  }
  return;
}

/*
   ----------------------------------------------------------------------------
   -------------------------------- STATE COINS -------------------------------
   ----------------------------------------------------------------------------
*/

void stateCountMoneyForReturn() {  // STATE_COUNT_MONEY_FOR_RETURN Coins
  debugMetavixln(F("Inside count money for return"));
  uint32_t valueToReturn = 0;
  volatile int i = 0;
  //valueToReturn = (messageSplited[4].toInt()) % NOTE_BOTTOM;// viejo sistema de monedas solo sirve si no esta la pendejada mundial de billetes de 5000
  valueToReturn = quantityCoins;
  debugMetavix(F("Value to return is: ")); debugMetavixln(String(valueToReturn));
  if (errorFlag > 0) {
    debugMetavix(F("Inside errorFlag"));
    valueToReturn = 0;
    for (i = (errorFlag - 1); i < NUMBER_OF_HOPPERS; i++) {
      valueToReturn = valueToReturn + ((coinsToReturn[i] - coinsReturned[i]) * staticCoinValues[i]);
      coinsToReturn[i] = 0;
    }
  }
  debugMetavix(F("Count each coin for motor"));
  for (i = errorFlag; i < NUMBER_OF_HOPPERS; i++) {
    debugMetavix(F("I of for is: ")); debugMetavixln(String(i));
    while (valueToReturn >= staticCoinValues[i]) {
      valueToReturn = valueToReturn - staticCoinValues[i];
      coinsToReturn[i]++;
    }
  }
  state = STATE_TURN_ON_MOTORS;
  return;
}

void stateReadingCoin() {  // STATE_READING_COIN

}

void stateSendingMessageToPc() {  // STATE_SENDING_MESSAGE_TO_PC
  debugMetavixln(F("Inside sending message to pc"));
  volatile int i = 0;
  if (errorFlag > 0) {
    Serial.print(F("ER:MD:"));
  } else {
    Serial.print(F("TO:OK:MD:"));
  }
  for (i = 0; i < NUMBER_OF_HOPPERS; i++) {
    Serial.print(String(staticCoinValues[i] * 1)); Serial.print("-"); Serial.print(String(coinsReturned[i]));
    if (i < (NUMBER_OF_HOPPERS - 1))
      Serial.print(F(";"));
  } Serial.println();
  clearCoinsToReturnAndReturned();
  errorFlag = 0;
  state = STATE_WAITTING_FOR_COMMAND;
  return;
}

void stateTurnOnMotors() {  // STATE_TURN_ON_MOTORS
  debugMetavixln(F("Inside turn on motors"));
  int i = 0;
  int sensorState = 0;
  int timeout = 0;
  errorFlag = 0;
  for (i = 0; i < NUMBER_OF_HOPPERS; i++) {
    while ((timeout < TIMEOUT) && (coinsToReturn[i] > coinsReturned[i])) {
      debugMetavix(F("Turning on motor "), 1); debugMetavixln(String(i), 1);
      digitalWrite(pinRelayNames[i], HIGH);
      delay(1);
      sensorState = digitalRead(pinSensorNames[i]);
      if (sensorState != lastSensorState[i]) {
        if (sensorState == HIGH) { // Toma de activacion de sensor con antirebote
          timeout = 0;
          coinsReturned[i]++;
          //Serial.print("CoinsReturned is: "); Serial.println(String(coinsReturned[i]));
          //Serial.print("coinsToReturn is: "); Serial.println(String(coinsToReturn[i]));
        }
      }
      lastSensorState[i] = sensorState;
      timeout++;
    } delay(30);
    digitalWrite(pinRelayNames[i], LOW); // Desactivacion de motor
    if (timeout >= TIMEOUT) {
      errorFlag = i + 1;
      Serial.print("ER:MD: "); Serial.println(coinErrorMessage[i + 1]);
      state = STATE_COUNT_MONEY_FOR_RETURN;
      return;
    }delay(1000);
  }
  state = STATE_SENDING_MESSAGE_TO_PC;
  return;
}

void stateCountPulses() {  // STATE_COUNT_PULSES
  debugMetavixln(F("Inside count pulses"));  // previous state of the button
  uint8_t pulse_cont=0;
  uint32_t pulseLow_cont=0;
  digitalWrite(ACTIVATOR_PIN_NAME, HIGH); 
  unsigned long timeout = millis();
  while(!digitalRead(COUNTER_PIN_NAME)){
    if((millis()-timeout)>200)digitalWrite(ACTIVATOR_PIN_NAME, LOW);return;
  }
  digitalWrite(ACTIVATOR_PIN_NAME, LOW); 
  pulse_cont++;
  while(pulseLow_cont<100000){
    if(!digitalRead(COUNTER_PIN_NAME)){
      pulseLow_cont++;
    }else{
      while(digitalRead(COUNTER_PIN_NAME));      
      pulseLow_cont=0;
      pulse_cont++;
    } 
  } 
  sendValueCoin(pulse_cont); 
  delay(50);
  digitalWrite(ACTIVATOR_PIN_NAME, LOW);return;
}

/*
   ----------------------------------------------------------------------------
   -------------------------- END COINS STATE ---------------------------------
   ----------------------------------------------------------------------------
*/

/*
   This function is to set all variables to the orgin value
*/

void setPrdefinedConfiguration() {
  volatile int i = 0;
  initializePinSensorNames();
  initializePinRelayNames();
  initializeStaticCoinValues();
  clearMessage();
  clearMessageSplited();
  fillErrorArray();
  motorFlag = 0;
#if defined(PULSE_COMMANDS)
  pinMode(COUNTER_PIN_NAME, INPUT_PULLUP);
#endif

#if defined(HEXA_COMMANDS)
  mySerial.begin(9600);
#endif
  pinMode(ACTIVATOR_PIN_NAME, OUTPUT);
  digitalWrite(ACTIVATOR_PIN_NAME, LOW);
  for (i = 0; i < NUMBER_OF_HOPPERS; i++) {
    pinMode(pinSensorNames[i], INPUT_PULLUP);
    pinMode(pinRelayNames[i], OUTPUT);
    digitalWrite(pinRelayNames[i], LOW);
    hopperQuantity[i] = 0;
    errorFlag = 0;
    lastSensorState[i] = digitalRead(pinSensorNames[i]);
    coinsToReturn[i] = 0;
  }
}

void fillErrorArray() {
  coinErrorMessage[0] = F("NORMAL");
  coinErrorMessage[1] = F("Empty or jam in 1 motor");
  coinErrorMessage[2] = F("Empty or jam in 2 motor");
  coinErrorMessage[3] = F("Empty or jam in 3 motor");
  coinErrorMessage[4] = F("Abnormal Near End sensor of 1 motor");
  coinErrorMessage[5] = F("Abnormal Near End sensor of 2 motor");
  coinErrorMessage[6] = F("Abnormal Near End sensor of 3 motor");
  coinErrorMessage[7] = F("Coin acceptor box loading error");
  coinErrorMessage[8] = F("Near to be full");
}

/*
   This function is to read each value of coin by pulses,
   in addition it has a return, it is to build an error control

   @arg pulses it is the quantity of pulses to know the value of coin
*/

void sendValueCoin(int pulses) {
  if (pulses == 5) {
    Serial.println("UN:MA:500");
    return;
  }
  if (pulses == 4) {
    Serial.println("UN:MA:200");
    return;
  }
  if (pulses == 3) {
    Serial.println("UN:MA:100");
    return;
  }
  
}

void initializePinSensorNames() {
  pinSensorNames[0] = SENSOR_ONE_PIN_NAME;
  pinSensorNames[1] = SENSOR_TWO_PIN_NAME;
  pinSensorNames[2] = SENSOR_THREE_PIN_NAME;
}

void initializePinRelayNames() {
  pinRelayNames[0] = RELAY_ONE_PIN_NAME;
  pinRelayNames[1] = RELAY_TWO_PIN_NAME;
  pinRelayNames[2] = RELAY_THREE_PIN_NAME;
}

void initializeStaticCoinValues() {
  staticCoinValues[0] = HOPPER_ONE;
  staticCoinValues[1] = HOPPER_TWO;
  staticCoinValues[2] = HOPPER_THREE;
}
void clearCoinsToReturnAndReturned() {
  volatile int i = 0;
  for (i = 0; i < NUMBER_OF_HOPPERS; i++) {
    coinsToReturn[i] = 0;
    coinsReturned[i] = 0;
  }
}

/*
   ----------------------------------------------------------------------------
   --------------------------- COINS GENERAL ----------------------------------
   ----------------------------------------------------------------------------
*/


/*
   ----------------------------------------------------------------------------
   ------------------------ CRC of dispenser ----------------------------------
   ----------------------------------------------------------------------------
*/

unsigned short Calculate_CRC_CCITT(const unsigned char* buffer, int size) {
  unsigned short tmp;
  unsigned short crc = 0xffff;
  for (int i = 0; i < size ; i++) {
    tmp = (crc >> 8) ^ buffer[i];
    crc = (crc << 8) ^ CRC_CCITT_TABLE[tmp];
  }
  return crc;
}

void decimalToHexaDispenser(uint16_t decimal, int len) {
  int high_byte = (decimal >> 8) & 0xff;
  int low_byte = decimal & 0xff;
  commands[len + 1] = char(low_byte);
  commands[len] = char(high_byte);
  delay(5);
}

void sendAck() {
  Serial2.write(ASCII_ACK);
  debugMetavix(F("Send ACK"));
}


void intToBytes(int number, int arrayPosition) {
  for (int i = 2; i >= 0; i--) {
    commands[i + arrayPosition] = (byte) (number % 10) + '0';
    number = number / 10;
  }
}

void debugMetavix(String msg, int temp) {
  if (debugFlag) {
    Serial.print(msg);
    delay(temp);
  }
}
void debugMetavixln(String msg, int temp) {
  if (debugFlag) {
    Serial.println(msg);
    delay(temp);
  }
}


void xorTable() {
  XN[0] = 0x57;  XN[8] = 0x58;  XN[16] = 0x59;  XN[24] = 0x60;  XN[32] = 0x61;  XN[40] = 0x62;  XN[48] = 0x63;  XN[56] = 0x64;
  XN[1] = 0x25;  XN[9] = 0x26;  XN[17] = 0x27;  XN[25] = 0x28;  XN[33] = 0x29;  XN[41] = 0x30;  XN[49] = 0x31;  XN[57] = 0x32;
  XN[2] = 0x49;  XN[10] = 0x50;  XN[18] = 0x51;  XN[26] = 0x52;  XN[34] = 0x53;  XN[42] = 0x54;  XN[50] = 0x55;  XN[58] = 0x56;
  XN[3] = 0x17;  XN[11] = 0x18;  XN[19] = 0x19;  XN[27] = 0x20;  XN[35] = 0x21;  XN[43] = 0x22;  XN[51] = 0x23;  XN[59] = 0x24;
  XN[4] = 0x41;  XN[12] = 0x42;  XN[20] = 0x43;  XN[28] = 0x44;  XN[36] = 0x45;  XN[44] = 0x46;  XN[52] = 0x47;  XN[60] = 0x48;
  XN[5] = 0x09;  XN[13] = 0x10;  XN[21] = 0x11;  XN[29] = 0x12;  XN[37] = 0x13;  XN[45] = 0x14;  XN[53] = 0x15;  XN[61] = 0x16;
  XN[6] = 0x33;  XN[14] = 0x34;  XN[22] = 0x35;  XN[30] = 0x36;  XN[38] = 0x37;  XN[46] = 0x38;  XN[54] = 0x39;  XN[62] = 0x40;
  XN[7] = 0x01;  XN[15] = 0x02;  XN[23] = 0x03;  XN[31] = 0x04;  XN[39] = 0x05;  XN[47] = 0x06;  XN[55] = 0x07;  XN[63] = 0x08;
}
