using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Collections;
using System.IO;
namespace Lab1_1
{
    public partial class

        Form1 : Form
    {
        private SerialPort serialPort;
        public Form1()
        {
            InitializeComponent();
            serialPort = new SerialPort();
            var portList = SerialPort.GetPortNames();
            for (int i = 0; i < portList.Length; i++)
            {
                comboBox1.Items.Add(portList[i]);
            }
            label10.Text = "9600";
            label9.Text = "8";
            label3.Text = "None";
            label2.Text = "1";
            label11.Text = "Input:";
            label12.Text = "Debug:";
            textBox2.ReadOnly = true;

            listView2.Scrollable = true;
            listView2.View = View.Details;
            ColumnHeader header2 = new ColumnHeader();
            header2.Width = listView2.Width;
            header2.Text = "Debug information: ";
            listView2.Columns.Add(header2);
            listView2.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private Boolean xOn = false;

        private byte[] CreatePackage(string message)
        {
            byte flag = Convert.ToByte("01011010", 2);
            byte destinationAddres = Convert.ToByte("00000000", 2);
            byte sourceAddres = Convert.ToByte("00000000", 2);
            byte[] bytesSent = System.Text.Encoding.ASCII.GetBytes(message);
            byte[] dataAfterStuffing = BitStuffing.CodeData(bytesSent);
            byte dataSize = (byte)dataAfterStuffing.Length;
            if (dataSize > 255)
            {
                this.Invoke((MethodInvoker)(delegate
                {
                    listView2.Items.Add("Data length more than 256 bytes");
                }));
                return null;
            }
            byte fcs = Convert.ToByte("11111111", 2);

            byte[] firstPart = new byte[] { flag, destinationAddres, sourceAddres, dataSize };
            byte[] lastPart = new byte[] { fcs };
            List<byte> firstList = new List<byte>(firstPart);
            List<byte> middleList = new List<byte>(dataAfterStuffing);
            List<byte> lastList = new List<byte>(lastPart);
            firstList.AddRange(middleList);
            firstList.AddRange(lastPart);
            byte[] package = firstList.ToArray();
            return package;
        }

        private byte[] ParsePackage(byte[] package)
        {
            byte flag;
            if (package[0] == Convert.ToByte("01011010", 2))
            {
                flag = package[0];
            }
            byte destinationAddres = package[1];
            byte sourceAddres = package[2];
            byte dataSize = package[3];
            byte[] undecodeData = new byte[dataSize];
            for (int i = 4, j = 0; i < 4 + dataSize; i++, j++)
            {
                undecodeData[j] = package[i];
            }
            byte[] decodeData = BitStuffing.DecodeData(undecodeData);
            byte fcs = package[package.Length - 1];
            string fcsLine = BitStuffing.ConvertBytesToBinaryString(new byte[] { fcs });
            int count = 0;
            for (int i = 0; i < fcsLine.Length; i++)
            {
                if (fcsLine[i] == '1')
                {
                    count++;
                }
            }
            if (count % 2 == 1)
            {
                this.Invoke((MethodInvoker)(delegate 
                {
                    listView2.Text = "Transfer bytes error";
                }));
            }
            return decodeData;
        }
        private void SendData()
        {
            while (!xOn)
            {
                Thread.Sleep(100);
            }


            byte[] package = CreatePackage(textBox1.Text);
            if (package != null)
            {
                serialPort.RtsEnable = true;
                byte[] bytesSent = CreatePackage(textBox1.Text);
                serialPort.Write(bytesSent, 0, bytesSent.Length);
                Thread.Sleep(100);
                serialPort.RtsEnable = false;


                this.Invoke((MethodInvoker)(delegate
                {
                    textBox1.Text = "";
                    listView2.Items.Add("Bytes sent:" + bytesSent.Length);
                }));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int baudRate = Int32.Parse(label10.Text);
                int dataBits = Int32.Parse(label9.Text);
                Parity parity = (Parity)Enum.Parse(typeof(Parity), label3.Text, true);
                StopBits stopBits = (StopBits)Enum.Parse(typeof(StopBits), label2.Text, true);
                serialPort = new SerialPort(comboBox1.SelectedItem.ToString(), baudRate, parity,
                       dataBits, stopBits);
                if (serialPort != null && serialPort.IsOpen)
                    throw (new Exception());
                serialPort.Encoding = Encoding.UTF8;
                serialPort.Open();
                button1.Enabled = false;
            }
            catch (Exception ex)
            {
                listView2.Items.Add("Error in port connection");
            }
            if (serialPort.IsOpen)
            {
                comboBox1.Enabled = false;

                listView2.Items.Add("Port is opened");
                listView2.Items.Add("Parity: " + label3.Text);
                listView2.Items.Add("COM port: " + comboBox1.SelectedItem);
                listView2.Items.Add("Baudrate: " + label10.Text);
                listView2.Items.Add("StopBits: " + label2.Text);
                listView2.Items.Add("DataBits: " + label9.Text);
            }
            serialPort.DataReceived += delegate
            {
                this.Invoke((MethodInvoker)(delegate()
                {
                    string message = serialPort.ReadExisting();
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message); 
                    
                    if (bytes.Length == 1 && bytes[0] == 0x13)
                    {
                        xOn = false;
                    }
                    else if (bytes.Length == 1 && bytes[0] == 0x11)
                    {
                        xOn = true;
                    }
                    else
                    {
                        byte[] decodeData = ParsePackage(bytes);
                        listView2.Items.Add("Bytes recieved:" + decodeData.Length);
                        textBox2.Text += Encoding.UTF8.GetString(decodeData, 0, decodeData.Length) + "\r\n";
                    }

                }));

            };

        }

        private void dataSendEvent(object sender, EventArgs e)
        {
            if (serialPort.IsOpen && serialPort.BytesToRead == 0)
            {
                Thread thread = new Thread(SendData);
                thread.Start();
            }
            else listView2.Items.Add("To send message connect ports first!");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                listView2.Items.Add("Port is closed");
                button1.Enabled = true;
            }
            comboBox1.Visible = true;
            comboBox1.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                Console.WriteLine("State: " + checkBox1.CheckState);
                if (checkBox1.CheckState == CheckState.Checked)
                {
                    var xOnByte = new byte[] { 0x11 };
                    serialPort.RtsEnable = true;
                    serialPort.Write(xOnByte, 0, 1);
                    Thread.Sleep(100);
                    serialPort.RtsEnable = false;
                }
                if (checkBox1.CheckState == CheckState.Unchecked)
                {
                    var xOffByte = new byte[] { 0x13 };
                    serialPort.RtsEnable = true;
                    serialPort.Write(xOffByte, 0, 1);
                    Thread.Sleep(100);
                    serialPort.RtsEnable = false;
                }
            }
            catch (Exception ex)
            {
                listView2.Items.Add("You aren't connected to any port");
            }
        }

    }
}


