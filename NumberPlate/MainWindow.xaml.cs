﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
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

        public MainWindow()
        {
            InitializeComponent();
            GetPorts();
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

            if (message.Contains("is set to:") && connectionType == ConnectionType.Serial)
                device.GetStatus();
            else
                BusyChange(false);
        }

        void DeserializeMessage(string message)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
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
                        case "ip":
                            labelIpAddress.Content = messagePartSub[1];
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
            ));
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
                    device = new DeviceWeb(labelIpAddress.Content.ToString());
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
    }

    public enum ConnectionType
    {
        None,
        Serial,
        Wifi
    }
}
