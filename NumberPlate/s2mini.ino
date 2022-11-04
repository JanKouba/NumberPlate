#include <WiFi.h>
#include <WiFiClient.h>
#include <WebServer.h>
#include <ESPmDNS.h>

#define LED 15

bool stav = false;
int treshold = 25;
int value = 0;
bool result = false;

bool connected = false;

char ssid[50]= "netis_78B5E8";
char password[50] = "password";

int opakovani = 0;

IPAddress ip;

WebServer server(8081);

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

bool Connect() {

  bool result = false;

  Serial.println();
  Serial.println();
  Serial.print("Pripojovani k ");
  Serial.println(ssid);
  Serial.print("Heslo ");
  Serial.println(password);

  WiFi.begin(ssid, password);

  opakovani = 0;

  while (WiFi.status() != WL_CONNECTED && opakovani < 3) {
    delay(500);
    Serial.print(".");
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

  connected = result;
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

  digitalWrite(LED, true);

  if (InitWifi())
    if (Connect())
      StartHttp();
}

void loop() {
  
  Blinking();
  ReceiveSerial();
  server.handleClient();

  Display();
  delay(10);  
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
  }

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
    }

    rtnMsg = "SSID is set to: ";
    rtnMsg.concat(ssid);
  }
  else if (command.equals("pass"))
  {
    if (argLen > 0)
    {
      argument.toCharArray(password, argument.length()+1);
    }

    rtnMsg = "Password is set to: ";
    rtnMsg.concat(password);
  }
   else if (command.equals("treshold"))
  {
    if (argLen > 0)
    if (StringIsNumber(argument))
      treshold = argument.toInt();

    rtnMsg = "Treshold is set to: ";
    rtnMsg.concat(String(treshold));
  }
  else if (command.equals("value"))
  {
    if (argLen > 0)
      if (StringIsNumber(argument))
        value = argument.toInt();

    rtnMsg = "Value is set to: ";
    rtnMsg.concat(String(value));
  }
  else if (command.equals("add") || command.equals("plus") || command.equals("+"))
  {
    value = value + 1;

    rtnMsg = "Value is set to: ";
    rtnMsg.concat(String(value));
  }
  else if (command.equals("deduct") || command.equals("minus") || command.equals("-"))
  {
    value = value - 1;

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
  else if(command.equals("status"))
  {
    rtnMsg  = "conn:";
    rtnMsg.concat(String(connected));
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