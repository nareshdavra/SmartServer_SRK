using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;



using DBClass;
using DataClass;

using SDK_SC_RFID_Devices;
using SDK_SC_Fingerprint;
using SDK_SC_MedicalCabinet;
namespace TcpIP_class
{  

    public class TcpIpServer
    {

        private const string regKey = @"HKEY_CURRENT_USER\SOFTWARE\SmartTracker_V2";
        private const string regValueInstall = "Executable_Path";

        public struct Client
        {
            public Socket Socket;
            public Thread Thread;
            public string ClientName;
            public string Command;
        }      

     

        volatile private deviceClass[] localDeviceArray = null;
        public deviceClass[] LocalDeviceArray { get { return localDeviceArray; } set { localDeviceArray = value; } }

        volatile private string lastBadgeRead = string.Empty;
        public string LastBadgeRead { set { lastBadgeRead = value; } get { return lastBadgeRead; } }

        private string strLog = null;

        private TextBox txtInfoServer;
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

        public double TimeZoneOffset;

        private TcpListener myListener = null;
        private Socket socketServeur = null;
        private NetworkStream nsClient = null;

        private bool stop = false;
        public int Port { get { return port; } set { port = value; } }
        private int port;

        private Dictionary<string, string > spareDataCol = new Dictionary<string,string>();
        public Dictionary<string, string> SpareDataCol { get { return spareDataCol; } set { spareDataCol = value; } }

        private string spareData1 = null;
        public string SpareData1 { get { return spareData1; } set { spareData1 = value; } }

        private string spareData2 = null;
        public string SpareData2 { get { return spareData2; } set { spareData2 = value; } }

        private bool needrefreshScan = false;
        public bool NeedRefreshScan { get { return needrefreshScan; } set { needrefreshScan = value; } }

        private bool needrefreshSQL = false;
        public bool NeedRefreshSQL { get { return needrefreshSQL; } set { needrefreshSQL = value; } }

        private bool needrefreshUser = false;
        public bool NeedRefreshUser { get { return needrefreshUser; } set { needrefreshUser = value; } }

        private bool needrefreshFP = false;
        public bool NeedRefreshFP { get { return needrefreshFP; } set { needrefreshFP = value; } }
        
        private bool needrefreshTree = false;
        public bool NeedRefreshTree { get { return needrefreshTree; } set { needrefreshTree = value; } }

        private bool needrefreshTimeZone = false;
        public bool NeedrefreshTimeZone { get { return needrefreshTimeZone; } set { needrefreshTimeZone = value; } }

        private bool accumulateMode = false;
        public bool AccumulateMode { get { return accumulateMode; } set { accumulateMode = value; } }

        private string serverIP;
        public string ServerIP { get { return serverIP; } }

        public EventWaitHandle eventScanStart = new AutoResetEvent(false);
        public EventWaitHandle eventScanCompleted = new AutoResetEvent(false);
        public EventWaitHandle eventScanCancelled = new AutoResetEvent(false);
        public bool requestScanFromServer = false;
        public bool requestRestart = false;
        public TcpIpServer(int port)
        {
            //this.serverIP = tcpUtils.getLocalIp().ToString();
            this.serverIP = tcpUtils.GetPhysicalIPAdress();
           // this.serverIP = "192.168.1.25";
            this.port = port;
            ListeClients = new ArrayList();
        }




        /* Fonction: StartServer(int myPort)
         * 
         * Fonction qui lance un thread de surveillance du port donné en argument.
         */
        public void StartServer()
        {
            if (vigile != null)
            {
                vigile.Abort();
                vigile = null;
            }

            TextBoxRefresh("Start Server on port " + Port.ToString(), true, ">");
            vigile = new Thread(new ThreadStart(ConnexionClient));
            vigile.IsBackground = true;
            vigile.Start();

        }

        /* Fonction: ConnexionClient()
		 * 
		 * Cette fonction crée un TcpListener sur le port donné en argument de la fonction
		 * StartServer(int myPort) et tourne en boucle en attendant la connexion d'un
		 * client. Lorsqu'un client se connecte, la fonction crée un socket à l'aide du
		 * TcpListener et lance un thread qui gèrera réellement ce socket. Pendant ce
		 * temps, cette fonction continue d'attendre la connexion d'autres clients.
		 */
        private void ConnexionClient()
        {

            myListener = new TcpListener(IPAddress.Any, Port);
            //myListener = new TcpListener(Port);
            myListener.ExclusiveAddressUse = true; 
            myListener.Start();
            //TextBoxRefresh("Server Started on port " + Port.ToString(), false, ">");
            while (!stop)
            {
                try
                {
                    if (!myListener.Pending())
                    {
                        Thread.Sleep(500); // choose a number (in milliseconds) that makes sense
                        continue; // skip to next iteration of loop
                    }

                    socketServeur = myListener.AcceptSocket();
                    IPEndPoint ipIn = socketServeur.LocalEndPoint as IPEndPoint;
                    if ((ipIn != null) && (ipIn.Address.ToString() != this.serverIP))
                    {
                        this.serverIP = ipIn.Address.ToString();
                    }
                    threadClient = new Thread(new ThreadStart(EcouteClient));
                    threadClient.Start();
                }
                catch
                {
                }
            }
            TextBoxRefresh("Server Stopped", true, ">");
        }



