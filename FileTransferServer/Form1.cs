using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
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

        private async void buttonListen_Click(object sender, EventArgs e)
        {
            try
            {
                int port = int.Parse(textPort.Text);
                if (!File.GetAttributes(textFolder.Text).HasFlag(FileAttributes.Directory))
                {
                    throw new DirectoryNotFoundException();
                }
                if (port > 65535)
                {
                    throw new ArgumentOutOfRangeException();
                }

                MessageBox.Show("Listening on port " + port);

                await Task.Factory.StartNew(() => HandleIncomingFile(port));
                MessageBox.Show("done" + port);

            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("Invalid port!");
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("Directory not found!");
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
                    using (Socket handlerSocket = tcpListener.AcceptSocket())
                    {
                        if (handlerSocket.Connected)
                        {
                            //Prepare for reading file
                            var networkStream = new NetworkStream(handlerSocket);
                            var blockSize = 1024;
                            var dataByte = new byte[blockSize];

                            //Get the name of the file you got and want to save
                            networkStream.Read(dataByte, 0, blockSize);
                            int fileNameLength = BitConverter.ToInt32(dataByte, 0);
                            string fileName = Encoding.ASCII.GetString(dataByte, 4, fileNameLength);

                            //Check if the file exists, if so, append a number at the end of the filename to get rid of collisions
                            string filePath = Path.Combine(textFolder.Text, fileName);
                            string safeName = fileName;
                            var count = 1;

                            while (File.Exists(Path.Combine(textFolder.Text, safeName)))
                                safeName =
                                    $"{Path.GetFileNameWithoutExtension(filePath)}{count++}{Path.GetExtension(filePath)}";
                            try
                            {
                                //Create file with the now safe and unused filename and write the first kilobyte of read data to it
                                using (Stream fileStream = File.OpenWrite(Path.Combine(textFolder.Text, safeName)))
                                {
                                    fileStream.Write(dataByte, 4 + fileNameLength, 1024 - (4 + fileNameLength));

                                    //Read and save the rest of the file
                                    while (true)
                                    {
                                        int readByteLength = networkStream.Read(dataByte, 0, blockSize);
                                        fileStream.Write(dataByte, 0, readByteLength);
                                        if (readByteLength == 0)
                                            break;
                                    }
                                }
                            }
                            catch (IOException)
                            {
                                MessageBox.Show("File operation failed!");
                                if (File.Exists(Path.Combine(textFolder.Text, safeName)))
                                {
                                    File.Delete(Path.Combine(textFolder.Text, safeName));
                                }
                            }

                            //Make sure the delegate is filled and show the completed dialog
                            NewFileRecieved?.Invoke(this, fileName);
                        }
                    } 
                }
            }
            catch (IOException )
            {
                MessageBox.Show("Writing file failed!");

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
