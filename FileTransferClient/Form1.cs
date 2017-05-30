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

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "File Sharing Client";
            dlg.ShowDialog();
            testFilePath.Text = dlg.FileName;
            fileName = dlg.FileName;
            shortFileName = dlg.SafeFileName;
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            try
            {
                string ipAddress = textIPAddress.Text;
                int port = int.Parse(textPort.Text);
                string fileName = testFilePath.Text;
                Task.Factory.StartNew(() => SendFile(ipAddress, port, fileName, shortFileName));
                //MessageBox.Show("Sending...");
            }
            catch (Exception exception)
            {
                MessageBox.Show("Invalid IP/Port!");
                Console.WriteLine(exception);
                //throw;
            }
        }

        public void SendFile(string remoteHostIP, int remoteHostPort,
            string longFileName, string shortFileName)
        {
            try
            {
                if (!string.IsNullOrEmpty(remoteHostIP))
                {
                    byte[] fileNameByte = Encoding.ASCII.GetBytes(shortFileName);
                    byte[] fileData = File.ReadAllBytes(longFileName);
                    byte[] clientData = new byte[4 + fileNameByte.Length + fileData.Length];
                    byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);
                    fileNameLen.CopyTo(clientData, 0);
                    fileNameByte.CopyTo(clientData, 4);
                    fileData.CopyTo(clientData, 4 + fileNameByte.Length);
                    using (TcpClient clientSocket = new TcpClient(remoteHostIP, remoteHostPort))
                    {
                        NetworkStream networkStream = clientSocket.GetStream();
                        networkStream.Write(clientData, 0, clientData.GetLength(0));
                    }

                }
            }
            catch
            {
                MessageBox.Show("Cannot connect to server!");
            }
        }
    }
}
