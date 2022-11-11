#include <WiFi.h>
#include <WiFiClient.h>
#include <WebServer.h>
#include <ESPmDNS.h>
#include <EEPROM.h>
#include <AsyncUDP.h>

#define LED 15

AsyncUDP udp;

bool stav = false;
int treshold = 25;
int value = 0;
bool result = false;

bool connected = false;

String wifiMode = "none";

char ssid[50]= "cedule";
char password[50] = "password";

int opakovani = 0;

IPAddress ip;
IPAddress localIp (192,168,0,1);
IPAddress subnet (255,255,255,0);
IPAddress gateWay (192,168,0,1);

IPAddress broadcast (192,168,0,2);

WebServer server(8081);

int mem_ssid = 0;
int mem_pass = 0;
int mem_mode = 0;
int mem_value = 0;
int mem_tresh = 0;

void handleRoot() {

  String message = Execute("status","");

  for (uint8_t i = 0; i < server.args(); i++)
      message = Execute(server.argName(i), server.arg(i));

  server.send(200, "text/plain", message);
}

bool InitWifi() {
  bool result = false;

  if (strlen(ssid) == 0)
    Serial.println(GetErrorMessage(1));
  else {
    if (strlen(password) == 0)
      Serial.println(GetErrorMessage(2));
    else
      result = true;
  }

  return result;
}

bool Connect() 
{
  if (wifiMode == "client")
      result = ClientMode();
  else if(wifiMode == "ap")
      result = APMode();      

  connected = result;
  return result;
}

bool ClientMode()
{

  bool result = false;

  Serial.println();
  Serial.println();
  Serial.print("Pripojovani k ");
  Serial.println(ssid);
  Serial.print("Heslo ");
  Serial.println(password);

  WiFi.begin(ssid, password);

  opakovani = 0;
  delay(1000);
  
  while (WiFi.status() != WL_CONNECTED && opakovani < 3) {
    delay(500);
    Serial.print(".");
    WiFi.begin(ssid, password);
    opakovani++;
  }

  if (opakovani == 3) {
    Serial.println("");
    Serial.println("Nelze se pripojit k siti.");
  } else {
      ip = WiFi.localIP();
      Serial.println("");
      Serial.println("WiFi pripojena");
      Serial.println("IP adresa: ");
      Serial.println(ip);
      result = true;
  }

  return result;

}

bool APMode()
{

  Serial.println();
  Serial.println();
  Serial.print("Vytvoreni site ");
  Serial.println(ssid);
  Serial.print("Heslo ");
  Serial.println(password);

  WiFi.mode(WIFI_AP);

  WiFi.softAPConfig(localIp, gateWay, subnet);

  result = WiFi.softAP(ssid,password);

  if (result)
    ip = localIp;
  else
    ip = {0,0,0,0};

  Serial.println("IP adresa: ");
  Serial.println(ip);

  return result;
  
}

bool StartHttp() {
  
  if (MDNS.begin("esp32")) {
    Serial.println("MDNS responder started");

    server.on("/", handleRoot);

    server.on("/inline", []() {
      server.send(200, "text/plain", "this works as well");
    });

    server.onNotFound(handleNotFound);

    server.begin();
    Serial.println("HTTP server started");

    return true;
  }
}

void handleNotFound() {

  String message = "File Not Found\n\n";
  message += "URI: ";
  message += server.uri();
  message += "\nMethod: ";
  message += (server.method() == HTTP_GET) ? "GET" : "POST";
  message += "\nArguments: ";
  message += server.args();
  message += "\n";
  for (uint8_t i = 0; i < server.args(); i++) {
    message += " " + server.argName(i) + ": " + server.arg(i) + "\n";
  }
  server.send(404, "text/plain", message);
}

void setup() {

  pinMode(LED, OUTPUT);
  Serial.begin(115200);

  EEPROM.begin(1024);

  CalculateMemoryOffset();
  LoadSettings();

  digitalWrite(LED, true);

  if (InitWifi())
    if (Connect())
    {
      /*udp.connect(IPAddress(255,255,255,255), 8061);*/
      GetBroadcast();
      StartHttp();
    }
}

void loop() {
  
  Blinking();
  ReceiveSerial();
  server.handleClient();

  Display();
  delay(10);  
}

