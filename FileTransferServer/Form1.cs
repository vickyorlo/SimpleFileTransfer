using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTransferServer
{
    public partial class Form1 : Form
    {
        public delegate void FileRecievedEventHandler(object source, string fileName);
        public event FileRecievedEventHandler NewFileRecieved;

        public Form1()
        {
            InitializeComponent();
        }

        private void buttonListen_Click(object sender, EventArgs e)
        {
            try
            {
                int port = int.Parse(textPort.Text);
                Task.Factory.StartNew(() => HandleIncomingFile(port));
                MessageBox.Show("Listening on port" + port);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.NewFileRecieved += new FileRecievedEventHandler(Form1_NewFileRecieved);
        }

        private void Form1_NewFileRecieved(object sender, string fileName)
        {
            this.BeginInvoke(new Action(delegate ()
            {
                MessageBox.Show("New File Received\n" + fileName);
                System.Diagnostics.Process.Start("explorer", textFolder.Text);
            }));
        }

        public void HandleIncomingFile(int port)
        {
            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                while (true)
                {
                    Socket handlerSocket = tcpListener.AcceptSocket();
                    if (handlerSocket.Connected)
                    {
                        NetworkStream networkStream = new NetworkStream(handlerSocket);
                        int blockSize = 1024;
                        Byte[] dataByte = new Byte[blockSize];

                        int readByteLength = networkStream.Read(dataByte, 0, blockSize);
                        int fileNameLength = BitConverter.ToInt32(dataByte, 0);
                        string fileName = Encoding.ASCII.GetString(dataByte, 4, fileNameLength);
                        Stream fileStream = File.OpenWrite(textFolder.Text + fileName);
                        fileStream.Write(dataByte, 4 + fileNameLength, (1024 - (4 + fileNameLength)));

                        while (true)
                        {
                            readByteLength = networkStream.Read(dataByte, 0, blockSize);
                            fileStream.Write(dataByte, 0, readByteLength);
                            if (readByteLength == 0)
                                break;
                        }
                        fileStream.Close();
                    
                        NewFileRecieved?.Invoke(this, fileName);
                        handlerSocket = null;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Choose destination folder";
                DialogResult result = fbd.ShowDialog();

                textFolder.Text = fbd.SelectedPath;
            }
        }
    }
}
