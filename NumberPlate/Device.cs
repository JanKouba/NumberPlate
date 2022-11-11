using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Net.Http;

namespace NumberPlate
{

    public delegate void SerialPort_DataReceived(object sender, DeviceEventArgs e);

    public interface Device
    {
        void Connect(string comPort);
        
        void GetStatus();

        void Plus();
        void Minus();
        void SetValue(string value);
        void SetTreshold(string treshold);

        event EventHandler MessageReceived;
    }

    public class DeviceSerial : Device
    {
        SerialPort serialPort = new SerialPort();

        public event EventHandler? MessageReceived;
        string message = String.Empty;

        bool busy = false;

        public DeviceSerial(string comPort)
        {
            Connect(comPort);
        }
       
        public void Connect(string comPort)
        {
            try
            {
                serialPort.PortName = comPort;
                serialPort.BaudRate = 115200;
                serialPort.Parity = Parity.None;
                serialPort.StopBits = StopBits.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = Handshake.None;
                serialPort.Encoding = ASCIIEncoding.ASCII;
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;

                serialPort.DataReceived += SerialPort_DataReceived;

                serialPort.Open();

                message = "Port is Open" + Environment.NewLine;

                GetStatus();
            }
            catch (Exception ex)
            {
                message = ex.Message + Environment.NewLine;
            }

            MessageReceived?.Invoke(this, new DeviceEventArgs(message));
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            message = serialPort.ReadLine();
            MessageReceived?.Invoke(this, new DeviceEventArgs(message));
            
        }

        public void WifiConnect()
        {
            serialPort.Write("connect");
        }

        public void WifiDisonnect()
        {
            serialPort.Write("disconnect");
        }

        public void WifiSetMode(WifiMode mode)
        {
            if (!busy)
            { 
                if (mode == NumberPlate.WifiMode.APMode)
                    serialPort.Write("wifimode ap");
                if (mode == NumberPlate.WifiMode.ClientMode)
                    serialPort.Write("wifimode client");
            }
        }

        public void GetStatus()
        {
            if (!busy)
                serialPort.Write("status");

        }

        public void Plus()
        {
            serialPort.Write("plus");
        }

        public void Minus()
        {
            serialPort.Write("minus");
        }

        public void SetValue(string value)
        {
            serialPort.Write("value " + value);
        }

        public void SetTreshold(string treshold)
        {
            serialPort.Write("treshold " + treshold);
        }

        public void SetSSID(string ssid)
        {
            serialPort.Write("ssid " + ssid);
        }

        public void SetPassword(string password)
        {
            serialPort.Write("pass " + password);
        }
    }

    public class DeviceWeb : Device
    {
        public event EventHandler? MessageReceived;
        string message = String.Empty;

        private string ipaddress;
        public string IpAddress { get { return ipaddress; } }

        string webAddress = string.Empty;

        public void Connect(string comPort) { }

        public DeviceWeb(string ip)
        {
            ipaddress = ip;
            webAddress = String.Format("{0}{1}{2}", "http://", ipaddress, ":8081");
        }

        public async void GetStatus()
        {
            await Get("?status=0");
        }

        public async void Plus()
        {
            await Get("?plus=0");
            await Get("?status");

        }

        public async void Minus()
        {
            await Get("?minus=0");
            await Get("?status");

        }

        public async void SetTreshold(string treshold)
        {
            await Get("?treshold=" + treshold);
            await Get("?status");

        }

        public async void SetValue(string value)
        {
            await Get("?value=" + value);
            await Get("?status");

        }

        private async Task Get(string functionName)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(webAddress + functionName);

            if (response.IsSuccessStatusCode)
            {
                message = await response.Content.ReadAsStringAsync();
                MessageReceived?.Invoke(this, new DeviceEventArgs(message + "\r"));
            }
        }
    }

    public class DeviceEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public DeviceEventArgs(string message)
        {
            Message = message;
        }
    }

    public enum WifiMode
    {
        APMode,
        ClientMode
    }
}
