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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AtrackListener
{
    public partial class AtrackMain : Form
    {
        TcpListener AtrackServer;
        Thread AtrackListenerThread;
        private static AtrackMain atrackForm = null;
        private delegate void EnableDelegate(string text);

        public AtrackMain()
        {
            InitializeComponent();
            atrackForm = this;
        }

        private async Task TaskServerAsync()
        {
            IPAddress ip;
            if (!IPAddress.TryParse(txtIP.Text, out ip))
            {
                LogData("Failed to get IP address, service will listen for client activity on all network interfaces.");
                ip = IPAddress.Any;
            }

            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                throw new ArgumentException("Port is not valid.");
            }

            AtrackServer = new TcpListener(ip, port);
            LogData("Starting listener...");
            AtrackServer.Start();
            LogData("Listening...");
            while (true)
            {
                TcpClient _client = await AtrackServer.AcceptTcpClientAsync();
                var cw = new TcpAtrackService(_client);
                var thread = ThreadPool.UnsafeQueueUserWorkItem(x => ((TcpAtrackService)x).RunTask(), cw);

            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                 if (btnStart.Text == "Start")
                {
                    AtrackListenerThread = new Thread(new ThreadStart(this.TaskServerAsync().Wait));
                    AtrackListenerThread.Start();
                
                    btnStart.Text = "Stop";
                }
                else
                {

                    AtrackListenerThread.Join(1000);
                    if (AtrackListenerThread.IsAlive)
                    {
                        AtrackListenerThread.Abort();
                    }
                    AtrackListenerThread = null;
                    AtrackServer.Stop();
                    btnStart.Text = "Start";
                    LogData("Listener stopped!");
                    lstUnits.Items.Clear();
                }
            }
            catch (Exception ex)
            {
                
                
            }
        }

        public static void LogData(string txt)
        {
            if (atrackForm != null)
                atrackForm.LogText(txt);

        }

        private void LogText(string text)
        {
            // If this returns true, it means it was called from an external thread.
            if (InvokeRequired)
            {
                // Create a delegate of this method and let the form run it.
                this.Invoke(new EnableDelegate(LogText), new object[] { text });
                return; // Important
            }

            // Set textBox
            lstLogBox.Items.Insert(0, text);
            LogRawData(text);
            // keep only a few lines in the log
            while (lstLogBox.Items.Count > 1000)
            {
                lstLogBox.Items.RemoveAt(lstLogBox.Items.Count - 1);
            }
        }

        public static void LogImei(string txt)
        {
            if (atrackForm != null)
                atrackForm.LogIMEI(txt);

        }

        private void LogIMEI(string text)
        {
            // If this returns true, it means it was called from an external thread.
            if (InvokeRequired)
            {
                // Create a delegate of this method and let the form run it.
                this.Invoke(new EnableDelegate(LogIMEI), new object[] { text });
                return; // Important
            }

            if (!lstUnits.Items.Contains(text))
                lstUnits.Items.Add(text);
        }

        private void LogRawData(string message)
        {
            if (!Directory.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "Log"))
            {
                Directory.CreateDirectory(System.AppDomain.CurrentDomain.BaseDirectory + "Log");
            }

            string path = System.AppDomain.CurrentDomain.BaseDirectory + "Log\\RawDataLog.txt";
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(message);
                writer.Close();
            }
        }
    }
}