void CallBroadcast()
{
  //udp.broadcast("tralala");
  Serial.println("Broadcasting...");
}

void GetBroadcast()
{
  if(udp.listen(8061)) {
  udp.onPacket([](AsyncUDPPacket packet) {
            Serial.print("UDP Packet Type: ");
            Serial.print(packet.isBroadcast()?"Broadcast":packet.isMulticast()?"Multicast":"Unicast");
            Serial.print(", From: ");
            Serial.print(packet.remoteIP());
            Serial.print(":");
            Serial.print(packet.remotePort());
            Serial.print(", To: ");
            Serial.print(packet.localIP());
            Serial.print(":");
            Serial.print(packet.localPort());
            Serial.print(", Length: ");
            Serial.print(packet.length());
            Serial.print(", Data: ");
            Serial.write(packet.data(), packet.length());
            Serial.println();
            //reply to the client
            packet.printf("Got %u bytes of data", packet.length());
        });
  }
}

void Display()
{
  if (value >= treshold)
    result = true;
  else  
    result = false;
}

unsigned long currTime = 0;
unsigned long lastTime = 0;

void Blinking()
{
  unsigned long _delay = 0;

 if (!connected)
      _delay = 200;
    else
      _delay = 500;

  currTime = millis();

  if (currTime  - lastTime > _delay)
  {
    stav = !stav;
    digitalWrite(LED, stav);
    lastTime = currTime;
    
    /*
    if (connected && stav)
      GetBroadcast();
      */
  }

}

void CalculateMemoryOffset()
{
  mem_ssid = 0;
  mem_pass = mem_ssid + 32 + 1;
  mem_mode = mem_pass + 64 + 1;
  mem_value = mem_mode + 6 + 1;
  mem_tresh = mem_value + 4 + 1;
}

void LoadSettings()
{
   String eepromValue = "";

     //SSID
   eepromValue = readStringFromEEPROM(mem_ssid);
   eepromValue.toCharArray(ssid, eepromValue.length()+1);
   Serial.println(ssid);

     //Password
   eepromValue = readStringFromEEPROM(mem_pass);
   eepromValue.toCharArray(password, eepromValue.length()+1);
   Serial.println(password);
   
     //Wifi Mode
   eepromValue = readStringFromEEPROM(mem_mode);
   wifiMode = eepromValue;
   Serial.println(wifiMode);

     //Value
   eepromValue = readStringFromEEPROM(mem_value);
   value = eepromValue.toInt();
   Serial.println(value);

     //Treshold
   eepromValue = readStringFromEEPROM(mem_tresh);
   treshold = eepromValue.toInt();
   Serial.println(treshold);

}

String GetErrorMessage(int errCode) 
{
  String errMsg = "(";
  errMsg.concat(String(errCode));
  errMsg.concat(") ");

  switch (errCode) {

    case 1:
      errMsg.concat("Missing WiFi SSID");
      break;
    case 2:
      errMsg.concat("Missing WiFi password");
      break;
    case 3:
      errMsg.concat("Invalid command");
      break;
    case 4:
      errMsg.concat("Missing argument");
      break;
    case 5:
      errMsg.concat("Unknown command");
      break;
    default:
      errMsg.concat("Unknown error code");
      break;
  }

  return errMsg;
}

void ReceiveSerial()
{
  String strReceived = "";

  if (Serial.available() > 0)
  {
    digitalWrite(LED, true);
    strReceived = Serial.readStringUntil('\n'); //
    digitalWrite(LED, false);

    Deserialize(strReceived);
  }
}

void Deserialize(String message)
{
  int index = -1;
  String command = "";
  String argument = "";
  
  if (message.length() > 0)
  {
    index = message.indexOf(' ');
    
    if (index > 0)
    {
      argument = message.substring(index+1);
      command = message.substring(0, index);
    }
    else 
    {
      command = message;
    }

    Execute(command, argument);
  }
  else
    Serial.println(GetErrorMessage(3));
}

