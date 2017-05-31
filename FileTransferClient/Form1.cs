using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;
using System.Linq.Expressions;


namespace FileTransferClient
{
    public partial class Form1 : Form
    {
        private static string shortFileName = "";
        private static string fileName = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void labelChanger(string status)
        {
            labelStatus.Text = status;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog {Title = "File Sharing Client"};
            dlg.ShowDialog();
            testFilePath.Text = dlg.FileName;
            fileName = dlg.FileName;
            shortFileName = dlg.SafeFileName;
        }

        private async void buttonSend_Click(object sender, EventArgs e)
        {
            try
            {
                labelStatus.Text = "Sending...";
                IPAddress ipAddress = IPAddress.Parse(textIPAddress.Text);
                int port = int.Parse(textPort.Text);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);
                if (port > 65535) throw new FormatException();
                string fileName = testFilePath.Text;


                var progress = new Progress<string>(s => labelStatus.Text = s);
                var task = Task.Factory.StartNew(() => SendFile(ipEndPoint, fileName, shortFileName, progress));
                await task;
                labelStatus.Text = "Sent!";
            }
            catch (AggregateException ae)
            {
                labelStatus.Text = "Waiting...";
                throw ae.Flatten();
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid IP/Port!");
            }
            catch (ArgumentException)
            {
                MessageBox.Show("No file selected!");
            }
            catch (SocketException)
            {
                MessageBox.Show("Invalid IP Address or host unreachable!");
            }
        }

        public void SendFile(IPEndPoint hostIP,
            string longFileName, string shortFileName,IProgress<string> progress)
        {
                byte[] fileNameByte = Encoding.ASCII.GetBytes(shortFileName);
                byte[] fileData = File.ReadAllBytes(longFileName);
                byte[] clientData = new byte[4 + fileNameByte.Length + fileData.Length];
                byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);
                fileNameLen.CopyTo(clientData, 0);
                fileNameByte.CopyTo(clientData, 4);
                fileData.CopyTo(clientData, 4 + fileNameByte.Length);
                using (TcpClient clientSocket = new TcpClient(hostIP))
                {
                    NetworkStream networkStream = clientSocket.GetStream();
                    networkStream.Write(clientData, 0, clientData.GetLength(0));
                }
        }
    }
}