        private void EcouteClient()
        {

                MainDBClass db = null;

                string strReceive = null;
                Client myClient = new Client();           
                myClient.Thread = threadClient;
                Socket mySocket = socketServeur;
                myClient.Socket = mySocket;
                ListeClients.Add(myClient);
                int nbArg;

            try
            {
                if ((mySocket == null) || (mySocket.RemoteEndPoint == null)) return;
                TextBoxRefresh(
                    "Client Connected " + ClientNumber().ToString() + " : " + mySocket.RemoteEndPoint.ToString(), true,
                    ">");
            }
            catch
            {
                
            }
                
                try
                {
               
                    

                    strReceive = ReadData(mySocket);
                    if (localDeviceArray == null)
                    {
                        SendReturnCode(mySocket, myClient, ReturnType.readerNotReady+ "\r\n", false);
                        return;
                    }
                    if (strReceive == null)
                    {
                        TextBoxRefresh("Unknown or null Command Received :" + myClient.Command, false, ">");
                        //SendReturnCode(mySocket, myClient, ReturnType.unknownCmd + "\r\n", false);
                        return;
                    }

                    if ((localDeviceArray[0].infoDev.deviceType == DeviceType.DT_SBR) && (strReceive.StartsWith("|"))) // mode direct
                    {
                         bool bExit = false;
                         do
                         {
                             string cmd = strReceive; 
                             switch (cmd)
                             {
                                 case "|BK":
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
                                     bExit = true;
                                     break;
                                 case "|ST":
                                     if (localDeviceArray[0].bDataCompleted)
                                         SendReturnCode(mySocket, myClient, localDeviceArray[0].rfidDev.DeviceStatus.ToString() + "\r\n", false, false);
                                     else
                                         SendReturnCode(mySocket, myClient, DeviceStatus.DS_InScan.ToString() + "\r\n", false, false);
                                     break;
                                 case "|PG":
                                     SendReturnCode(mySocket, myClient, "PING OK\r\n", false, false);
                                     break;
                                 case "|RD":
                                       requestScanFromServer = true;
                                    accumulateMode = false;
                                    eventScanStart.Reset();
                                    eventScanCancelled.Reset();
                                    eventScanCompleted.Reset();
                                    
                                    if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                        (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                    {

                                        localDeviceArray[0].rfidDev.ScanDevice(false);
                                        if (eventScanStart.WaitOne(2500, false))
                                            SendReturnCode(mySocket, myClient, ReturnType.scanStarted + "\r\n", false);
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.failedToStartScan + "\r\n", false);
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.readerNotReady + "\r\n", false);

                                    break;
                                 case "|RW":
                                        requestScanFromServer = true;
                                        eventScanStart.Reset();
                                        eventScanCancelled.Reset();
                                        eventScanCompleted.Reset();                                   

                                        if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                       (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                        {
                                            localDeviceArray[0].rfidDev.ScanDevice(false);

                                            int nbTagTosend = 0;
                                            int nbTagSend = 0;
                                            int timeout = (int)localDeviceArray[0].rfidDev.get_RFID_Device.ScanTimeout;

                                            if (eventScanStart.WaitOne(2500, false))
                                            {

                                                do
                                                {
                                                    Thread.Sleep(250);
                                                    nbTagTosend = localDeviceArray[0].rfidDev.get_RFID_Device.ReaderData.strListTag.Count;

                                                    for (int loop = nbTagSend; loop < nbTagTosend; loop++)
                                                    {
                                                        SendReturnCode(mySocket, myClient, "Tag " + (loop+1) + ":" + localDeviceArray[0].rfidDev.get_RFID_Device.ReaderData.strListTag[loop] + "\r\n", false, false);
                                                    }
                                                    nbTagSend = nbTagTosend;

                                                }
                                                while (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_InScan);

                                                nbTagTosend = localDeviceArray[0].rfidDev.get_RFID_Device.ReaderData.strListTag.Count;

                                                for (int loop = nbTagSend; loop < nbTagTosend; loop++)
                                                {
                                                    SendReturnCode(mySocket, myClient, "Tag " + (loop + 1) + ":" + localDeviceArray[0].rfidDev.get_RFID_Device.ReaderData.strListTag[loop] + "\r\n", false, false);
                                                }

                                                SendReturnCode(mySocket, myClient, "Scan completed\r\n" , false, false);                                               
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, "Error Start Scan", false,false);
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, localDeviceArray[0].rfidDev.DeviceStatus.ToString(), false);
                                        }
                                    break;
                                 case "|NT":
                                    if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                     (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                    {                                        
                                        SendReturnCode(mySocket, myClient, localDeviceArray[0].currentInventory.listTagAll.Count.ToString() + "\r\n", false, false);                                     

                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.readerNotReady + "\r\n", false);

                                    break;

                                 case "|GT":
                                    if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                     (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                    {
                                        int nindex = 1;
                                        foreach (string uid in localDeviceArray[0].currentInventory.listTagAll)
                                        {
                                            SendReturnCode(mySocket, myClient,"Tag " + nindex + ":" + uid + "\r\n", false,false);
                                            nindex++;
                                        }
                                        
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.readerNotReady + "\r\n", false);

                                    break;

                                 default: SendReturnCode(mySocket, myClient, "Unknown command\r\n", false, false); break;
                             }
                             if (!bExit)
                             {
                                 bool bReceive = false;
                                 do
                                 {
                                     Byte[] buffer = new Byte[4];
                                     int size = mySocket.Receive(buffer);

                                     if (buffer[0] == 0x7C) // direct command
                                     {
                                         string data = System.Text.Encoding.ASCII.GetString(buffer, 0, size);
                                         strReceive = tcpUtils.DecodeString(data);
                                         TextBoxRefresh(strReceive + "( " + strReceive.Length.ToString() + " )", false, "<");
                                         bReceive = true;
                                     }
                                 }
                                 while (!bReceive);
                             }
                         }
                         while (!bExit);
                    }
                    else // mode classique
                    {
                        
                        string[] command = null;
                        nbArg = 0;

                        bool isTagCommand = false;
                        List<string> tagCommands = new List<string>();
                        tagCommands.Add("START_LIGHTING_LED");
                        tagCommands.Add("GET_TAG_AT_INDEX");
                        tagCommands.Add("WRITE_BLOCK");

                        foreach (string tagCommand in tagCommands)
                        {
                            if (strReceive.Contains(tagCommand))
                            {
                                command = strReceive.Split(tcpUtils.TCPDelimiter);
                                myClient.Command = command[0];
                                nbArg = command.Length;
                                isTagCommand = true;
                                break;
                            }
                        }

                        if(!isTagCommand)
                        {
                            if (strReceive.Contains(";"))
                            {
                                command = strReceive.Split(';');
                                myClient.Command = command[0];
                                nbArg = command.Length;
                            }

                            else
                            {
                                myClient.Command = strReceive;
                                nbArg = 1;
                            }
                        }

                        switch (myClient.Command)
                        {
                            #region RENEWFP
                            case "RENEWFP?":
                            {
                                if (File.Exists(@"c:\devcon\x86\removeFP.bat"))
                                {
                                    Process fpProcess = new Process();
                                    fpProcess.StartInfo.FileName = @"c:\devcon\x86\removeFP.bat";
                                    fpProcess.StartInfo.UseShellExecute = true;
                                   
                                    fpProcess.Start();
                                    fpProcess.WaitForExit(30000);
           
                                    needrefreshFP = true;  //refreshF }
                                    SendReturnCode(mySocket, myClient, ReturnType.restartOk, false);
                                }
                                else
                                {
                                    SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                }
                                break;
                            }
                            #endregion
                            #region RESTART
                            case "RESTART?":
                                {
                                    requestRestart = true;
                                    SendReturnCode(mySocket, myClient, ReturnType.restartOk, false);

                                    break;
                                }
                            #endregion
                            #region REBOOT
                            case "REBOOT?":
                                {
                                    SendReturnCode(mySocket, myClient, ReturnType.rebootOk, false);
                                    NativeMethods.Reboot();
                                    break;
                                }
                            #endregion
                            #region GET_TIME_ZONE
                            case "GET_TIME_ZONE?":
                                {
                                    SendReturnCode(mySocket, myClient, TimeZoneOffset.ToString(), false);
                                    break;
                                }
                            #endregion
                            #region GET_SYSTEM_TIME
                            case "GET_SYSTEM_ZONE?":
                                {
                                    SendReturnCode(mySocket, myClient, DateTime.UtcNow.ToString(), false);
                                    break;
                                }
                            #endregion
                            #region SET_TIME_ZONE
                            case "SET_TIME_ZONE?":
                                {
                                    double TimeZonetmp;
                                    if (nbArg == 2) // unique reader assume in index  0 of local array
                                    {

                                        string valTime = command[1];
                                        System.Globalization.CultureInfo culture;

                                        if (valTime.Contains("."))
                                        {
                                            // Utilisation de InvariantCulture si présence du . comme séparateur décimal. 
                                            culture = System.Globalization.CultureInfo.InvariantCulture;
                                        }
                                        else
                                        {
                                            // Utilisation de CurrentCulture sinon (utilisation de , comme séparateur décimal).
                                            culture = System.Globalization.CultureInfo.CurrentCulture;
                                        }

                                        if (double.TryParse(valTime, System.Globalization.NumberStyles.Number, culture, out TimeZonetmp))
                                        {
                                            TimeZoneOffset = TimeZonetmp;
                                            needrefreshTimeZone = true;
                                            SendReturnCode(mySocket, myClient, ReturnType.timeZoneok, false);
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.timeZoneBad, false);
                                        }
                                    }
                                    break;
                                }
                            #endregion
                            #region CMD PING
                            case "PING?":
                                {
                                    SendReturnCode(mySocket, myClient, ReturnType.pingServerOk, false);
                                    break;
                                }
                            #endregion
                            #region CMD REFRESH USER
                            case "REFRESH_USER?":
                                {
                                    needrefreshUser = true;
                                    SendReturnCode(mySocket, myClient, ReturnType.refreshUser, false);
                                    break;
                                }
                            #endregion
                            #region CMG GET_DEVICE
                            case "GET_DEVICE?":
                                {

                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        DeviceInfo[] di = db.RecoverDevice(true);
                                        if (di != null)
                                        {
                                            SendReturnCode(mySocket, myClient, di.Length.ToString(), false);
                                            foreach (DeviceInfo dev in di)
                                            {
                                                BinaryFormatter bf = new BinaryFormatter();
                                                MemoryStream mem = new MemoryStream();
                                                bf.Serialize(mem, dev);
                                                string str = Convert.ToBase64String(mem.ToArray());
                                                SendReturnCode(mySocket, myClient, str, false);
                                            }
                                            db.CloseDB();
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
                                        else
                                            SendReturnCode(mySocket, myClient, "0", false);

                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    break;
                                }
                            #endregion
                            #region CMD SET_REMOTE_ACCESS
                            case "SET_REMOTE_ACCESS?":
                                {
                                    string serialRFID = command[1];
                                    string IpRemote = command[2];
                                    int portRemote = 0;
                                    int.TryParse(command[3], out portRemote);


                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        if (db.UpdateNetworkDevice(serialRFID, IpRemote, portRemote))
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.setRemoteOk, false);

                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.setRemoteError, false);
                                        db.CloseDB();
                                        requestRestart = true;
                                        //needrefreshNet = true;
                                    }
                                    break;
                                }
                            #endregion
                            #region CMD DELETE_REMOTE_ACCESS
                            case "DELETE_REMOTE_ACCESS?":
                                {
                                    string serialRFID = command[1];
                                    string IpSender = command[2];


                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        if (db.UpdateNetworkDevice(serialRFID, null, 0))
                                        {
                                            db.DeleteUserGrant(IpSender);
                                            SendReturnCode(mySocket, myClient, ReturnType.unsetRemoteOk, false);
                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.unsetRemoteError, false);
                                        db.CloseDB();
                                        needrefreshUser = true;
                                    }
                                    break;
                                }
                            #endregion
                            #region CMD GET_INVENTORY?

                            case "GET_INVENTORY?":
                                {
                                    string serialRFID = command[1];


                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        Hashtable ColumnInfo = db.GetColumnInfo();
                                        InventoryData id = db.GetLastScan(serialRFID);
                                        if (id != null)
                                        {
                                            DateTime utcDate = id.eventDate.ToUniversalTime();
                                            SendReturnCode(mySocket, myClient, utcDate.ToString("u"), false);
                                        }
                                        else
                                        {
                                            DateTime dt = DateTime.UtcNow.AddDays(-1.0);
                                            SendReturnCode(mySocket, myClient, dt.ToString("u"), false);
                                        }

                                        string strRet = ReadData(mySocket);
                                        int nbInv = 0;
                                        int.TryParse(strRet, out nbInv);

                                        if (nbInv > 0)
                                        {
                                            InventoryData[] invData = new InventoryData[nbInv];
                                            for (int loop = 0; loop < nbInv; loop++)
                                            {
                                                string data = ReadData(mySocket);
                                                if (data != null)
                                                {
                                                    try
                                                    {
                                                        //InventoryData tmpinv = new InventoryData();
                                                        BinaryFormatter bf = new BinaryFormatter();
                                                        MemoryStream mem = new MemoryStream(Convert.FromBase64String(data));
                                                        StoredInventoryData siv = new StoredInventoryData();
                                                        siv = (StoredInventoryData)bf.Deserialize(mem);
                                                        InventoryData dt = ConvertInventory.ConvertForUse(siv, ColumnInfo);
                                                        //tmpinv = (InventoryData)bf.Deserialize(mem);
                                                        invData[loop] = dt;
                                                    }
                                                    catch
                                                    {
                                                        invData[loop] = null;
                                                    }

                                                }
                                            }


                                            foreach (InventoryData ind in invData)
                                            {
                                                if (ind != null)
                                                {
                                                    db.StoreInventory(ind);
                                                    int IdScanEvent = db.getRowInsertIndex();
                                                    db.storeTagEvent(ind, IdScanEvent);
                                                }

                                            }

                                            db.CloseDB();
                                            needrefreshScan = true;
                                        }
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    break;
                                }
                            #endregion
                            #region CMD GET USER?
                            case "GET_USER?":
                                {
                                    if (nbArg == 2) // il y a un serial donc return allowed
                                    {
                                        string serialRFID = command[1];

                                        db = new MainDBClass();

                                        if (db.OpenDB())
                                        {
                                            //UserClassTemplate[] uct = db.RecoverUser();
                                            //UserClassTemplate[] uct = db.RecoverAllowedUser(serialRFID);
                                            DeviceGrant[] uct = db.RecoverAllowedUser(serialRFID);
                                            if (uct != null)
                                            {
                                                SendReturnCode(mySocket, myClient, uct.Length.ToString(), false);
                                                foreach (DeviceGrant dev in uct)
                                                {
                                                    BinaryFormatter bf = new BinaryFormatter();
                                                    MemoryStream mem = new MemoryStream();
                                                    bf.Serialize(mem, dev.user);
                                                    string str = Convert.ToBase64String(mem.ToArray());
                                                    SendReturnCode(mySocket, myClient, str, false);
                                                }
                                                db.CloseDB();
                                            }
                                            else
                                            {
                                                SendReturnCode(mySocket, myClient, "0", false);
                                            }
                                        }

                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }
                                    else // pas de serial return all template
                                    {
                                        db = new MainDBClass();

                                        if (db.OpenDB())
                                        {

                                            UserClassTemplate[] uct = db.RecoverUser();
                                            if (uct != null)
                                            {
                                                SendReturnCode(mySocket, myClient, uct.Length.ToString(), false);
                                                foreach (UserClassTemplate dev in uct)
                                                {
                                                    BinaryFormatter bf = new BinaryFormatter();
                                                    MemoryStream mem = new MemoryStream();
                                                    bf.Serialize(mem, dev);
                                                    string str = Convert.ToBase64String(mem.ToArray());
                                                    SendReturnCode(mySocket, myClient, str, false);
                                                }
                                                db.CloseDB();
                                            }
                                            else
                                            {
                                                SendReturnCode(mySocket, myClient, "0", false);
                                            }
                                        }

                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region GET_USER_WITH_GRANT
                            case "GET_USER_WITH_GRANT?":
                                {
                                    if (nbArg == 2) // il y a un serial donc return allowed
                                    {
                                        string serialRFID = command[1];

                                        db = new MainDBClass();

                                        if (db.OpenDB())
                                        {
                                            //UserClassTemplate[] uctall = db.RecoverUser();
                                            //UserClassTemplate[] uct = db.RecoverAllowedUser(serialRFID);
                                            /*DeviceGrant[] uct = db.RecoverAllowedUser(serialRFID);
                                            if (uct != null)
                                            {
                                                SendReturnCode(mySocket, myClient, uct.Length.ToString(), false);
                                                //foreach (UserClassTemplate dev in uct)
                                                foreach (DeviceGrant dev in uct)
                                                {
                                                    BinaryFormatter bf = new BinaryFormatter();
                                                    MemoryStream mem = new MemoryStream();
                                                    bf.Serialize(mem, dev);
                                                    string str = Convert.ToBase64String(mem.ToArray());
                                                    SendReturnCode(mySocket, myClient, str, false);
                                                }
                                                db.CloseDB();
                                            }
                                            else
                                            {
                                                SendReturnCode(mySocket, myClient, "0", false);
                                            }*/
                                            UserClassTemplate[] uctall = db.RecoverUser();
                                            DeviceGrant[] dgall = db.RecoverAllowedUser(serialRFID);
                                            if (uctall != null)
                                            {
                                                SendReturnCode(mySocket, myClient, uctall.Length.ToString(), false);
                                                foreach (UserClassTemplate uct in uctall)
                                                {
                                                    DeviceGrant newUser = null;
                                                    bool buserWithGrant = false;
                                                    if (dgall != null)
                                                    {
                                                        foreach (DeviceGrant dg in dgall)
                                                        {
                                                            if ((dg.user.lastName.Equals(uct.lastName)) && (dg.user.firstName.Equals(uct.firstName)))
                                                            {
                                                                newUser = dg;
                                                                buserWithGrant = true;
                                                                break;
                                                            }
                                                        }
                                                    }

                                                    if (buserWithGrant) // find user with grant - send it
                                                    {
                                                        BinaryFormatter bf = new BinaryFormatter();
                                                        MemoryStream mem = new MemoryStream();
                                                        bf.Serialize(mem, newUser);
                                                        string str = Convert.ToBase64String(mem.ToArray());
                                                        SendReturnCode(mySocket, myClient, str, false);
                                                    }
                                                    else // find user with no grant - create it and send it
                                                    {
                                                        newUser = new DeviceGrant();
                                                        newUser.user = uct;
                                                        newUser.userGrant = UserGrant.UG_NONE;
                                                        newUser.serialRFID = serialRFID;
                                                        BinaryFormatter bf = new BinaryFormatter();
                                                        MemoryStream mem = new MemoryStream();
                                                        bf.Serialize(mem, newUser);
                                                        string str = Convert.ToBase64String(mem.ToArray());
                                                        SendReturnCode(mySocket, myClient, str, false);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                SendReturnCode(mySocket, myClient, "0", false);
                                            }


                                        }

                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.noData, false);

                                    break;
                                }
                            #endregion
                            #region CMD GET USER_STR?
                            case "GET_USER_STR?":
                                {
                                    if (nbArg == 2) // il y a un serial donc return allowed
                                    {
                                        string serialRFID = command[1];

                                        db = new MainDBClass();

                                        if (db.OpenDB())
                                        {
                                            //UserClassTemplate[] uct = db.RecoverUser();
                                            //UserClassTemplate[] uct = db.RecoverAllowedUser(serialRFID);
                                            DeviceGrant[] uct = db.RecoverAllowedUser(serialRFID);

                                            if (uct != null)
                                            {
                                                SendReturnCode(mySocket, myClient, uct.Length.ToString(), false);
                                                //foreach (UserClassTemplate dev in uct)
                                                foreach (DeviceGrant dev in uct)
                                                {

                                                    BinaryFormatter bf = new BinaryFormatter();
                                                    MemoryStream mem = new MemoryStream(Convert.FromBase64String(dev.user.template));
                                                    UserClass TheUser = new UserClass();
                                                    TheUser = (UserClass)bf.Deserialize(mem);

                                                    string strUser = TheUser.firstName;
                                                    strUser += ";" + TheUser.lastName;
                                                    strUser += ";" + dev.user.BadgeReaderID;

                                                    int nIndex = 0;

                                                    FingerData fd = new FingerData();
                                                    fd.CopyUserToFinger(TheUser);

                                                    foreach (string template in TheUser.strFingerprint)
                                                    {
                                                        if (!string.IsNullOrEmpty(template))
                                                        {
                                                            strUser += ";" + nIndex.ToString() + ";" + tcpUtils.ByteArrayToHexString(fd.GetFingerTemplate(nIndex++));
                                                        }
                                                        nIndex++;
                                                    }
                                                    SendReturnCode(mySocket, myClient, strUser, false);
                                                }
                                                db.CloseDB();
                                            }
                                            else
                                            {
                                                SendReturnCode(mySocket, myClient, "0", false);
                                            }
                                        }

                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }
                                    else // pas de serial return all template
                                    {
                                        db = new MainDBClass();

                                        if (db.OpenDB())
                                        {
                                            //UserClassTemplate[] uct = db.RecoverUser();
                                            UserClassTemplate[] uct = db.RecoverUser();

                                            if (uct != null)
                                            {
                                                SendReturnCode(mySocket, myClient, uct.Length.ToString(), false);
                                                foreach (UserClassTemplate dev in uct)
                                                {
                                                    BinaryFormatter bf = new BinaryFormatter();
                                                    MemoryStream mem = new MemoryStream(Convert.FromBase64String(dev.template));
                                                    UserClass TheUser = new UserClass();
                                                    TheUser = (UserClass)bf.Deserialize(mem);

                                                    string strUser = TheUser.firstName;
                                                    strUser += ";" + TheUser.lastName;
                                                    strUser += ";" + dev.BadgeReaderID;

                                                    int nIndex = 0;

                                                    FingerData fd = new FingerData();
                                                    fd.CopyUserToFinger(TheUser);

                                                    foreach (string template in TheUser.strFingerprint)
                                                    {
                                                        if (!string.IsNullOrEmpty(template))
                                                        {
                                                            byte[] byteTemplate = fd.GetFingerTemplate(nIndex);
                                                            if (byteTemplate.Length > 0)
                                                            {
                                                                strUser += ";" + nIndex.ToString() + ";" + tcpUtils.ByteArrayToHexString(byteTemplate);
                                                            }
                                                        }
                                                        nIndex++;
                                                    }
                                                    SendReturnCode(mySocket, myClient, strUser, false);
                                                }
                                                db.CloseDB();
                                            }
                                            else
                                            {
                                                SendReturnCode(mySocket, myClient, "0", false);
                                            }
                                        }

                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region CMD PINGDEVICE
                            case "PINGDEVICE?":
                                {
                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        if (localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected)
                                            SendReturnCode(mySocket, myClient, ReturnType.readerOk, false);
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                    }
                                    else // several reader ; search it
                                    {

                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                if (dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected)
                                                    SendReturnCode(mySocket, myClient, ReturnType.readerOk, false);
                                                else
                                                    SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region STATUS
                            case "STATUS?":
                                {

                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        if (localDeviceArray[0].bDataCompleted)
                                            SendReturnCode(mySocket, myClient, localDeviceArray[0].rfidDev.DeviceStatus.ToString(), false);
                                        else
                                            SendReturnCode(mySocket, myClient, DeviceStatus.DS_InScan.ToString(), false);
                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                if (dc.bDataCompleted)
                                                    SendReturnCode(mySocket, myClient, dc.rfidDev.DeviceStatus.ToString(), false);
                                                else
                                                    SendReturnCode(mySocket, myClient, DeviceStatus.DS_InScan.ToString(), false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region STATUS+Tag
                            case "STATUS_AND_TAG?":
                                {

                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        if ((localDeviceArray[0].rfidDev != null) &&
                                            (localDeviceArray[0].rfidDev.get_RFID_Device != null) &&
                                            (localDeviceArray[0].rfidDev.get_RFID_Device.ReaderData != null))
                                        {
                                            string ret = localDeviceArray[0].rfidDev.DeviceStatus.ToString() + ";" + localDeviceArray[0].rfidDev.get_RFID_Device.ReaderData.nbTagScan;
                                            SendReturnCode(mySocket, myClient, ret, false);
                                        }
                                        else
                                         SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                if ((dc.rfidDev != null) &&
                                                    (dc.rfidDev.get_RFID_Device != null) &&
                                                    (dc.rfidDev.get_RFID_Device.ReaderData != null))
                                                {
                                                    bFind = true;
                                                    string ret = dc.rfidDev.DeviceStatus.ToString() + ";" + dc.rfidDev.get_RFID_Device.ReaderData.nbTagScan;
                                                    SendReturnCode(mySocket, myClient, ret, false);
                                                }
                                                else
                                                SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region TagAt Index
                            case "GET_TAG_AT_INDEX?":
                                {
                                    if (nbArg == 3) // unique reader assume in index  0 of local array
                                    {
                                        string uid = null;
                                        string serialRFID = command[1];
                                        int nIndex = int.Parse(command[2]);
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;

                                                uid = string.Empty;



                                                if (nIndex < dc.rfidDev.get_RFID_Device.ReaderData.strListTag.Count)
                                                {
                                                    for (int i = nIndex; i < dc.rfidDev.get_RFID_Device.ReaderData.strListTag.Count; i++)
                                                    {
                                                        uid += dc.rfidDev.get_RFID_Device.ReaderData.strListTag[i].ToString() + tcpUtils.TCPDelimiter;
                                                    }
                                                    if (uid.Length > 1)
                                                        uid = uid.Substring(0, uid.Length - 1);
                                                    SendReturnCode(mySocket, myClient, uid, false);
                                                }
                                                else
                                                {
                                                    SendReturnCode(mySocket, myClient, ReturnType.noData, false);
                                                }

                                            }

                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                }
                                break;
                            #endregion
                            #region GET_LIGHT
                            case "GET_LIGHT_VALUE?":
                                {

                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        SendReturnCode(mySocket, myClient, localDeviceArray[0].rfidDev.get_RFID_Device.LightValue.ToString(), false);
                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                SendReturnCode(mySocket, myClient, dc.rfidDev.get_RFID_Device.LightValue.ToString(), false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region SET_LIGHT
                            case "SET_LIGHT_VALUE?":
                                {
                                    int power;
                                    if (nbArg == 2) // unique reader assume in index  0 of local array
                                    {
                                        if (!int.TryParse(command[1], out power))
                                            power = 0;
                                        if (power < 0) power = 0;
                                        if (power > 300) power = 300;
                                        if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                            (localDeviceArray[0].rfidDev.DeviceStatus != DeviceStatus.DS_InScan))
                                        {
                                            localDeviceArray[0].rfidDev.SetLight((ushort)power);
                                            SendReturnCode(mySocket, myClient, ReturnType.setLightOk, false);
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                        }
                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;

                                        if (!int.TryParse(command[1], out power))
                                            power = 0;
                                        if (power < 0) power = 0;
                                        if (power > 300) power = 300;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                if ((dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                                    (dc.rfidDev.DeviceStatus != DeviceStatus.DS_InScan))
                                                {
                                                    dc.rfidDev.SetLight((ushort)power);
                                                    SendReturnCode(mySocket, myClient, ReturnType.setLightOk, false);
                                                }
                                                else
                                                {
                                                    SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                                }
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region SCAN
                            case "SCAN?":
                                {
                                    requestScanFromServer = true;
                                    accumulateMode = false;
                                    eventScanStart.Reset();
                                    eventScanCancelled.Reset();
                                    eventScanCompleted.Reset();

                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                         (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                        {

                                            localDeviceArray[0].rfidDev.ScanDevice(false);
                                            if (eventScanStart.WaitOne(2500, false))
                                                SendReturnCode(mySocket, myClient, ReturnType.scanStarted, false);
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.failedToStartScan, false);
                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);

                                    }
                                    else
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                if ((dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                                 (dc.rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                                {
                                                    dc.rfidDev.ScanDevice(false);
                                                    if (eventScanStart.WaitOne(2500, false))
                                                        SendReturnCode(mySocket, myClient, ReturnType.scanStarted, false);
                                                    else
                                                        SendReturnCode(mySocket, myClient, ReturnType.failedToStartScan, false);
                                                }
                                                else
                                                    SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }

                            #endregion
                            #region SCANACCUMULATE
                            case "ACCUMULATE_SCAN?":
                                {
                                    requestScanFromServer = true;
                                    accumulateMode = true;
                                    eventScanStart.Reset();
                                    eventScanCancelled.Reset();
                                    eventScanCompleted.Reset();

                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        if (localDeviceArray[0].infoDev.deviceType == DeviceType.DT_SBR)
                                        {
                                            if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                             (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                            {

                                                localDeviceArray[0].rfidDev.ScanDevice(false);
                                                if (eventScanStart.WaitOne(2500, false))
                                                    SendReturnCode(mySocket, myClient, ReturnType.scanStarted, false);
                                                else
                                                    SendReturnCode(mySocket, myClient, ReturnType.failedToStartScan, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.wrongReader, false);
                                        }

                                    }
                                    else
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                if ((dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                                 (dc.rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                                {
                                                    dc.rfidDev.ScanDevice(false);
                                                    if (eventScanStart.WaitOne(2500, false))
                                                        SendReturnCode(mySocket, myClient, ReturnType.scanStarted, false);
                                                    else
                                                        SendReturnCode(mySocket, myClient, ReturnType.failedToStartScan, false);
                                                }
                                                else
                                                    SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }

                            #endregion
                            #region STOP_ACCUMULATE
                            case "STOP_ACCUMULATE_SCAN?":
                                {
                                    accumulateMode = false;
                                    SendReturnCode(mySocket, myClient, ReturnType.stopAccumulate, false);
                                    break;
                                }
                            #endregion
                            #region SCAN_AND_WAIT_STR
                            case "SCAN_AND_WAIT_STR?":
                                {
                                    requestScanFromServer = true;
                                    eventScanStart.Reset();
                                    eventScanCancelled.Reset();
                                    eventScanCompleted.Reset();

                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {

                                        if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                       (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                        {
                                            localDeviceArray[0].rfidDev.ScanDevice(false);

                                            if (eventScanStart.WaitOne(2500, false))
                                            {
                                                int timeout = (int)localDeviceArray[0].rfidDev.get_RFID_Device.ScanTimeout;
                                                if (eventScanCompleted.WaitOne(timeout, false))
                                                {
                                                    string ret = "OK;";
                                                    ret += localDeviceArray[0].currentInventory.eventDate.ToString("u") + ";";
                                                    ret += localDeviceArray[0].currentInventory.userFirstName + ";";
                                                    ret += localDeviceArray[0].currentInventory.userLastName + ";";
                                                    ret += localDeviceArray[0].currentInventory.userDoor.ToString() + ";";
                                                    ret += localDeviceArray[0].currentInventory.nbTagAll.ToString();

                                                    foreach (string uid in localDeviceArray[0].currentInventory.listTagAll)
                                                    {
                                                        ret += ";" + uid;
                                                    }
                                                    SendReturnCode(mySocket, myClient, ret, false);
                                                }
                                                else
                                                    SendReturnCode(mySocket, myClient, ReturnType.errorDuringScan, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.failedToStartScan, false);
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, localDeviceArray[0].rfidDev.DeviceStatus.ToString(), false);
                                        }


                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                serialRFID = command[1];
                                                if ((dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                                  (dc.rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                                {

                                                    dc.rfidDev.ScanDevice(false);
                                                    if (eventScanStart.WaitOne(2500, false))
                                                    {
                                                        int timeout = (int)dc.rfidDev.get_RFID_Device.ScanTimeout;
                                                        if (eventScanCompleted.WaitOne(timeout, false))
                                                        {
                                                            string ret = "OK;";
                                                            ret += dc.currentInventory.eventDate.ToString("u") + ";";
                                                            ret += dc.currentInventory.userFirstName + ";";
                                                            ret += dc.currentInventory.userLastName + ";";
                                                            ret += dc.currentInventory.userDoor.ToString() + ";";
                                                            ret += dc.currentInventory.nbTagAll.ToString();

                                                            foreach (string uid in dc.currentInventory.listTagAll)
                                                            {
                                                                ret += ";" + uid;
                                                            }
                                                            SendReturnCode(mySocket, myClient, ret, false);
                                                        }
                                                        else
                                                            SendReturnCode(mySocket, myClient, ReturnType.errorDuringScan, false);
                                                    }
                                                    else
                                                        SendReturnCode(mySocket, myClient, ReturnType.failedToStartScan, false);
                                                }
                                                else
                                                    SendReturnCode(mySocket, myClient, dc.rfidDev.DeviceStatus.ToString(), false);

                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }

                            #endregion
                            #region SCAN_AND_WAIT
                            case "SCAN_AND_WAIT?":
                                {
                                    requestScanFromServer = true;
                                    eventScanStart.Reset();
                                    eventScanCancelled.Reset();
                                    eventScanCompleted.Reset();

                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {

                                        if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                       (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                        {
                                            localDeviceArray[0].rfidDev.ScanDevice(false);

                                            if (eventScanStart.WaitOne(2500, false))
                                            {
                                                int timeout = (int)localDeviceArray[0].rfidDev.get_RFID_Device.ScanTimeout;
                                                if (eventScanCompleted.WaitOne(timeout, false))
                                                {
                                                    Hashtable ColumnInfo = null;
                                                    MainDBClass db2 = new MainDBClass();

                                                    if (db2.OpenDB())
                                                    {
                                                        ColumnInfo = db2.GetColumnInfo();
                                                        //StoredInventoryData siv = ConvertInventory.ConvertForStore(localDeviceArray[0].currentInventory, ColumnInfo);
                                                        StoredInventoryData siv = ConvertInventory.ConvertForStore(localDeviceArray[0].currentInventory);
                                                        if (siv != null)
                                                        {
                                                            BinaryFormatter bf = new BinaryFormatter();
                                                            MemoryStream mem = new MemoryStream();
                                                            bf.Serialize(mem, siv);
                                                            string idStream = Convert.ToBase64String(mem.ToArray());
                                                            SendReturnCode(mySocket, myClient, idStream, false);
                                                        }
                                                        else
                                                        {
                                                            string info = string.Format("Added : {0}/{1} , Present : {2}/{3} , Removed : {4}/{5}", localDeviceArray[0].currentInventory.nbTagAdded, localDeviceArray[0].currentInventory.dtTagAdded.Rows.Count, localDeviceArray[0].currentInventory.nbTagPresent, localDeviceArray[0].currentInventory.dtTagPresent.Rows.Count, localDeviceArray[0].currentInventory.nbTagRemoved, localDeviceArray[0].currentInventory.dtTagRemove.Rows.Count);
                                                            ErrorMessage.ExceptionMessageBox.Show("Error After Convert for Store", info, "Info in  server cmd SCAN_AND_WAIT?");
                                                            SendReturnCode(mySocket, myClient, "Error during processing data", false);

                                                        }
                                                    }
                                                    db2.CloseDB();
                                                }
                                                else
                                                    SendReturnCode(mySocket, myClient, "Error During Scan", false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, "Error Start Scan", false);
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, localDeviceArray[0].rfidDev.DeviceStatus.ToString(), false);
                                        }


                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                serialRFID = command[1];
                                                if ((dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                                  (dc.rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                                {

                                                    dc.rfidDev.ScanDevice(false);
                                                    if (eventScanStart.WaitOne(2500, false))
                                                    {
                                                        int timeout = (int)dc.rfidDev.get_RFID_Device.ScanTimeout;
                                                        if (eventScanCompleted.WaitOne(timeout, false))
                                                        {
                                                            Hashtable ColumnInfo = null;
                                                            MainDBClass db2 = new MainDBClass();

                                                            if (db2.OpenDB())
                                                            {
                                                                ColumnInfo = db2.GetColumnInfo();
                                                                //StoredInventoryData siv = ConvertInventory.ConvertForStore(dc.currentInventory, ColumnInfo);
                                                                StoredInventoryData siv = ConvertInventory.ConvertForStore(dc.currentInventory);
                                                                if (siv != null)
                                                                {
                                                                    BinaryFormatter bf = new BinaryFormatter();
                                                                    MemoryStream mem = new MemoryStream();
                                                                    bf.Serialize(mem, siv);
                                                                    string idStream = Convert.ToBase64String(mem.ToArray());
                                                                    SendReturnCode(mySocket, myClient, idStream, false);
                                                                }
                                                                else
                                                                {
                                                                    string info = string.Format("Added : {0}/{1} , Present : {2}/{3} , Removed : {4}/{5}", dc.currentInventory.nbTagAdded, dc.currentInventory.nbTagPresent, localDeviceArray[0].currentInventory.dtTagPresent.Rows.Count, dc.currentInventory.nbTagRemoved, dc.currentInventory.dtTagRemove.Rows.Count);
                                                                    ErrorMessage.ExceptionMessageBox.Show("Error After Convert for Store", info, "Info in  server cmd SCAN_AND_WAIT?");
                                                                    SendReturnCode(mySocket, myClient, "Error during processing data", false);
                                                                }
                                                            }
                                                        }
                                                        else
                                                            SendReturnCode(mySocket, myClient, "Error During Scan", false);
                                                    }
                                                    else
                                                        SendReturnCode(mySocket, myClient, "Error Start Scan", false);
                                                }
                                                else
                                                    SendReturnCode(mySocket, myClient, dc.rfidDev.DeviceStatus.ToString(), false);

                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, "Reader Not Exist", false);
                                    }
                                    break;
                                }

                            #endregion
                            #region STOP_SCAN
                            case "STOP_SCAN?":
                                {
                                    eventScanStart.Reset();
                                    eventScanCancelled.Reset();
                                    eventScanCompleted.Reset();

                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                         (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_InScan))
                                        {

                                            localDeviceArray[0].rfidDev.StopScan();
                                            eventScanCancelled.WaitOne(2500, false);
                                            SendReturnCode(mySocket, myClient, localDeviceArray[0].rfidDev.DeviceStatus.ToString(), false);
                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, localDeviceArray[0].rfidDev.DeviceStatus.ToString(), false);
                                    }
                                    else
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                if ((dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                                 (dc.rfidDev.DeviceStatus == DeviceStatus.DS_InScan))
                                                {
                                                    dc.rfidDev.StopScan();
                                                    eventScanCancelled.WaitOne(2500, false);
                                                    SendReturnCode(mySocket, myClient, dc.rfidDev.DeviceStatus.ToString(), false);
                                                }
                                                else
                                                    SendReturnCode(mySocket, myClient, dc.rfidDev.DeviceStatus.ToString(), false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region GET_LAST_SCAN_STR
                            case "GET_LAST_SCAN_STR?":
                                {
                                    bool bFind = false;
                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {

                                        if (localDeviceArray[0].rfidDev.DeviceStatus != DeviceStatus.DS_Ready)
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.ReaderNotInReadyState, false);
                                            break;
                                        }
                                        string ret = "OK;";
                                        ret += localDeviceArray[0].currentInventory.eventDate.ToString("MM/dd/yyyy h:mm tt") + ";";
                                        ret += localDeviceArray[0].currentInventory.userFirstName + ";";
                                        ret += localDeviceArray[0].currentInventory.userLastName + ";";
                                        ret += localDeviceArray[0].currentInventory.nbTagAll.ToString();

                                        foreach (string uid in localDeviceArray[0].currentInventory.listTagAll)
                                        {
                                            ret += ";" + uid;
                                        }
                                        SendReturnCode(mySocket, myClient, ret, false);

                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];

                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                if (dc.rfidDev.DeviceStatus != DeviceStatus.DS_Ready)
                                                {
                                                    SendReturnCode(mySocket, myClient, ReturnType.ReaderNotInReadyState, false);
                                                    break;
                                                }
                                                bFind = true;
                                                string ret = "OK;";
                                                ret += dc.currentInventory.eventDate.ToString("MM/dd/yyyy h:mm tt") + ";";
                                                ret += dc.currentInventory.userFirstName + ";";
                                                ret += dc.currentInventory.userLastName + ";";
                                                ret += dc.currentInventory.nbTagAll.ToString();

                                                foreach (string uid in dc.currentInventory.listTagAll)
                                                {
                                                    ret += ";" + uid;
                                                }
                                                SendReturnCode(mySocket, myClient, ret, false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region GET_LAST_SCAN
                            case "GET_LAST_SCAN?":
                                {
                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {

                                        if (localDeviceArray[0].rfidDev.DeviceStatus != DeviceStatus.DS_Ready)
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.ReaderNotInReadyState, false);
                                            break;
                                        }

                                        Hashtable ColumnInfo = null;
                                        MainDBClass db3 = new MainDBClass();

                                        if (db3.OpenDB())
                                        {
                                            ColumnInfo = db3.GetColumnInfo();
                                            //StoredInventoryData siv = ConvertInventory.ConvertForStore(localDeviceArray[0].currentInventory, ColumnInfo);
                                            StoredInventoryData siv = ConvertInventory.ConvertForStore(localDeviceArray[0].currentInventory);
                                            if (siv != null)
                                            {
                                                BinaryFormatter bf = new BinaryFormatter();
                                                MemoryStream mem = new MemoryStream();
                                                bf.Serialize(mem, siv);
                                                string idStream = Convert.ToBase64String(mem.ToArray());
                                                SendReturnCode(mySocket, myClient, idStream, false);
                                            }
                                            else
                                            {
                                                string info = string.Format("Added : {0}/{1} , Present : {2}/{3} , Removed : {4}/{5}", localDeviceArray[0].currentInventory.nbTagAdded, localDeviceArray[0].currentInventory.dtTagAdded.Rows.Count, localDeviceArray[0].currentInventory.nbTagPresent, localDeviceArray[0].currentInventory.dtTagPresent.Rows.Count, localDeviceArray[0].currentInventory.nbTagRemoved, localDeviceArray[0].currentInventory.dtTagRemove.Rows.Count);
                                                ErrorMessage.ExceptionMessageBox.Show("Error After Convert for Store", info, "Info in  server cmd GET_LAST_SCAN");
                                                SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                            }
                                        }
                                        db3.CloseDB();

                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];

                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {

                                                if (dc.rfidDev.DeviceStatus != DeviceStatus.DS_Ready)
                                                {
                                                    SendReturnCode(mySocket, myClient, ReturnType.ReaderNotInReadyState, false);
                                                    break;
                                                }

                                                Hashtable ColumnInfo = null;
                                                MainDBClass db3 = new MainDBClass();

                                                if (db3.OpenDB())
                                                {
                                                    ColumnInfo = db3.GetColumnInfo();
                                                    //StoredInventoryData siv = ConvertInventory.ConvertForStore(dc.currentInventory, ColumnInfo);
                                                    StoredInventoryData siv = ConvertInventory.ConvertForStore(dc.currentInventory);
                                                    if (siv != null)
                                                    {
                                                        BinaryFormatter bf = new BinaryFormatter();
                                                        MemoryStream mem = new MemoryStream();
                                                        bf.Serialize(mem, siv);
                                                        string idStream = Convert.ToBase64String(mem.ToArray());
                                                        SendReturnCode(mySocket, myClient, idStream, false);
                                                    }

                                                    else
                                                    {
                                                        string info = string.Format("Added : {0}/{1} , Present : {2}/{3} , Removed : {4}/{5}", dc.currentInventory.nbTagAdded, dc.currentInventory.dtTagAdded.Rows.Count, dc.currentInventory.nbTagPresent, dc.currentInventory.dtTagPresent.Rows.Count, dc.currentInventory.nbTagRemoved, dc.currentInventory.dtTagRemove.Rows.Count);
                                                        ErrorMessage.ExceptionMessageBox.Show("Error After Convert for Store", info, "Info in  server cmd GET_LAST_SCAN");
                                                        SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                                    }
                                                }
                                                db3.CloseDB();
                                            }
                                        }
                                    }
                                    break;
                                }

                            #endregion
                            #region GET_SCAN_FROM_ID
                            case "GET_SCAN_FROM_ID?":
                                {
                                    if (nbArg == 2) // unique reader assume in index  0 of local array
                                    {

                                        /* if (localDeviceArray[0].rfidDev.DeviceStatus != DeviceStatus.DS_Ready)
                                         {
                                             SendReturnCode(mySocket, myClient, ReturnType.ReaderNotInReadyState, false);
                                             break;
                                         }*/

                                        Hashtable ColumnInfo = null;
                                        MainDBClass db3 = new MainDBClass();
                                        string IdEventStr = command[1];

                                        if (db3.OpenDB())
                                        {
                                            ColumnInfo = db3.GetColumnInfo();
                                            InventoryData tmpInv = db3.GetLastScanFromID(localDeviceArray[0].infoDev.SerialRFID, int.Parse(IdEventStr));
                                            //StoredInventoryData siv = ConvertInventory.ConvertForStore(tmpInv, ColumnInfo);
                                            StoredInventoryData siv = ConvertInventory.ConvertForStore(tmpInv);
                                            if (siv != null)
                                            {
                                                BinaryFormatter bf = new BinaryFormatter();
                                                MemoryStream mem = new MemoryStream();
                                                bf.Serialize(mem, siv);
                                                string idStream = Convert.ToBase64String(mem.ToArray());
                                                SendReturnCode(mySocket, myClient, idStream, false);
                                            }
                                            else
                                            {
                                                string info = string.Format("Added : {0}/{1} , Present : {2}/{3} , Removed : {4}/{5}", localDeviceArray[0].currentInventory.nbTagAdded, localDeviceArray[0].currentInventory.dtTagAdded.Rows.Count, localDeviceArray[0].currentInventory.nbTagPresent, localDeviceArray[0].currentInventory.dtTagPresent.Rows.Count, localDeviceArray[0].currentInventory.nbTagRemoved, localDeviceArray[0].currentInventory.dtTagRemove.Rows.Count);
                                                ErrorMessage.ExceptionMessageBox.Show("Error After Convert for Store", info, "Info in  server cmd GET_LAST_SCAN");
                                                SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                            }
                                        }
                                        db3.CloseDB();

                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        string IdEventStr = command[2];

                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {

                                                if (dc.rfidDev.DeviceStatus != DeviceStatus.DS_Ready)
                                                {
                                                    SendReturnCode(mySocket, myClient, ReturnType.ReaderNotInReadyState, false);
                                                    break;
                                                }

                                                Hashtable ColumnInfo = null;
                                                MainDBClass db3 = new MainDBClass();

                                                if (db3.OpenDB())
                                                {
                                                    ColumnInfo = db3.GetColumnInfo();
                                                    InventoryData tmpInv = db3.GetLastScanFromID(serialRFID, int.Parse(IdEventStr));
                                                    //StoredInventoryData siv = ConvertInventory.ConvertForStore(tmpInv, ColumnInfo);
                                                    StoredInventoryData siv = ConvertInventory.ConvertForStore(tmpInv);
                                                    if (siv != null)
                                                    {
                                                        BinaryFormatter bf = new BinaryFormatter();
                                                        MemoryStream mem = new MemoryStream();
                                                        bf.Serialize(mem, siv);
                                                        string idStream = Convert.ToBase64String(mem.ToArray());
                                                        SendReturnCode(mySocket, myClient, idStream, false);
                                                    }

                                                    else
                                                    {
                                                        string info = string.Format("Added : {0}/{1} , Present : {2}/{3} , Removed : {4}/{5}", dc.currentInventory.nbTagAdded, dc.currentInventory.dtTagAdded.Rows.Count, dc.currentInventory.nbTagPresent, dc.currentInventory.dtTagPresent.Rows.Count, dc.currentInventory.nbTagRemoved, dc.currentInventory.dtTagRemove.Rows.Count);
                                                        ErrorMessage.ExceptionMessageBox.Show("Error After Convert for Store", info, "Info in  server cmd GET_LAST_SCAN");
                                                        SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                                    }
                                                }
                                                db3.CloseDB();
                                            }
                                        }
                                    }
                                    break;
                                }

                            #endregion
                            #region GET_LAST_SCAN_DATE
                            case "GET_LAST_SCAN_DATE?":
                                {
                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        DateTime utcDate = localDeviceArray[0].lastProcessInventoryGmtDate;
                                        SendReturnCode(mySocket, myClient, utcDate.ToString("u"), false);
                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                DateTime utcDate = dc.lastProcessInventoryGmtDate;
                                                SendReturnCode(mySocket, myClient, utcDate.ToString("u"), false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region GET_LAST_SCAN_ID
                            case "GET_LAST_SCAN_ID?":
                                {
                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        int IdScan = -1;
                                        try
                                        {
                                            IdScan = localDeviceArray[0].currentInventory.IdScanEvent;
                                        }
                                        catch
                                        {
                                        }
                                        SendReturnCode(mySocket, myClient, IdScan.ToString(), false);
                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                int IdScan = -1;
                                                try
                                                {
                                                    IdScan = dc.currentInventory.IdScanEvent;
                                                }
                                                catch
                                                {
                                                }
                                                SendReturnCode(mySocket, myClient, IdScan.ToString(), false);
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region GET_SCAN_FROM_DATE
                            case "GET_SCAN_FROM_DATE?":
                                {
                                    string serialRFID;
                                    string date;
                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {

                                        if (nbArg == 2)
                                        {
                                            serialRFID = localDeviceArray[0].infoDev.SerialRFID;
                                            date = command[1];
                                        }
                                        else
                                        {
                                            serialRFID = command[1];
                                            date = command[2];
                                        }

                                        string[] invd = db.GetInventory(serialRFID, date);

                                        if (invd != null)
                                        {
                                            SendReturnCode(mySocket, myClient, invd.Length.ToString(), false);
                                            foreach (string str in invd)
                                            {
                                                SendReturnCode(mySocket, myClient, str, false);
                                            }
                                            db.CloseDB();
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, "0", false);
                                        }
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    break;
                                }
                            #endregion
                            #region GET_SCAN_FROM_SPAREDATA?
                            case "GET_SCAN_FROM_SPAREDATA?":
                                {
                                    string serialRFID;
                                    string sp1, sp2;
                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {

                                        if (nbArg == 3)
                                        {
                                            serialRFID = localDeviceArray[0].infoDev.SerialRFID;
                                            sp1 = command[1];
                                            sp2 = command[2];
                                        }
                                        else
                                        {
                                            serialRFID = command[1];
                                            sp1 = command[2];
                                            sp2 = command[3];
                                        }

                                        string[] invd = db.GetInventoryFromData(serialRFID, sp1, sp2);

                                        if (invd != null)
                                        {
                                            SendReturnCode(mySocket, myClient, invd.Length.ToString(), false);
                                            foreach (string str in invd)
                                            {
                                                SendReturnCode(mySocket, myClient, str, false);
                                            }
                                            db.CloseDB();
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, "0", false);
                                        }
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    break;
                                }
                            #endregion
                            #region ADD_USER_FINGER
                            case "ADD_USER_FINGER?":
                                {
                                    string FirstName = command[1];
                                    string LastName = command[2];
                                    string fingerIndex = command[3];
                                    string fingerHexa = command[4];

                                    int fIndex = int.Parse(fingerIndex);
                                    byte[] template = tcpUtils.HexStringToByteArray(fingerHexa);

                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {

                                        UserClassTemplate user = db.RecoverUser(FirstName, LastName);

                                        if (user != null)
                                        {
                                            UserClass TheUser = new UserClass();
                                            BinaryFormatter bf = new BinaryFormatter();
                                            MemoryStream mem = new MemoryStream(Convert.FromBase64String(user.template));
                                            TheUser = (UserClass)bf.Deserialize(mem);

                                            fingerUtils.AddTemplateToUser(TheUser, template, fIndex);

                                            BinaryFormatter bf2 = new BinaryFormatter();
                                            MemoryStream mem2 = new MemoryStream();
                                            bf2.Serialize(mem2, TheUser);
                                            string NewTemplate = Convert.ToBase64String(mem2.ToArray());

                                            user.template = NewTemplate;
                                            user.isFingerEnrolled[fIndex] = true;
                                            db.StoreUser(user);
                                        }
                                        else
                                        {
                                            UserClassTemplate newUser = new UserClassTemplate();
                                            newUser.firstName = FirstName;
                                            newUser.lastName = LastName;

                                            UserClass newUserFp = new UserClass();
                                            newUserFp.firstName = FirstName;
                                            newUserFp.lastName = LastName;

                                            fingerUtils.AddTemplateToUser(newUserFp, template, fIndex);

                                            BinaryFormatter bf2 = new BinaryFormatter();
                                            MemoryStream mem2 = new MemoryStream();
                                            bf2.Serialize(mem2, newUserFp);
                                            string NewTemplate = Convert.ToBase64String(mem2.ToArray());

                                            newUser.template = NewTemplate;
                                            newUser.isFingerEnrolled[fIndex] = true;
                                            db.StoreUser(newUser);

                                        }
                                        needrefreshUser = true;
                                        SendReturnCode(mySocket, myClient, ReturnType.addUserFinger, false);
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    break;
                                }
                            #endregion
                            #region ADD_USER_FROM_TEMPLATE
                            case "ADD_USER_FROM_TEMPLATE?":
                                {

                                    string FirstName = command[1];
                                    string LastName = command[2];
                                    string template = command[3];
                                    string BadgeReaderID = null;
                                    if (command.Length == 5)
                                        BadgeReaderID = command[4];

                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {

                                        UserClassTemplate user = db.RecoverUser(FirstName, LastName);

                                        if (user != null)
                                        {
                                            user.template = template;
                                            if (!string.IsNullOrEmpty(BadgeReaderID))
                                                user.BadgeReaderID = BadgeReaderID;
                                            db.StoreUser(user);
                                        }
                                        else
                                        {
                                            UserClassTemplate newUser = new UserClassTemplate();
                                            newUser.firstName = FirstName;
                                            newUser.lastName = LastName;
                                            newUser.template = template;
                                            if (!string.IsNullOrEmpty(BadgeReaderID))
                                                newUser.BadgeReaderID = BadgeReaderID;
                                            db.StoreUser(newUser);

                                        }
                                        needrefreshUser = true;
                                        SendReturnCode(mySocket, myClient, ReturnType.addUserTemplate, false);
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    break;
                                }
                            #endregion
                            #region ADD_USER_GRANT
                            case "ADD_USER_GRANT?":
                                {
                                    string FirstName = command[1];
                                    string LastName = command[2];
                                    string serialRFID = command[3];
                                    string userGrant = command[4];  //int of the enum

                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        if ((FirstName.Equals("ALL")) && (LastName.Equals("ALL")))
                                        {
                                            UserClassTemplate[] userArray = db.RecoverUser();
                                            if (userArray != null)
                                            {
                                                foreach (UserClassTemplate uct in userArray)
                                                {
                                                    int reg = 3;
                                                    int.TryParse(userGrant, out reg);
                                                    db.StoreGrant(uct, serialRFID, null, (UserGrant)reg);
                                                }
                                                needrefreshUser = true;
                                                SendReturnCode(mySocket, myClient, ReturnType.addUserGrant, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.unknownUser, false);
                                        }
                                        else
                                        {
                                            UserClassTemplate user = db.RecoverUser(FirstName, LastName);

                                            if (user != null)
                                            {
                                                int reg = 3;
                                                int.TryParse(userGrant, out reg);
                                                db.StoreGrant(user, serialRFID, null, (UserGrant)reg);
                                                needrefreshUser = true;
                                                SendReturnCode(mySocket, myClient, ReturnType.addUserGrant, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.unknownUser, false);
                                        }
                                    }
                                    else
                                    {
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }

                                    break;
                                }
                            #endregion
                            #region DEL_USER_GRANT
                            case "DEL_USER_GRANT?":
                                {
                                    string FirstName = command[1];
                                    string LastName = command[2];
                                    string serialRFID = command[3];

                                    spareData1 = null;
                                    spareData2 = null;

                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        if ((FirstName.Equals("ALL")) && (LastName.Equals("ALL")))
                                        {
                                            UserClassTemplate[] userArray = db.RecoverUser();
                                            if (userArray != null)
                                            {
                                                foreach (UserClassTemplate uct in userArray)
                                                {
                                                    db.DeleteUserGrant(uct, serialRFID);
                                                    SendReturnCode(mySocket, myClient, ReturnType.delUserGrant, false);
                                                }
                                                needrefreshUser = true;
                                            }
                                            else
                                            {
                                                SendReturnCode(mySocket, myClient, ReturnType.unknownUser, false);
                                            }
                                        }
                                        else
                                        {
                                            UserClassTemplate user = db.RecoverUser(FirstName, LastName);

                                            if (user != null)
                                            {
                                                db.DeleteUserGrant(user, serialRFID);
                                                needrefreshUser = true;
                                                SendReturnCode(mySocket, myClient, ReturnType.delUserGrant, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.unknownUser, false);
                                        }
                                    }
                                    else
                                    {
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }

                                    break;
                                }
                            #endregion
                            #region DEL_USER
                            case "DEL_USER?":
                                {
                                    string FirstName = command[1];
                                    string LastName = command[2];
                                    string serialRFID = command[3];


                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        UserClassTemplate user = db.RecoverUser(FirstName, LastName);

                                        if (user != null)
                                        {
                                            db.DeleteUser(FirstName, LastName);
                                            needrefreshUser = true;
                                            SendReturnCode(mySocket, myClient, ReturnType.delUser, false);
                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.unknownUser, false);
                                    }
                                    else
                                    {
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }

                                    break;
                                }
                            #endregion
                            #region ADD_USER_BADGE
                            case "ADD_USER_BADGE?":
                                {
                                    string FirstName = command[1];
                                    string LastName = command[2];
                                    string badgeID = command[3];


                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        UserClassTemplate user = db.RecoverUser(FirstName, LastName);

                                        if (user != null)
                                        {
                                            user.BadgeReaderID = badgeID;
                                            db.StoreUser(user);
                                            needrefreshUser = true;
                                            SendReturnCode(mySocket, myClient, ReturnType.addUserBadge, false);
                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.delUserBadge, false);
                                    }
                                    else
                                    {
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }

                                    break;
                                }
                            #endregion
                            #region DEL_USER_BADGE
                            case "DEL_USER_BADGE?":
                                {
                                    string FirstName = command[1];
                                    string LastName = command[2];

                                    spareData1 = null;
                                    spareData2 = null;

                                    db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        UserClassTemplate user = db.RecoverUser(FirstName, LastName);

                                        if (user != null)
                                        {
                                            user.BadgeReaderID = null;
                                            db.StoreUser(user);
                                            needrefreshUser = true;
                                            SendReturnCode(mySocket, myClient, ReturnType.delUserBadge, false);
                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.unknownUser, false);
                                    }
                                    else
                                    {
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }

                                    break;
                                }
                            #endregion
                            #region SET_WAIT_MODE
                            case "SET_WAIT_MODE?":
                                {
                                    accumulateMode = false;
                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {

                                        if (localDeviceArray[0].infoDev.deviceType == DeviceType.DT_SBR)
                                        {

                                            if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                             (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                            {
                                                if (localDeviceArray[0].rfidDev.EnableWaitMode())
                                                    SendReturnCode(mySocket, myClient, ReturnType.waitModeStarted, false);
                                                else
                                                    SendReturnCode(mySocket, myClient, ReturnType.failedWaitMode, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.wrongReader, false);
                                        }
                                    }
                                    else
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                if (dc.infoDev.deviceType == DeviceType.DT_SBR)
                                                {
                                                    bFind = true;
                                                    if ((dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                                     (dc.rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                                    {
                                                        bFind = true;
                                                        if (dc.rfidDev.EnableWaitMode())
                                                            SendReturnCode(mySocket, myClient, ReturnType.waitModeStarted, false);
                                                        else
                                                            SendReturnCode(mySocket, myClient, ReturnType.failedWaitMode, false);
                                                    }
                                                    else
                                                        SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                                }
                                                else
                                                {
                                                    SendReturnCode(mySocket, myClient, ReturnType.wrongReader, false);
                                                }
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    needrefreshTree = true;
                                    break;
                                }
                            #endregion
                            #region UNSET_WAIT_MODE
                            case "UNSET_WAIT_MODE?":
                                {
                                    accumulateMode = false;
                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {

                                        if (localDeviceArray[0].infoDev.deviceType == DeviceType.DT_SBR)
                                        {

                                            if ((localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                             (localDeviceArray[0].rfidDev.DeviceStatus == DeviceStatus.DS_WaitTag))
                                            {
                                                if (localDeviceArray[0].rfidDev.DisableWaitMode())
                                                    SendReturnCode(mySocket, myClient, ReturnType.waitModeStopped, false);
                                                else
                                                    SendReturnCode(mySocket, myClient, ReturnType.failedWaitModeOff, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.readerNotInWait, false);
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.wrongReader, false);
                                        }


                                    }
                                    else
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                if (dc.infoDev.deviceType == DeviceType.DT_SBR)
                                                {
                                                    bFind = true;
                                                    if ((dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                                     (dc.rfidDev.DeviceStatus == DeviceStatus.DS_WaitTag))
                                                    {
                                                        if (dc.rfidDev.DisableWaitMode())
                                                        {
                                                            SendReturnCode(mySocket, myClient, ReturnType.waitModeStopped, false);
                                                        }
                                                        else
                                                            SendReturnCode(mySocket, myClient, ReturnType.failedWaitModeOff, false);
                                                    }
                                                    else
                                                        SendReturnCode(mySocket, myClient, ReturnType.readerNotInWait, false);
                                                }
                                                else
                                                {
                                                    SendReturnCode(mySocket, myClient, ReturnType.wrongReader, false);
                                                }
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    needrefreshTree = true;
                                    break;
                                }
                            #endregion
                            #region GET_DEVICE_STR
                            case "GET_DEVICE_STR?":
                                {
                                    if (localDeviceArray != null)
                                    {
                                        SendReturnCode(mySocket, myClient, localDeviceArray.Length.ToString(), false);

                                        if (localDeviceArray.Length > 0)
                                        {
                                            foreach (deviceClass dc in localDeviceArray)
                                            {
                                                string ret;
                                                int type = (int)dc.infoDev.deviceType;
                                                if (dc.rfidDev.get_RFID_Device != null)
                                                {

                                                    ret = dc.infoDev.SerialRFID + ";" + type.ToString() + ";" + dc.rfidDev.get_RFID_Device.FirmwareVersion + ";" + dc.rfidDev.get_RFID_Device.HardwareVersion;
                                                }
                                                else
                                                {
                                                    ret = dc.infoDev.SerialRFID + ";" + type.ToString();
                                                }

                                                SendReturnCode(mySocket, myClient, ret, false);
                                            }
                                        }
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, "0", false);
                                    break;
                                }
                            #endregion
                            #region FLASH FIRMWARE
                            case "FLASH_FIRMWARE?":
                                {
                                    string serialRFID = command[1];
                                    string firmwareFile = command[2];
                                    bool bFind = false;

                                    foreach (deviceClass dc in localDeviceArray)
                                    {
                                        if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                        {
                                            bFind = true;
                                            if ((dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                                (dc.rfidDev.DeviceStatus == DeviceStatus.DS_Ready))
                                            {
                                                string path = Convert.ToString(Registry.GetValue(regKey, regValueInstall, null));
                                                if (!string.IsNullOrEmpty(path))
                                                {
                                                    string hexPath = path + Path.DirectorySeparatorChar + "Firmware" + Path.DirectorySeparatorChar + firmwareFile;
                                                    if (File.Exists(hexPath))
                                                    {
                                                        dc.rfidDev.get_RFID_Device.FlashFirmware(hexPath);
                                                        SendReturnCode(mySocket, myClient, ReturnType.flashStarted, false);
                                                    }
                                                    else
                                                        SendReturnCode(mySocket, myClient, ReturnType.fileNotexist, false);

                                                }
                                                SendReturnCode(mySocket, myClient, ReturnType.errorPath, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                        }
                                    }
                                    if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    break;
                                }

                            #endregion
                            #region Fridge Actual temp
                            case "GET_FRIDGE_CURRENT_TEMP?":
                                {

                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        if ((localDeviceArray[0].infoDev.deviceType == DeviceType.DT_SFR) || (localDeviceArray[0].infoDev.deviceType == DeviceType.DT_SBF))
                                        {
                                            if (localDeviceArray[0].myFridgeCabinet.GetTempInfo != null)
                                            {
                                                BinaryFormatter bf = new BinaryFormatter();
                                                MemoryStream mem = new MemoryStream();
                                                bf.Serialize(mem, localDeviceArray[0].myFridgeCabinet.GetTempInfo);
                                                string idTemp = Convert.ToBase64String(mem.ToArray());
                                                SendReturnCode(mySocket, myClient, idTemp, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.noData, false);
                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.wrongReader, false);
                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                if ((dc.infoDev.deviceType == DeviceType.DT_SFR) || (dc.infoDev.deviceType == DeviceType.DT_SBF))
                                                {
                                                    if (dc.myFridgeCabinet.GetTempInfo != null)
                                                    {
                                                        BinaryFormatter bf = new BinaryFormatter();
                                                        MemoryStream mem = new MemoryStream();
                                                        bf.Serialize(mem, dc.myFridgeCabinet.GetTempInfo);
                                                        string idTemp = Convert.ToBase64String(mem.ToArray());
                                                        bFind = true;
                                                        SendReturnCode(mySocket, myClient, idTemp, false);
                                                        break;
                                                    }
                                                    else
                                                        SendReturnCode(mySocket, myClient, ReturnType.noData, false);
                                                    break;
                                                }
                                                else
                                                {
                                                    SendReturnCode(mySocket, myClient, ReturnType.wrongReader, false);
                                                    break;
                                                }
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region FanemInfo
                            case "GET_FANEM_INFO?":
                                {
                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        if ((localDeviceArray[0].infoDev.deviceType == DeviceType.DT_SBF) && (localDeviceArray[0].infoDev.fridgeType == FridgeType.FT_FANEM))
                                        {
                                            if (localDeviceArray[0].myFridgeCabinet.GetFridgeFanemInfo != null)
                                            {
                                                BinaryFormatter bf = new BinaryFormatter();
                                                MemoryStream mem = new MemoryStream();
                                                bf.Serialize(mem, localDeviceArray[0].myFridgeCabinet.GetFridgeFanemInfo);
                                                string idTemp = Convert.ToBase64String(mem.ToArray());
                                                SendReturnCode(mySocket, myClient, idTemp, false);
                                            }
                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.noData, false);
                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.wrongReader, false);
                                    }
                                    else // several reader ; search it
                                    {
                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                if ((dc.infoDev.deviceType == DeviceType.DT_SBF) && (dc.infoDev.fridgeType == FridgeType.FT_FANEM))
                                                {
                                                    if (dc.myFridgeCabinet.GetFridgeFanemInfo != null)
                                                    {
                                                        BinaryFormatter bf = new BinaryFormatter();
                                                        MemoryStream mem = new MemoryStream();
                                                        bf.Serialize(mem, dc.myFridgeCabinet.GetFridgeFanemInfo);
                                                        string idTemp = Convert.ToBase64String(mem.ToArray());
                                                        bFind = true;
                                                        SendReturnCode(mySocket, myClient, idTemp, false);
                                                        break;
                                                    }
                                                    else
                                                        SendReturnCode(mySocket, myClient, ReturnType.noData, false);
                                                    break;
                                                }
                                                else
                                                {
                                                    SendReturnCode(mySocket, myClient, ReturnType.wrongReader, false);
                                                    break;
                                                }
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                }
                            #endregion
                            #region GET_TEMP_FROM_DATE
                            case "GET_TEMP_FROM_DATE?":
                                {
                                    string serialRFID;
                                    string date;
                                    db = new MainDBClass();
                                    if (db.OpenDB())
                                    {
                                        if (nbArg == 2)
                                        {
                                            serialRFID = localDeviceArray[0].infoDev.SerialRFID;
                                            date = command[1];
                                        }
                                        else
                                        {
                                            serialRFID = command[1];
                                            date = command[2];
                                        }

                                        string[] tempArray = db.GetFridgeTempAfter(serialRFID, date);
                                        if (tempArray != null)
                                        {
                                            SendReturnCode(mySocket, myClient, tempArray.Length.ToString(), false);
                                            foreach (string str in tempArray)
                                            {
                                                SendReturnCode(mySocket, myClient, str, false);
                                            }
                                            db.CloseDB();
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, "0", false);
                                        }
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    break;
                                }
                            #endregion
                            #region setIp
                            case "SET_IP?":
                                {
                                    if (nbArg > 3)
                                    {
                                        string args;
                                        string network = command[1];
                                        int dhcp = int.Parse(command[2]);
                                        string NewIp = command[3];
                                        string NewMask = command[4];
                                        string gateway = "none";

                                        if (dhcp == 0)
                                        {
                                            if (nbArg == 6)
                                            {
                                                gateway = command[5];
                                                args = "interface ip set address name=\"" + network + "\" static addr=" + NewIp + " mask=" + NewMask + " gateway=" + gateway + " gwmetric=1";
                                            }
                                            else
                                                args = "interface ip set address name=\"" + network + "\" static addr=" + NewIp + " mask=" + NewMask;
                                        }

                                        else
                                        {
                                            args = "interface ip set address name=\"" + network + "\" source=dhcp";
                                        }
                                        try
                                        {
                                            Process netshProcess = new Process();
                                            netshProcess.StartInfo.FileName = "netsh.exe";
                                            netshProcess.StartInfo.Arguments = args;
                                            netshProcess.StartInfo.UseShellExecute = false;
                                            netshProcess.StartInfo.CreateNoWindow = true;
                                            netshProcess.StartInfo.RedirectStandardOutput = true;
                                            netshProcess.Start();
                                            netshProcess.WaitForExit(5000);
                                            string info = netshProcess.StandardOutput.ReadToEnd();
                                            //ErrorMessage.ExceptionMessageBox.Show(info, Properties.ResStrings.strInfo);
                                            //ErrorMessage.ExceptionMessageBox.Show(localDeviceArray[0].infoDev.SerialRFID + " IP change in : " + args, Properties.ResStrings.strInfo);
                                            SendReturnCode(mySocket, myClient, ReturnType.SetIP_OK, false);
                                        }
                                        catch (Exception exp)
                                        {
                                            ErrorMessage.ExceptionMessageBox.Show(exp);
                                            SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                        }                                       

                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.noData, false);
                                    break;
                                }

                            #endregion
                            #region GET_SQL_EXPORT
                                case "GET_SQL_EXPORT?":
                                {
                                    DB_Class_SQLite.DBClassSQLite dbSQlite = null;
                                    string connectionString = null;
                                   
                                    string host = string.Empty;
                                    string login = string.Empty;
                                    string pwd = string.Empty;
                                    string dbName = string.Empty;
                                    string tableName = string.Empty;
                                    int bActive = 0;
                                    try
                                    {
                                        dbSQlite = new DB_Class_SQLite.DBClassSQLite();
                                        dbSQlite.OpenDB();
                                        dbSQlite.getExportInfo(1, out connectionString, out tableName, out bActive);
                                        if (string.IsNullOrEmpty(connectionString))
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.noData, false);
                                        }
                                        else
                                        {
                                            string[] strArray = connectionString.Split(';');
                                            foreach (string str2 in strArray)
                                            {
                                                if (str2.Length == 0) break;
                                                string strSwitch = str2.Substring(0, str2.IndexOf('=')).ToUpper().Trim();
                                                switch (strSwitch)
                                                {
                                                    case "DATA SOURCE":
                                                        host = str2.Substring(str2.IndexOf('=') + 1);
                                                        break;

                                                    case "INITIAL CATALOG":
                                                        dbName = str2.Substring(str2.IndexOf('=') + 1);
                                                        break;

                                                    case "USER ID":
                                                        login = str2.Substring(str2.IndexOf('=') + 1);
                                                        break;

                                                    case "PASSWORD":
                                                        {
                                                            string str3 = DBClass_SQLServer.UtilSqlServer.DencryptPassword(str2.Substring(str2.IndexOf('=') + 1));
                                                            pwd = str3;
                                                            break;
                                                        }
                                                }
                                            }
                                            string ret = bActive.ToString();
                                            ret += ";" + host;
                                            ret += ";" + dbName;
                                            ret += ";" + login;
                                            ret += ";" + pwd;
                                            ret += ";" + tableName;
                                            SendReturnCode(mySocket, myClient, ret, false);

                                        }
                                    }
                                    catch
                                    {
                                        SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                    }
                                    finally
                                    {
                                        if ((dbSQlite != null) && (dbSQlite.isOpen()))
                                        {
                                            dbSQlite.CloseDB();
                                            dbSQlite = null;                                           
                                        }
                                    }
                                    break;
                                }
                            #endregion
                            #region SetSQLExport
                                case "SET_SQL_EXPORT?":
                                {

                                    DB_Class_SQLite.DBClassSQLite dbSQlite = null;
                                    try
                                    {
                                        bool bDataOk = true;
                                        for (int loop = 0; loop < command.Length; loop++)
                                        {
                                            if (string.IsNullOrEmpty(command[loop]))
                                            {
                                                bDataOk = false;
                                                SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                                break;
                                            }
                                        }

                                        if (bDataOk)
                                        {
                                            if (command.Length != 7)
                                                SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                            else
                                            {


                                                int bActive = Convert.ToInt32(command[1]);
                                                string host = command[2];
                                                string login = command[3];
                                                string pwd = command[4];
                                                string dbName = command[5];
                                                string tableName = command[6];


                                                string str2 = null;
                                                string str3 = DBClass_SQLServer.UtilSqlServer.EncryptPassword(pwd);
                                                str2 = "Data Source=" + host + ";Initial Catalog=" + dbName + ";User ID=" + login + ";Password=" + str3 + ";";

                                                dbSQlite = new DB_Class_SQLite.DBClassSQLite();
                                                dbSQlite.OpenDB();
                                                if (dbSQlite.AddExportInfo(1, str2, tableName, bActive))
                                                {
                                                    needrefreshSQL = true;
                                                    SendReturnCode(mySocket, myClient, ReturnType.SetSQL_OK, false);
                                                }
                                                else
                                                    SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);

                                            }
                                        }
                                    }
                                    catch
                                    {


                                    }
                                    finally
                                    {
                                        if ((dbSQlite != null) && (dbSQlite.isOpen()))
                                        {
                                            dbSQlite.CloseDB();
                                            dbSQlite = null;
                                        }
                                    }

                                    break;
                                }
                            #endregion
                            #region TEST_SQL_EXPORT
                                case "TEST_SQL_EXPORT?":
                                {
                                    TestExportInventory exportSQL  = null;
                                    DB_Class_SQLite.DBClassSQLite dbSQlite = null;
                                    string connectionString = null;
                                    string tableName = null;
                                    string connectionstring = string.Empty;                                   
                                    int bEnable = 0;
                                    try
                                    {
                                        dbSQlite = new DB_Class_SQLite.DBClassSQLite();
                                        dbSQlite.OpenDB();
                                        if (dbSQlite.getExportInfo(1, out connectionstring, out tableName, out bEnable))
                                            exportSQL = new TestExportInventory(DBClass_SQLServer.UtilSqlServer.ConvertConnectionString(connectionstring), tableName);
                                        if ((exportSQL.OpenDB()) && (exportSQL.isTableExist()))
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.TestSQL_OK, false);
                                        }
                                        else
                                        {
                                            SendReturnCode(mySocket, myClient, tableName + " not exists in database" , false);
                                        }
                                    }
                                    catch (Exception exptest)
                                    {

                                        SendReturnCode(mySocket, myClient, exptest.Message, false);
                                    }
                                    finally
                                    {
                                        if ((dbSQlite != null) && (dbSQlite.isOpen()))
                                        {
                                            dbSQlite.CloseDB();
                                            dbSQlite = null;
                                        }
                                    }
                                    break;
                                }
                            #endregion
                            #region GET_TCP_NOTIFICATION
                                case "GET_TCP_NOTIFICATION?":
                                {
                                    DB_Class_SQLite.DBClassSQLite dbSQlite = null;
                                    string hostIp = string.Empty;
                                    string hospPort = string.Empty;                                   
                                    int bActive = 0;
                                    try
                                    {
                                        dbSQlite = new DB_Class_SQLite.DBClassSQLite();
                                        dbSQlite.OpenDB();
                                        dbSQlite.getExportInfo(2, out hostIp, out hospPort, out bActive);
                                        if (string.IsNullOrEmpty(hostIp))
                                        {
                                            SendReturnCode(mySocket, myClient, ReturnType.noData, false);
                                        }
                                        else
                                        {
                                            
                                            string ret = bActive.ToString();
                                            ret += ";" + hostIp;
                                            ret += ";" + hospPort;                                           
                                            SendReturnCode(mySocket, myClient, ret, false);

                                        }
                                    }
                                    catch
                                    {
                                        SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                    }
                                    finally
                                    {
                                        if ((dbSQlite != null) && (dbSQlite.isOpen()))
                                        {
                                            dbSQlite.CloseDB();
                                            dbSQlite = null;
                                        }
                                    }
                                    break;
                                }
                                #endregion
                            #region SET_TCP_NOTIFICATION
                                case "SET_TCP_NOTIFICATION?":
                                {
                                    DB_Class_SQLite.DBClassSQLite dbSQlite = null;
                                    try
                                    {
                                        bool bDataOk = true;
                                        for (int loop = 0; loop < command.Length; loop++)
                                        {
                                            if (string.IsNullOrEmpty(command[loop]) && loop != 2)
                                            {
                                                bDataOk = false;
                                                SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                                break;
                                            }
                                        }

                                        if (bDataOk)
                                        {
                                            if (command.Length != 4)
                                                SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                            else
                                            {
                                                int bActive = Convert.ToInt32(command[1]);
                                                string tcpServerIp = String.IsNullOrEmpty(command[2]) ? ((IPEndPoint) myClient.Socket.RemoteEndPoint).Address.ToString() : command[2];
                                                string tcpServerPort = command[3];

                                                dbSQlite = new DB_Class_SQLite.DBClassSQLite();
                                                dbSQlite.OpenDB();
                                                if (dbSQlite.AddExportInfo(2, tcpServerIp, tcpServerPort, bActive))
                                                {
                                                    needrefreshSQL = true;
                                                    SendReturnCode(mySocket, myClient, ReturnType.SetSQL_OK, false);
                                                }
                                                else
                                                    SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);

                                            }
                                        }
                                    }
                                    catch
                                    {


                                    }
                                    finally
                                    {
                                        if ((dbSQlite != null) && (dbSQlite.isOpen()))
                                        {
                                            dbSQlite.CloseDB();
                                            dbSQlite = null;
                                        }
                                    }

                                    break;
                                }
                                #endregion
                            #region SET_TCP_NOTIFICATION_ONOFF
                                case "SET_TCP_NOTIFICATION_ONOFF?":
                                {
                                    DB_Class_SQLite.DBClassSQLite dbSQlite = null;
                                    try
                                    {
                                        int bActive = Convert.ToInt32(command[1]);


                                        dbSQlite = new DB_Class_SQLite.DBClassSQLite();
                                        dbSQlite.OpenDB();

                                        if (bActive == 1)
                                            dbSQlite.setSqlExportOnOff(2,true);
                                        else
                                            dbSQlite.setSqlExportOnOff(2,false);
                                        needrefreshSQL = true;
                                        SendReturnCode(mySocket, myClient, ReturnType.TestSQL_OK, false);
                                    }
                                    catch
                                    {

                                        SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                    }
                                    finally
                                    {
                                        if ((dbSQlite != null) && (dbSQlite.isOpen()))
                                        {
                                            dbSQlite.CloseDB();
                                            dbSQlite = null;
                                        }
                                    }
                                    break;
                                }
                                #endregion
                            #region TEST_TCP_NOTIFICATION
                                case "TEST_TCP_NOTIFICATION?":
                                {
                                    DB_Class_SQLite.DBClassSQLite dbSQlite = null;
                                    string tcpServerIp = null;
                                    string tcpServerPort = null; 
                                    int bEnable = 0;
                                    try
                                    {
                                        dbSQlite = new DB_Class_SQLite.DBClassSQLite();
                                        dbSQlite.OpenDB();
                                        if (dbSQlite.getExportInfo(2, out  tcpServerIp, out tcpServerPort, out bEnable))
                                        {
                                            if (tcpUtils.PingAddress(tcpServerIp, 2000))
                                            {
                                                if (bEnable == 1)
                                                {
                                                    int port = 0;
                                                    int.TryParse(tcpServerPort, out port);
                                                    TcpClient testTcp = new TcpClient();
                                                    testTcp.Connect(tcpServerIp, port);
                                                    if (testTcp.Connected)
                                                    {
                                                        string TestInfo = "CR_DISPATCH CC_SB_TEST_TCP " + ServerIP + " " + Port.ToString() + " " + localDeviceArray[0].infoDev.SerialRFID;
                                                        Stream stm = testTcp.GetStream();
                                                        ASCIIEncoding asen = new ASCIIEncoding();
                                                        byte[] data = asen.GetBytes(TestInfo);
                                                        stm.Write(data, 0, data.Length);
                                                        testTcp.Close();
                                                        SendReturnCode(mySocket, myClient, ReturnType.TestSQL_OK, false);
                                                    }
                                                    else
                                                    {
                                                        SendReturnCode(mySocket, myClient,
                                                            "Unable to connect to " + tcpServerIp + ":" + tcpServerPort,
                                                            false);
                                                    }
                                                }
                                                else
                                                {
                                                    SendReturnCode(mySocket, myClient, "Tcp Notification Disable", false);
                                                }
                                            }
                                            else
                                            {
                                                SendReturnCode(mySocket, myClient, "Ping Not Ok", false);
                                            }
                                        }
                                        else
                                        {
                                              SendReturnCode(mySocket, myClient, ReturnType.errorDB, false);
                                        }
                                    }
                                    catch (Exception exptest)
                                    {

                                        SendReturnCode(mySocket, myClient, exptest.Message, false);
                                    }
                                    finally
                                    {
                                        if ((dbSQlite != null) && (dbSQlite.isOpen()))
                                        {
                                            dbSQlite.CloseDB();
                                            dbSQlite = null;
                                        }
                                    }
                                    break;
                                }
                                #endregion
                            #region  GET_DEVICE_INFO
                                case "GET_DEVICE_INFO?":
                                {
                                    string info = string.Empty;
                                    if (nbArg == 1) // unique reader assume in index  0 of local array
                                    {
                                        if (localDeviceArray[0].rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected)
                                            info = "TRUE;";
                                        else
                                            info = "FALSE;";

                                        if (localDeviceArray[0].bDataCompleted)
                                            info += localDeviceArray[0].rfidDev.DeviceStatus.ToString() + ";";
                                        else
                                            info += DeviceStatus.DS_InScan.ToString() + ";";

                                        DateTime utcDate = localDeviceArray[0].lastProcessInventoryGmtDate;
                                        info += utcDate.ToString("u");
                                        SendReturnCode(mySocket, myClient, info, false);
                                    }
                                    else // several reader ; search it
                                    {

                                        string serialRFID = command[1];
                                        bool bFind = false;
                                        foreach (deviceClass dc in localDeviceArray)
                                        {
                                            if (dc.infoDev.SerialRFID.Equals(serialRFID))
                                            {
                                                bFind = true;
                                                if (dc.rfidDev.ConnectionStatus == ConnectionStatus.CS_Connected)
                                                    info = "TRUE;";
                                                else
                                                    info = "FALSE;";

                                                if (dc.bDataCompleted)
                                                    info += dc.rfidDev.DeviceStatus.ToString()+ ";";
                                                else
                                                    info += DeviceStatus.DS_InScan.ToString() + ";";

                                                DateTime utcDate = dc.lastProcessInventoryGmtDate;
                                                info +=  utcDate.ToString("u");
                                                SendReturnCode(mySocket, myClient, info, false);
                                                break;
                                            }
                                        }
                                        if (!bFind) SendReturnCode(mySocket, myClient, ReturnType.readerNotExist, false);
                                    }
                                    break;
                                   
                                }
                            #endregion
                            #region SET_RESERVED_DATA?
                                case "SET_RESERVED_DATA?":
                                {
                                    if (nbArg == 3)
                                    {
                                        spareData1 = command[1];
                                        spareData2 = command[2];
                                        SendReturnCode(mySocket, myClient, "SET_OK", false);
                                    }
                                    else if (nbArg == 4)
                                    {
                                        spareData1 = command[1];
                                        spareData2 = command[2];
                                        string badgeID = command[3];
                                        if (spareDataCol.ContainsKey(badgeID))
                                            spareDataCol[badgeID] = command[1] + ";" + command[2];
                                        else
                                            spareDataCol.Add(badgeID, command[1] + ";" + command[2]);
                                        SendReturnCode(mySocket, myClient, "SET_OK", false);
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.Data_Error, false);
                                }
                                break;
                                #endregion
                            #region GetLastBadge
                                case "GET_LAST_BADGE?":
                                {                                   
                                        if (string.IsNullOrEmpty(LastBadgeRead))
                                            SendReturnCode(mySocket, myClient, "No Badge", false);
                                        else
                                            SendReturnCode(mySocket, myClient, LastBadgeRead, false);
                                }
                                break;
                            #endregion
                            #region IS_SPCE2_AVAILABLE
                                case "IS_SPCE2_AVAILABLE?":
                                {
                                    RFID_Device currentDevice = localDeviceArray[0].rfidDev;
                                    if ((currentDevice.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                         (currentDevice.DeviceStatus == DeviceStatus.DS_Ready))
                                    {
                                        if(currentDevice.IsSpec2Available())
                                            SendReturnCode(mySocket, myClient, ReturnType.SPCE2Available, false);
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.SPCE2Notavailable, false);
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                }
                                break;
                            #endregion
                            #region GET_FIRMWARE_VERSION?
                                case "GET_FIRMWARE_VERSION?":
                                {
                                    RFID_Device currentDevice = localDeviceArray[0].rfidDev;
                                    SendReturnCode(mySocket, myClient, currentDevice.GetfirwareVersion().ToString(), false);
                                }
                                break;
                                #endregion
                            #region START_LIGHTING_LED
                                case "START_LIGHTING_LED?":
                                {
                                    RFID_Device currentDevice = localDeviceArray[0].rfidDev;

                                    if ((currentDevice.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                         (currentDevice.DeviceStatus == DeviceStatus.DS_Ready))
                                    {
                                        if (currentDevice.IsSpec2Available())
                                        {
                                            if ((currentDevice.ListTagWithChannel == null) || (currentDevice.ListTagWithChannel.Count == 0))
                                            {
                                                SendReturnCode(mySocket, myClient, ReturnType.noData, false);
                                            }
                                            else if (command.Length > 1)
                                                // command + tag ID(s) to light : at least 2 args
                                            {
                                                List<String> tagsToLight = new List<String>();

                                                for (int i = 1; i < command.Length; ++i)
                                                    tagsToLight.Add(command[i]);

                                                currentDevice.TestLighting(tagsToLight);

                                                StringBuilder spce2Response =
                                                    new StringBuilder(ReturnType.SPCE2StartLighting);
                                                foreach (string tagId in tagsToLight)
                                                    spce2Response.Append(tcpUtils.TCPDelimiter).Append(tagId);
                                                        // tag ID(s) left in list are missing, return them to Client.

                                                SendReturnCode(mySocket, myClient, spce2Response.ToString(), false);
                                            }

                                            else
                                                SendReturnCode(mySocket, myClient, ReturnType.SPCE2NoTags, false);
                                        }
                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.SPCE2Notavailable, false);
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                }    
                                break;
                            #endregion
                            #region STOP_LIGHTING_LED
                                case "STOP_LIGHTING_LED?":
                                {
                                    RFID_Device currentDevice = localDeviceArray[0].rfidDev;

                                    if ((currentDevice.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                         (currentDevice.DeviceStatus == DeviceStatus.DS_LedOn))
                                    {
                                        SendReturnCode(mySocket, myClient, ReturnType.SPCE2StopLighting, false);
                                        currentDevice.StopLightingLeds();                                    
                                     
                                      
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);

                                    if (currentDevice.DeviceStatus != DeviceStatus.DS_InScan)
                                        currentDevice.SetLight(150);
                                }    
                                break;
                            #endregion
                            #region WRITE_BLOCK
                                case "WRITE_BLOCK?":
                                {
                                    RFID_Device currentDevice = localDeviceArray[0].rfidDev;

                                    if ((currentDevice.ConnectionStatus == ConnectionStatus.CS_Connected) &&
                                         (currentDevice.DeviceStatus == DeviceStatus.DS_Ready))
                                    {
                                        if (command.Length > 3) // command + old tagID + new tagID + Mode
                                        {
                                            string oldTagId = command[1].Trim();
                                            string newTagId = command[2].Trim();
                                            string writeModeType = command[3].Trim();

                                            WriteCode codeResult = WriteCode.WC_Error; 

                                            if (writeModeType == "0") // classic mode
                                                codeResult = currentDevice.WriteNewUID(oldTagId, newTagId);
                                            else if (writeModeType == "1") //SPCE2 With family
                                                codeResult = currentDevice.WriteNewUidWithFamily(oldTagId, newTagId);
                                            else if (writeModeType == "2")  // SPCE2 Decimal
                                                codeResult = currentDevice.WriteNewUidDecimal(oldTagId, newTagId);
                                                            
                                            SendReturnCode(mySocket, myClient, ((int)codeResult).ToString(), false);
                                        }
                                        else if (command.Length > 2) // command + old tagID + new tagID 
                                        {
                                            string oldTagId = command[1].Trim();
                                            string newTagId = command[2].Trim();

                                            WriteCode codeResult = currentDevice.WriteNewUID(oldTagId, newTagId);
                                            SendReturnCode(mySocket, myClient, ((int)codeResult).ToString(), false);
                                        }

                                        else
                                            SendReturnCode(mySocket, myClient, ReturnType.WriteIDNotEnoughArgs, false);
                                    }
                                    else
                                        SendReturnCode(mySocket, myClient, ReturnType.readerNotReady, false);
                                }
                                break;
                            #endregion
                            #region IS_USING_TCP_NOTIFICATION
                            case "IS_USING_TCP_NOTIFICATION?":
                            {
                                // Return something to assure st201 Version , if not ther client will receive unknown command
                                SendReturnCode(mySocket, myClient, ReturnType.readerOk, false);
                                break;
                            }
                            #endregion
                            default: 
                                //SendReturnCode(mySocket, myClient, ReturnType.unknownCmd, false);
                                TextBoxRefresh("Unknown Command Received :" + myClient.Command, false, ">");
                            break;
                        }
                    }
                
                    }
                catch (System.NullReferenceException nullExp)
                {
                    if (strReceive != null)
                        ErrorMessage.ExceptionMessageBox.Show(nullExp, "Error TCP Server in cmd " + myClient.Command);
                    else
                        ErrorMessage.ExceptionMessageBox.Show(nullExp, "Error TCP Server in Read Data");

                }
                catch (Exception exp)
                {
                    ErrorMessage.ExceptionMessageBox.Show(exp, "Error TCP Server in cmd " + myClient.Command);
                   
                }
                finally
                {
                    if (db != null)
                    {
                        db.CloseDB();
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
            
            
        }

       
        private string ReadData(Socket mySocket)
        {
            int size = -1;
            int nbToRead = -1;
            int dataRead = 0;
            string data = string.Empty;
            try
            {
                Byte[] buffer = new Byte[4];
                size = mySocket.Receive(buffer);
               
                if (buffer[0] == 0x7C) // direct command
                {
                    data = System.Text.Encoding.ASCII.GetString(buffer, 0, size);
                    return tcpUtils.DecodeString(data);
                }
                else //modeclassique
                {

                    nbToRead = BitConverter.ToInt32(buffer, 0);
                    TextBoxRefresh("nb to read : " + nbToRead, false, "<");
                    if ((nbToRead > 100000))
                        return null;
                    
                    do
                    {
                        Byte[] buf = new Byte[nbToRead];
                        int ret = mySocket.Receive(buf);
                        dataRead += ret;
                        data += System.Text.Encoding.ASCII.GetString(buf, 0, ret);

                    } while (dataRead < nbToRead);

                    if (data.Length < 100)
                        TextBoxRefresh(data + "( " + data.Length.ToString() + " )", false, "<");
                    else
                        TextBoxRefresh(data.Substring(0, 100) + "( " + data.Length.ToString() + " )", false, "<");


                    return tcpUtils.DecodeString(data);
                }
            }
            catch
            {
                //string error = string.Format("Error in server in read Data\r\nSize : {0} \r\n nbToRead : {1}\r\ndataRead :{2}\r\nData : {3}", size, nbToRead, dataRead, data);
               // ErrorMessage.ExceptionMessageBox.Show("Error in Read Data in server", error, "TCP Server Error in ReadData sunction");
                return null;
            }

        }

        private void SendReturnCode(Socket mySocket, Client myClient, string str, bool disconnect , bool sendSize = true)
        {
            try
            {
                for (int i = 0; i < ListeClients.Count; i++)
                {
                    Client c = (Client) ListeClients[i];
                    if (c.Socket == mySocket)
                    {
                        int nbTrame = 0;
                        byte[] data = System.Text.Encoding.ASCII.GetBytes(tcpUtils.EncodeString(str));
                        int total = 0;
                        int size = data.Length;
                        TextBoxRefresh(size + " bytes to send ", false, ">");
                        int dataleft = size;
                        int sent;

                        if (sendSize)
                        {
                            byte[] userDataLen = BitConverter.GetBytes((Int32) data.Length);
                            c.Socket.Send(userDataLen, 0, 4, System.Net.Sockets.SocketFlags.None);
                        }


                        while (total < size)
                        {
                            sent = c.Socket.Send(data, total, dataleft, System.Net.Sockets.SocketFlags.None);
                            total += sent;
                            dataleft -= sent;
                            nbTrame++;
                        }
                        //TextBoxRefresh(nbTrame.ToString() + " frame(s) to " + myClient.ClientName, false, ">");

                        if (str.Length < 100)
                            TextBoxRefresh("sent : " + str + " (" + total.ToString() + " bytes)", false, ">");
                        else
                            TextBoxRefresh("sent : " + str.Substring(0, 100) + "... (" + total.ToString() + " bytes)",
                                false, ">");

                    }
                    if (disconnect)
                    {
                        c.Socket.Close();
                        ListeClients.RemoveAt(i);
                        TextBoxRefresh("Client Disconnected ", false, ">");
                    }
                }
            }
            catch (ObjectDisposedException ode)
            {

            }
            catch 
            {
                
            }
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
                for (int n = 0; n < ListeClients.Count; n++)
                {
                    Client c = (Client)ListeClients[n];
                    c.Socket.Close();
                    c.Thread.Abort();
                    c.Thread.Join(500);
                }
                ListeClients.Clear();
            }
            if (myListener != null)
            {
               myListener.Stop();
               myListener = null;
            }


            if (socketServeur != null)
            {
                socketServeur.Close();
                socketServeur = null;
            }

            if (nsClient != null)
            {
                nsClient.Close();
                nsClient = null;
            }
            if (vigile != null)
            {
                vigile.Abort();
                vigile.Join(1000);
                vigile = null;
            }
            if (threadClient != null)
            {
                threadClient.Abort();
                threadClient.Join(1000);
                threadClient = null;
            }
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

            if (strLog.Length > 8192)
                strLog = strLog.Substring(0, 8192);

            if (txtInfoServer == null) return;
            if (txtInfoServer.InvokeRequired)
            {
                txtInfoServer.Invoke((MethodInvoker)delegate
                {
                    txtInfoServer.Text = null;
                    txtInfoServer.Text = strLog;
                    txtInfoServer.Refresh();
                });
                /*System.Threading.Thread.Sleep(50);
                Application.DoEvents();
                System.Threading.Thread.Sleep(50);*/
            }
           
        }
    }

    
}
