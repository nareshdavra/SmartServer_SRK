using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;

namespace TcpIP_class
{
    public class TcpNotificationServer
    {
        public delegate void TcpNotifyHandlerDelegate(Object sender, rfidTcpNotArg arg);

        public event TcpNotifyHandlerDelegate TcpNotifyEvent = null;

        public struct Client
        {
            public Socket Socket;
            public Thread Thread;
            public string ClientName;
            public string Command;

        }
        private string strLog = null;
        private TextBox txtInfoServer = null;
        public TextBox TxtInfoServer
        {
            set
            {
                txtInfoServer = value;
                txtInfoServer.Text = strLog;
            }
        }
        private ArrayList ListeClients;
        private Thread threadClient;
        private Thread vigile = null;

        private TcpListener myListener = null;
        private Socket socketServeur = null;

        private bool stop = false;
        public int Port { get { return port; } set { port = value; } }
        private int port;



        public TcpNotificationServer(int port)
        {

            this.port = port;
            ListeClients = new ArrayList();
        }
        public void StartServer()
        {
            TextBoxRefresh("Start Server on port " + Port.ToString(), true, ">");
            vigile = new Thread(new ThreadStart(ConnexionClient));
            vigile.IsBackground = true;
            vigile.Start();
        }

        private void ConnexionClient()
        {

            //myListener = new TcpListener(IPAddress.Any, Port);
            myListener = new TcpListener(Port);
            try
            {
                myListener.Start();
            }
            catch (SocketException se)
            {
                // Socket already opened on this port
                myListener.Stop();
                return;
            }

            //TextBoxRefresh("Server Started on port " + Port.ToString(), false, ">");
            while (!stop)
            {
                try
                {
                    socketServeur = myListener.AcceptSocket();
                    threadClient = new Thread(new ThreadStart(EcouteClient));
                    threadClient.Start();
                }
                catch(Exception e)
                {
                    //MessageBox.Show(e.Message);
                    throw e;
                }
            }

            myListener.Stop();
            TextBoxRefresh("Server Stopped", true, ">");
        }
        private void EcouteClient()
        {
            string strReceive = null;
            Client myClient = new Client();
            myClient.Thread = threadClient;
            Socket mySocket = socketServeur;
            myClient.Socket = mySocket;
            ListeClients.Add(myClient);

            try
            {

                TextBoxRefresh("Client Connected " + ClientNumber().ToString() + " : " + mySocket.RemoteEndPoint.ToString(), true, ">");
                strReceive = ReadData(mySocket);
                TextBoxRefresh(strReceive, false, "<");
                if (!string.IsNullOrEmpty(strReceive))
                {
                    string[] dataNot = strReceive.Split(' ');

                    // At least 5 packets: CR_DISPATCH CC_SB_<EVENT> IP PORT DEVICE_SERIAL
                    if (dataNot == null || dataNot.Length < 5)
                    {
                        return;
                    }

                    rfidTcpNotArg arg;

                    switch(dataNot[1])
                    {
                        case "CC_SB_SCAN_STARTED":
                            arg = new rfidTcpNotArg(dataNot[4], rfidTcpNotArg.ReaderTcpNotify.RN_ScanStarted);
                            break;

                        case "CC_SB_DOOR_OPEN":
                            arg = new rfidTcpNotArg(dataNot[4], rfidTcpNotArg.ReaderTcpNotify.RN_Door_Opened);
                            break;

                        case "CC_SB_NEWINV":
                            int scanId;

                            if (dataNot.Length < 6 || !Int32.TryParse(dataNot[5], out scanId))
                            {
                                // Scan ID is missing or invalid => drop the event
                                return;
                            }

                            arg = new rfidTcpNotArg(dataNot[4], rfidTcpNotArg.ReaderTcpNotify.RN_ScanCompleted, scanId);
                            break;

                        case "CC_SB_SCAN_CANCEL_BY_HOST":
                            arg = new rfidTcpNotArg(dataNot[4], rfidTcpNotArg.ReaderTcpNotify.RN_ScanCancelledByHost);
                            break;

                        case "CC_SB_TEST_TCP":
                            arg = new rfidTcpNotArg(dataNot[4], rfidTcpNotArg.ReaderTcpNotify.RN_TestNotification);
                            break;
                        case "CC_SB_TEMP_CHANGED":
                            double tmp;
                            if (dataNot.Length < 6 || !double.TryParse(dataNot[5], out tmp))
                            {
                                // Scan ID is missing or invalid => drop the event
                                return;
                            }
                            arg = new rfidTcpNotArg(dataNot[4],rfidTcpNotArg.ReaderTcpNotify.RN_TempEventChanged,tmp,tmp);
                            break;
                        default:
                            return;
                    }

                    if (TcpNotifyEvent != null)
                    {
                        TcpNotifyEvent(this, arg);
                    }
                }


                for (int i = 0; i < ListeClients.Count; i++)
                {
                    Client c = (Client)ListeClients[i];
                    if (c.Socket == mySocket)
                    {
                        mySocket.Close();
                        ListeClients.RemoveAt(i);
                        TextBoxRefresh("Client Disconnected ", false, ">");
                    }
                }
            }

            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
                throw e;
            }
        }

