using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using ToggleSwitch;


namespace NumberPlate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Device device;
        ConnectionType connectionType = ConnectionType.None;
        Settings settings = new Settings();

        int langCode = 1029;

        public MainWindow()
        {
            InitializeComponent();
            GetPorts();
            SetLanguage();

            Socket s = new Socket();
        }

        private void SetLanguage()
        {
            ResourceDictionary dict = new ResourceDictionary();
            switch (langCode)
            {
                case 1033:
                    dict.Source = new Uri("..\\Lang\\Dict-en.xaml", UriKind.Relative);
                    break;
                case 1029:
                    dict.Source = new Uri("..\\Lang\\Dict-cz.xaml", UriKind.Relative);
                    break;
            }
            this.Resources.MergedDictionaries.Add(dict);

        }

        private void SaveSettings()
        {
            settings.SSID = "nazev_site";
            settings.Password = "zmrde";

            XmlSerializer writer = new XmlSerializer(settings.GetType());

            using (StreamWriter file = new StreamWriter("data.xml"))
            {
                writer.Serialize(file, settings);
            }
        }

        private void BusyChange(bool busy)
        {
            this.Dispatcher.Invoke(new Action(()=> { 

            if (busy)
            {
                viewboxNonWork.Visibility = Visibility.Visible;
                buttonConnectWifi.IsEnabled = false;
                buttonDisconnectWifi.IsEnabled = false;
                buttonSetSsid.IsEnabled = false;
                buttonSetPassword.IsEnabled = false;
                buttonGetStatus.IsEnabled = false;
                buttonMinus.IsEnabled = false;
                buttonPlus.IsEnabled = false;
                buttonSetTreshold.IsEnabled = false;
                buttonSetValue.IsEnabled = false;
            }
            else 
            {
                
                viewboxNonWork.Visibility = Visibility.Hidden;
                if (connectionType == ConnectionType.Serial)
                {
                    buttonConnectWifi.IsEnabled = true;
                    buttonDisconnectWifi.IsEnabled = true;
                    buttonSetSsid.IsEnabled = true;
                    buttonSetPassword.IsEnabled = true;
                }
                buttonGetStatus.IsEnabled = true;
                buttonMinus.IsEnabled = true;
                buttonPlus.IsEnabled = true;
                buttonSetTreshold.IsEnabled = true;
                buttonSetValue.IsEnabled = true;
            }
            }
            ));
        }

        private void GetPorts()
        {
            var portNames = SerialPort.GetPortNames();
            comboBoxComPort.Items.Clear();

            comboBoxComPort.Items.Add("Wi-Fi");

            foreach (var port in portNames)
            { 
                comboBoxComPort.Items.Add(port);
                comboBoxComPort.SelectedIndex = 0;
            }
        }

        private void Device_MessageReceived(object? sender, EventArgs e)
        {
            string message = ((DeviceEventArgs)e).Message;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBoxLog.AppendText(String.Format("{0}{1}"
                           , DateTime.Now.ToString("[HH:mm:ss.fff] ")
                           , message));

                textBoxLog.ScrollToEnd();
            }));

            DeserializeMessage(message);

            //In case of some settigns changed, call Status
            if (message.Contains("is set to:") && connectionType == ConnectionType.Serial)
                device.GetStatus();
            else
                BusyChange(false);
        }

        void DeserializeMessage(string message)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                string macAddress = string.Empty;

                string[] messagePart = message.Split('|');
                int messageLength = messagePart.Length;

                int currentMessagePart = 0;

                while (currentMessagePart < messageLength)
                {
                    string[] messagePartSub = messagePart[currentMessagePart].Split(':');

                    string messageLabel = messagePartSub[0].Trim('\r');

                    switch (messageLabel)
                    {
                        case "conn":
                            if (messagePartSub[1] == "1")
                            {
                                labelWifi.Style = (Style)Resources["LabelConnected"];
                                labelWifi.Content = "Wi-Fi avaliable";
                            }
                            if (messagePartSub[1] == "0")
                            {
                                labelWifi.Style = (Style)Resources["LabelDisconnected"];
                                labelWifi.Content = "Wi-Fi unavaliable";
                            }
                            break;
                        case "ssid":
                            labelWifiSsid.Content = messagePartSub[1];
                            break;
                        case "password":
                            labelWifiPass.Content = messagePartSub[1];
                            break;
                        case "wifimode":
                            if (messagePartSub[1] == "ap")
                                checkBoxWifiMode.IsChecked = true;
                            if (messagePartSub[1] == "client")
                                checkBoxWifiMode.IsChecked = false;
                            break;
                        case "ip":
                            labelIpAddress.Content = messagePartSub[1];
                            break;
                        case "mac":
                            macAddress = String.Join(":", messagePartSub[1..]);
                            labelMacAddress.Content = macAddress;
                            break;
                        case "result":
                            if (messagePartSub[1].Trim('\r') == "1")
                                labelValue.Style = (Style)Resources["Label7SegmentGreen"];
                            if (messagePartSub[1].Trim('\r') == "0")
                                labelValue.Style = (Style)Resources["Label7SegmentRed"];
                            break;
                        case "value":
                            labelValue.Content = messagePartSub[1];
                            break;
                        case "treshold":
                            labelTreshold.Content = "Treshold: " + messagePartSub[1];
                            break;
                        case "Disconnected":
                            labelWifi.Style = (Style)Resources["LabelDisconnected"];
                            labelWifi.Content = "Wi-Fi unavaliable";
                            break;
                    }

                    currentMessagePart++;
                }
            }
            )
            );
        }

        private void buttonConnectWifi_Click(object sender, RoutedEventArgs e)
        {
            BusyChange(true);
            ((DeviceSerial)device).WifiConnect();
        }

        private void buttonDisconnectWifi_Click(object sender, RoutedEventArgs e)
        {
            BusyChange(true);
            ((DeviceSerial)device).WifiDisonnect();
        }

        private void buttonGetStatus_Click(object sender, RoutedEventArgs e)
        {
            BusyChange(true);
            device.GetStatus();
        }

        private void buttonPlus_Click(object sender, RoutedEventArgs e)
        {
            BusyChange(true);
            device.Plus();
        }

        private void buttonMinus_Click(object sender, RoutedEventArgs e)
        {
            BusyChange(true);
            device.Minus();
        }

        private void buttonSetValue_Click(object sender, RoutedEventArgs e)
        {
            BusyChange(true);
            device.SetValue(textBoxValue.Text);
        }

        private void buttonSetTreshold_Click(object sender, RoutedEventArgs e)
        {
            BusyChange(true);
            device.SetTreshold(textBoxValue.Text);
        }

        private void comboBoxComPort_DropDownOpened(object sender, EventArgs e)
        {
            GetPorts();
        }

        private void buttonConnectSerial_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxComPort.SelectedItem != null)
            {
                
                if (comboBoxComPort.SelectedItem.ToString().Contains("COM"))
                {
                    device = new DeviceSerial(comboBoxComPort.SelectedItem.ToString());
                    BusyChange(true);
                    connectionType = ConnectionType.Serial;
                }
                else 
                {
                    device = new DeviceWeb(textBoxTextValue.Text);
                    BusyChange(true);
                    connectionType = ConnectionType.Wifi;
                }

                device.MessageReceived += Device_MessageReceived;
                Thread.Sleep(1500);
                device.GetStatus();
            }
        }

        private void buttonSetPassword_Click(object sender, RoutedEventArgs e)
        {
            ((DeviceSerial)device).SetPassword(textBoxTextValue.Text);
        }

        private void buttonSetSsid_Click(object sender, RoutedEventArgs e)
        {
            ((DeviceSerial)device).SetSSID(textBoxTextValue.Text);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            BusyChange(true);
            ((DeviceSerial)device).WifiSetMode(WifiMode.APMode);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            BusyChange(true);
            ((DeviceSerial)device).WifiSetMode(WifiMode.ClientMode);
        }

        private void comboBoxWifiMode_DropDownOpened(object sender, EventArgs e)
        {

        }

        private void checkBoxWifiMode_Copy_Checked(object sender, RoutedEventArgs e)
        {
            langCode = 1029;
            SetLanguage();
        }

        private void checkBoxWifiMode_Copy_Unchecked(object sender, RoutedEventArgs e)
        {
            langCode = 1033;
            SetLanguage();
        }
    }

    public enum ConnectionType
    {
        None,
        Serial,
        Wifi
    }

    public struct Settings
    {
        public string Password;
        public string SSID;
        public string IpAddress;
    }
}
