using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;


namespace NumberPlate
{
    public class Socket
    {
        public event EventHandler DataReceived;

        UdpClient udpClient = new UdpClient();
        int port = 8061;

        public string ReceivedData = "";

        private string partnerIP;

        public string PartnerIP
        {
            get { return partnerIP; }
            set { partnerIP = value; }
        }

        public void NewDataRecieved(EventHandler inputHandler)
        {
            EventHandler handler = inputHandler;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public Socket()
        {
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

            Task.Run(() =>
            {
                RecvData();
            });
        }

        private void RecvData()
        {
            while (true)
            {
                bool ignoreData = false;

                var from = new IPEndPoint(IPAddress.Any, port);

                var recvBuffer = udpClient.Receive(ref from);
                ReceivedData = Encoding.UTF8.GetString(recvBuffer);

                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                    if (from.Address.ToString() == ip.ToString() || from.Address.AddressFamily == AddressFamily.InterNetwork)
                        ignoreData = true;

                //if (!ignoreData)
                NewDataRecieved(DataReceived);

                ignoreData = false;
            }
        }

        public void SendData(string Data)
        {
            var data = Encoding.UTF8.GetBytes(Data);
            udpClient.Send(data, data.Length, partnerIP, port);
        }


    }
}