        private string ReadData(Socket mySocket)
        {
            int size = -1;
            int nbToRead = -1;
            int dataRead = 0;
            string data = string.Empty;
            try
            {

                Byte[] buf = new Byte[1024];
                int ret = mySocket.Receive(buf);
                dataRead += ret;
                data += System.Text.Encoding.ASCII.GetString(buf, 0, ret);
                return data;
            }
            catch
            {

            }
            return null;
        }


        public int ClientNumber()
        {
            return ListeClients.Count;
        }

        public void StopSocket()
        {
            stop = true;
            if (ListeClients.Count != 0)
            {
                for (int n = 0; n < ListeClients.Count; ++n)
                {
                    Client c = (Client) ListeClients[n];
                    c.Socket.Close();
                }
            }

            if (socketServeur != null)
            {
                socketServeur.Close();
            }

            stop = true;
        }
        private delegate void TextBoxRefreshDelegate(string s, bool erase, string direction);
        private void TextBoxRefresh(string s, bool erase, string direction)
        {
            if (txtInfoServer == null) return;
            TimeSpan time = DateTime.Now.TimeOfDay;
            string message = (string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}: {4} {5}\r\n",
                    time.Hours, time.Minutes, time.Seconds, time.Milliseconds, direction, s));
            string line = "\r\n";
            // if (erase) strLog = string.Empty;
            string bck = strLog;
            strLog = message;
            if (erase) strLog += line;
            strLog += bck;

            if (strLog.Length > 4096)
                strLog = strLog.Substring(0, 4096);

            if (txtInfoServer == null) return;
            if (txtInfoServer.InvokeRequired)
            {
                txtInfoServer.Invoke((MethodInvoker)delegate
                {
                    txtInfoServer.Text = null;
                    txtInfoServer.Text = strLog;
                    txtInfoServer.Refresh();
                });
            }

        }
    }
    public class rfidTcpNotArg : System.EventArgs
    {
        public enum ReaderTcpNotify
        {
            RN_ScanStarted = 0x03,
            RN_ScanCompleted = 0x04,
            RN_ScanCancelledByHost = 0x05,
            RN_Door_Opened = 0x40,
            RN_Door_Closed = 0x41,
            RN_TempEvent = 0x43,
            RN_TempEventChanged = 0x44,
            RN_TestNotification = 0x45,
        }

        private string serialNumber;
        private ReaderTcpNotify rnValue;
        private double lastTempBottle;
        private double lastTempChamber;
        public int ScanId { get; private set; }

        public rfidTcpNotArg(string serialNumber, ReaderTcpNotify RNValue)
        {
            this.serialNumber = serialNumber;
            this.rnValue = RNValue;
        }

        public rfidTcpNotArg(string serialNumber, ReaderTcpNotify RNValue, double lastTempBottle, double lastTempChamber) : this(serialNumber, RNValue)
        {
            this.lastTempBottle = lastTempBottle;
            this.lastTempChamber = lastTempChamber;
        }

        public rfidTcpNotArg(string serialNumber, ReaderTcpNotify RNValue, int scanId) : this(serialNumber, RNValue)
        {
            ScanId = scanId;
        }

        public string SerialNumber
        {
            get { return serialNumber; }
        }
        public ReaderTcpNotify RN_Value
        {
            get { return rnValue; }
        }
        public double LastTempBottle
        {
            get { return lastTempBottle; }
        }
        public double LastTempChamber
        {
            get { return lastTempChamber; }
        }
    }
}