String Execute(String command, String argument)
{

  String rtnMsg = "";
  char buf[50];
  int argLen = 0;
  argLen = argument.length();

  command.toLowerCase();
  argument.toCharArray(buf, argument.length()+1);

  if (command.equals("ssid"))
  {
    if (argLen > 0)
    {
      argument.toCharArray(ssid, argument.length()+1);
      writeStringToEEPROM(mem_ssid, argument);
    }

    rtnMsg = "SSID is set to: ";
    rtnMsg.concat(ssid);
  }
  else if (command.equals("pass"))
  {
    if (argLen > 0)
    {
      argument.toCharArray(password, argument.length()+1);
      writeStringToEEPROM(mem_pass, argument);
    }

    rtnMsg = "Password is set to: ";
    rtnMsg.concat(password);
  }
   else if (command.equals("treshold"))
  {
    if (argLen > 0)
    if (StringIsNumber(argument))
    {
      treshold = argument.toInt();
      writeStringToEEPROM(mem_tresh, argument);
    }

    rtnMsg = "Treshold is set to: ";
    rtnMsg.concat(String(treshold));
  }
  else if (command.equals("value"))
  {
    if (argLen > 0)
      if (StringIsNumber(argument))
      {
        value = argument.toInt();
        writeStringToEEPROM(mem_value, argument);
      }

    rtnMsg = "Value is set to: ";
    rtnMsg.concat(String(value));
  }
  else if (command.equals("add") || command.equals("plus") || command.equals("+"))
  {
    value = value + 1;

    writeStringToEEPROM(mem_value, String(value));

    rtnMsg = "Value is set to: ";
    rtnMsg.concat(String(value));
  }
  else if (command.equals("deduct") || command.equals("minus") || command.equals("-"))
  {
    value = value - 1;

    writeStringToEEPROM(mem_value, String(value));

    rtnMsg = "Value is set to: ";
    rtnMsg.concat(String(value));
  }
  else if (command.equals("connect"))
  {
    if (InitWifi())
      Connect();
  }
  else if (command.equals("disconnect"))
  {
    WiFi.disconnect();
    connected = false;
    rtnMsg = "Disconnected";
  }
  else if (command.equals("wifimode"))
  {
    writeStringToEEPROM(mem_mode, argument);
    wifiMode = argument;
    rtnMsg = "Value is set to: ";
    rtnMsg.concat(String(wifiMode));
  }

  else if (command.equals("load"))
  {
    LoadSettings();
  }

  else if(command.equals("status"))
  {
    rtnMsg  = "conn:";
    rtnMsg.concat(String(connected));
    rtnMsg.concat("|wifimode:");
    rtnMsg.concat(wifiMode);
    rtnMsg.concat("|ssid:");
    rtnMsg.concat(ssid);
    rtnMsg.concat("|password:");
    rtnMsg.concat(password);
    rtnMsg.concat("|ip:");
    rtnMsg.concat(String(ip[0]));
    rtnMsg.concat(".");
    rtnMsg.concat(String(ip[1]));
    rtnMsg.concat(".");
    rtnMsg.concat(String(ip[2]));
    rtnMsg.concat(".");
    rtnMsg.concat(String(ip[3]));
    rtnMsg.concat("|mac:");
    rtnMsg.concat(String(WiFi.macAddress()));
    rtnMsg.concat("|treshold:");
    rtnMsg.concat(String(treshold));
    rtnMsg.concat("|value:");
    rtnMsg.concat(String(value));
    rtnMsg.concat("|result:");
    rtnMsg.concat(String(result));
  }
  else
  {
    rtnMsg = GetErrorMessage(5);
  }
  Serial.println(rtnMsg);
  return rtnMsg;
}

bool StringIsNumber(String inputString)
{
  bool retVal = true;

  for (int strPos = 0; strPos < inputString.length(); strPos++)
    if (!isDigit(inputString.charAt(strPos)))
      retVal = false;

  return retVal;
}

void writeStringToEEPROM(int addrOffset, const String &strToWrite)
{
  byte len = strToWrite.length();
  EEPROM.write(addrOffset, len);
  
  for (int i = 0; i < len; i++)
  {
    EEPROM.write(addrOffset + 1 + i, strToWrite[i]);
  }

  EEPROM.commit();

}

String readStringFromEEPROM(int addrOffset)
{
  int newStrLen = EEPROM.read(addrOffset);

  Serial.println(String(newStrLen));

  char data[newStrLen + 1];
  for (int i = 0; i < newStrLen; i++)
  {
    data[i] = EEPROM.read(addrOffset + 1 + i);
  }
  
  data[newStrLen] = '\0';
  return String(data);
}