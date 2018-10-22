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
        private byte destinationByte;
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
            textBox4.Text = "0";
            textBox5.Text = "0";
            textBox3.ReadOnly = true;
            textBox4.ReadOnly = true;
            button3.Text = "Send address";
            textBox3.ScrollBars = ScrollBars.Both;
        }

        private Boolean xOn = false;

        private byte[] CreatePackage(string message)
        {
            byte flag = Convert.ToByte("01100001", 2);
            long sourceAddress = Convert.ToInt64(textBox5.Text.Length > 0 ? textBox5.Text : "0");
            if (sourceAddress < 0 || sourceAddress > 255)
            {
                this.Invoke((MethodInvoker)(delegate
                {
                    textBox3.Text += "Source address must be between 0 and 255" + "\r\n";
                }));
                return null;
            }
            byte destinationByte = Convert.ToByte(textBox4.Text.Length > 0 ? textBox4.Text : "0");
            byte sourceByte = Convert.ToByte(textBox5.Text.Length > 0 ? textBox5.Text : "0");

            byte[] bytesSent = System.Text.Encoding.ASCII.GetBytes(message);
            byte[] addresses = new byte[] { destinationByte, sourceByte, (byte)bytesSent.Length };
            List<byte> addressList = new List<byte>(addresses);
            List<byte> bytesSentList = new List<byte>(bytesSent);
            addressList.AddRange(bytesSentList);
            if (bytesSent.Length > 255)
            {
                this.Invoke((MethodInvoker)(delegate
                {
                    textBox3.Text += "Data length more than 256 bytes" + "\r\n";
                }));
                return null;
            }
            byte[] dataAfterStuffing = BitStuffing.CodeData(addressList.ToArray());

            byte fcs = Convert.ToByte("11111111", 2);

            List<byte> firstListBefore = new List<byte>(new byte[] { flag, destinationByte, sourceByte, (byte)bytesSent.Length });
            List<byte> middleListBefore = new List<byte>(Encoding.ASCII.GetBytes(message));
            List<byte> lastListBefore = new List<byte>(new byte[] { fcs });
            firstListBefore.AddRange(middleListBefore);
            firstListBefore.AddRange(lastListBefore);
            PrintBytes(firstListBefore.ToArray(), "Package before stuffing");

            byte[] firstPart = new byte[] { flag };
            byte[] lastPart = new byte[] { fcs };
            List<byte> firstList = new List<byte>(firstPart);
            List<byte> middleList = new List<byte>(dataAfterStuffing);
            List<byte> lastList = new List<byte>(lastPart);
            firstList.AddRange(middleList);
            firstList.AddRange(lastPart);
            byte[] package = firstList.ToArray();
            return package;
        }
        private void PrintBytes(byte[] package, string debugMessage)
        {
            this.Invoke((MethodInvoker)(delegate
            {
                textBox3.Text += debugMessage + ": ";
                foreach (byte bt in package)
                {
                    textBox3.Text += Convert.ToString(bt, 16) + " ";
                }
                textBox3.Text += "\r\n";
            }));
        }
        private byte[] ParsePackage(byte[] package)
        {
            PrintBytes(package, "Received package");
            byte flag = 0;
            if (package[0] == Convert.ToByte("01100001", 2))
            {
                flag = package[0];
            }
            byte[] undecodeData = new byte[package.Length - 2];
            for (int i = 1, j = 0; i < package.Length - 1; i++, j++)
            {
                undecodeData[j] = package[i];
            }
            byte[] decodeData = BitStuffing.DecodeData(undecodeData);
            byte destinationAddress = decodeData[0];
            byte sourceAddress = decodeData[1];
            byte dataSize = decodeData[2];
            byte[] data = new byte[dataSize];
            for (int i = 3, j = 0; i < decodeData.Length; i++, j++)
            {
                data[j] = decodeData[i];
            }

            byte fcs = package[package.Length - 1];
            List<byte> firstListBefore = new List<byte>(new byte[] { flag, destinationAddress, sourceAddress });
            List<byte> middleListBefore = new List<byte>(data);
            List<byte> lastListBefore = new List<byte>(new byte[] { fcs });
            firstListBefore.AddRange(middleListBefore);
            firstListBefore.AddRange(lastListBefore);
            PrintBytes(firstListBefore.ToArray(), "Package after remove bits stuffing");
            return data;
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

                byte[] bytesSent = CreatePackage(textBox1.Text);
                if (bytesSent != null)
                {
                    serialPort.Write(bytesSent, 0, bytesSent.Length);
                    serialPort.RtsEnable = true;
                    Thread.Sleep(100);
                    serialPort.RtsEnable = false;
                    this.Invoke((MethodInvoker)(delegate
                                        {
                                            textBox1.Text = "";
                                            textBox3.Text += "Bytes sent:" + bytesSent.Length + "\r\n";
                                            PrintBytes(bytesSent, "Sent package");
                                        }));
                }

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
                textBox3.Text += "Error in port connection" + "\r\n";
            }
            if (serialPort.IsOpen)
            {
                comboBox1.Enabled = false;

                textBox3.Text += "Port is opened" + "\r\n";
                textBox3.Text += "Parity: " + label3.Text + "\r\n";
                textBox3.Text += "COM port: " + comboBox1.SelectedItem + "\r\n";
                textBox3.Text += "Baudrate: " + label10.Text + "\r\n";
                textBox3.Text += "StopBits: " + label2.Text + "\r\n";
                textBox3.Text += "DataBits: " + label9.Text + "\r\n";
            }
            serialPort.DataReceived += delegate
            {
                this.Invoke((MethodInvoker)(delegate()
                {
                    int size = serialPort.BytesToRead;
                    byte[] bytes = new byte[size];
                    serialPort.Read(bytes, 0, bytes.Length);
                    if (bytes.Length == 1 && bytes[0] == 0x13)
                    {
                        xOn = false;
                    }
                    else if (bytes.Length == 1 && bytes[0] == 0x11)
                    {
                        xOn = true;
                    }
                    else if (bytes.Length == 2 && bytes[0] == 0x15)
                    {
                        destinationByte = bytes[1];
                        textBox4.Text = Convert.ToString(destinationByte);
                    }
                    else
                    {
                        byte[] decodeData = ParsePackage(bytes);
                        textBox3.Text += "Bytes recieved:" + bytes.Length + "\r\n";
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
            else textBox3.Text += "To send message connect ports first!" + "\r\n";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                textBox3.Text += "Port is closed" + "\r\n";
                button1.Enabled = true;
            }
            comboBox1.Visible = true;
            comboBox1.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
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
                textBox3.Text += "You aren't connected to any port" + "\r\n";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                long sourceAddress = Convert.ToInt64(textBox5.Text.Length > 0 ? textBox5.Text : "0");
                if (sourceAddress < 0 || sourceAddress > 255)
                {
                    this.Invoke((MethodInvoker)(delegate
                    {
                        textBox3.Text += "Source address must be between 0 and 255" + "\r\n";
                    }));
                    return;
                }
                var xOnByte = new byte[] { 0x15, (byte)sourceAddress };
                serialPort.RtsEnable = true;
                serialPort.Write(xOnByte, 0, 2);
                Thread.Sleep(100);
                serialPort.RtsEnable = false;
            }
            catch (Exception ex)
            {
                textBox3.Text += "You aren't connected to any port" + "\r\n";
            }
        }

    }
}


