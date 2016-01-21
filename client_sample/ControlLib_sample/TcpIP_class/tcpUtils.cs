using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Data;
using DataClass;

using System.Collections.Specialized;

namespace TcpIP_class
{
    static public class tcpUtils
    {
        public static char TCPDelimiter = (char) 0x1C;

        static public string DecodeString(string str)
        {
            str = str.Replace("%01", "å");
            str = str.Replace("%02", "Å");
            str = str.Replace("%03", "ä");
            str = str.Replace("%04", "Ä");
            str = str.Replace("%05", "ö");
            str = str.Replace("%06", "Ö");
            str = str.Replace("%07", "ü");
            str = str.Replace("%08", "Ü");
            str = str.Replace("%09", "é");
            str = str.Replace("%10", "É");
            str = str.Replace("%11", "§");
            return str;
        }
        static public string EncodeString(string str)
        {
            str = str.Replace("å", "%01");
            str = str.Replace("Å", "%02");
            str = str.Replace("ä", "%03");
            str = str.Replace("Ä", "%04");
            str = str.Replace("ö", "%05");
            str = str.Replace("Ö", "%06");
            str = str.Replace("ü", "%07");
            str = str.Replace("Ü", "%08");
            str = str.Replace("é", "%09");
            str = str.Replace("§", "%11");
      
            return str;
        }

        public static bool CheckIp(string ipAddress)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(ipAddress,
            @"^(25[0-5]|2[0-4]\d|[0-1]?\d?\d)(\.(25[0-5]|2[0-4]\d|[0-1]?\d?\d)){3}$");
        }
        static public IPAddress getLocalIp()
        {
            IPHostEntry _IPHostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (IPAddress _IPAddress in _IPHostEntry.AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                    return _IPAddress;
            }       
            return null;
        }
        public static string GetPhysicalIPAdress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null && !addr.Address.ToString().Equals("0.0.0.0"))
                {
                    //break to give in first ethernet
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null && !addr.Address.ToString().Equals("0.0.0.0"))
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            return String.Empty;
        }

        static public ArrayList getAllIpOnNetwork(IPAddress _IPaddress)
        {
            ArrayList ipList = new ArrayList();

            byte[] ip = _IPaddress.GetAddressBytes();

            for (int loop = 0; loop < 255 ; loop++)
            {
                ip[3] = (byte)loop;
                IPAddress tmpIP = new IPAddress(ip);
                ipList.Add(tmpIP);
            }
            return ipList;
        }

        private static long _numberOfThreadsNotYetCompleted = 255;
        private static ManualResetEvent _doneEvent = new ManualResetEvent(false);
        static public ArrayList findOnlineMachine(int port)
        {
            ArrayList listOnline = new ArrayList();
            //ManualResetEvent[] doneEvents = new ManualResetEvent[255];
            searchTCP[] tcp = new searchTCP[255];

            ArrayList listIp = new ArrayList();
            listIp = getAllIpOnNetwork(getLocalIp());

            for (int i = 0; i < 255; i++)
            {
                serverInfo si = new serverInfo();
                si.IP = listIp[i].ToString();
                si.port = port;
                si.threadnumber = i;              
                //doneEvents[i] = new ManualResetEvent(false);
                //searchTCP tmpsearch = new searchTCP(doneEvents[i]);
                searchTCP tmpsearch = new searchTCP();
                tcp[i] = tmpsearch;
                ThreadPool.QueueUserWorkItem(new WaitCallback(tmpsearch.ThreadPoolCallback), (object)si);
            }

           // WaitHandle.WaitAll(doneEvents);
            _doneEvent.WaitOne();
            foreach (searchTCP tc in tcp)
                if (tc.Ret == 1) listOnline.Add(tc.IP);
            return listOnline;

        }
        public static bool PingAddress(string ip,int timeout)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            PingReply reply = pingSender.Send(ip, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                return true;
            }
            return false;

        }

        public class serverInfo
        {
            public string IP;
            public int port;
            public int threadnumber;        
        }

        public class searchTCP
        {    
            public void ThreadPoolCallback(Object threadContext)
            {
                try
                {                    
                    serverInfo server = (serverInfo)threadContext;                
                    
                    _ip = server.IP;
                   if (PingAddress(_ip,500))
                    {
                        try
                        {
                            IPHostEntry IpEntry = Dns.GetHostEntry(_ip);
                            string machineName = IpEntry.HostName;
                            if ((machineName.ToUpper().StartsWith("VN120")) ||
                                (machineName.ToUpper().StartsWith("VK468")) || 
                                (machineName.ToUpper().StartsWith("VK520")) || 
                                (machineName.ToUpper().StartsWith("V400")) ||
                                (machineName.ToUpper().StartsWith("V401")) ||
                                (machineName.ToUpper().StartsWith("V402")) ||
                                (machineName.ToUpper().StartsWith("V380")) || 
                                (machineName.ToUpper().StartsWith("V340")) || 
                                (machineName.ToUpper().StartsWith("V300")) || 
                                (machineName.ToUpper().StartsWith("SMC")) ||
                                (machineName.ToUpper().StartsWith("SBR")) ||
                                (machineName.ToUpper().StartsWith("DSB")) ||
                                (machineName.ToUpper().StartsWith("JSC")) ||
                                (machineName.ToUpper().StartsWith("MSR")) ||
                                (machineName.ToUpper().StartsWith("SFR")) ||
                                (machineName.ToUpper().StartsWith("SAS")))
                            {
                                TcpIpClient tcp = new TcpIpClient();
                                _ret = (int)tcp.pingServer(server.IP, server.port);
                            }
                            else
                                _ret = 0;
                        }
                        catch
                        {
                            _ret = 0;
                        }
                    }
                    else
                        _ret = 0;                  
                    
                }
                finally
                {
                    if (Interlocked.Decrement(ref _numberOfThreadsNotYetCompleted) == 0)
                    {   
                        if (_doneEvent != null)
                        _doneEvent.Set();
                    }
                }
            }
            public int Ret { get { return _ret; } }
            private int _ret;

            private string _ip;
            public string IP { get { return _ip; } }
        }

        public static string ByteArrayToHexString(byte[] Bytes)
        {
            StringBuilder Result = new StringBuilder();
            string HexAlphabet = "0123456789ABCDEF";

            foreach (byte B in Bytes)
                {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0x0F)]);
                }

            return Result.ToString();
        }

        public static byte[] HexStringToByteArray(string Hex)
        {
            byte[] Bytes = new byte[Hex.Length / 2];
            int[] HexValue = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 
                                         0x06, 0x07, 0x08, 0x09, 0x00, 0x00, 
                                         0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 
                                         0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

            for (int x = 0, i = 0; i < Hex.Length; i += 2, x += 1)
            {
                Bytes[x] = (byte)(HexValue[Char.ToUpper(Hex[i + 0]) - '0'] << 4 |
                                  HexValue[Char.ToUpper(Hex[i + 1]) - '0']);
            }

            return Bytes;
        }

        public static bool GetNetworkTime(out DateTime worldGmtTime)
        {
            worldGmtTime = DateTime.Now;

            try
            {
                // Test Internet connection
                System.Net.IPHostEntry i = System.Net.Dns.GetHostEntry("www.google.com");

                const string ntpServer = "pool.ntp.org";
                var ntpData = new byte[48];
                ntpData[0] = 0x1B; //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

                var addresses = Dns.GetHostEntry(ntpServer).AddressList;
                var ipEndPoint = new IPEndPoint(addresses[0], 123);
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                socket.ReceiveTimeout = 1000;
                socket.SendTimeout = 1000;
                socket.Connect(ipEndPoint);
                socket.Send(ntpData);
               
                socket.Receive(ntpData);
                socket.Close();

                ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
                ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);
                worldGmtTime = networkDateTime;
                return true;
            }
            catch
            {
                return false;
            }
        }


       


        public struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }
        [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        public extern static bool Win32SetSystemTime(ref SYSTEMTIME sysTime);


        public static void setSystemTime(DateTime dateToSet)
        {
            try
            {
                SYSTEMTIME updatedTime = new SYSTEMTIME();
                updatedTime.wYear = (ushort) dateToSet.Year;
                updatedTime.wMonth = (ushort) dateToSet.Month;
                updatedTime.wDay = (ushort) dateToSet.Day;
                // UTC time; it will be modified according to the regional settings of the target computer so the actual hour might differ
                updatedTime.wHour = (ushort) dateToSet.Hour;
                updatedTime.wMinute = (ushort) dateToSet.Minute;
                updatedTime.wSecond = (ushort) dateToSet.Second;
                // Call the unmanaged function that sets the new date and time instantly
                // Work on windows XP or since vista with admin right
                Win32SetSystemTime(ref updatedTime);
            }
            catch (Exception exp)
            {
                string msg = exp.Message;
            }


        }
    }
    
    static public class NativeMethods
    {
        /// 
        /// Reboot the computer
        /// 

        public static void Reboot()
        {
            IntPtr tokenHandle = IntPtr.Zero;

            try
            {
                // get process token
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle,
                    TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES,
                    out tokenHandle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to open process token handle");
                }

                // lookup the shutdown privilege
                TOKEN_PRIVILEGES tokenPrivs = new TOKEN_PRIVILEGES();
                tokenPrivs.PrivilegeCount = 1;
                tokenPrivs.Privileges = new LUID_AND_ATTRIBUTES[1];
                tokenPrivs.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

                if (!LookupPrivilegeValue(null,
                    SE_SHUTDOWN_NAME,
                    out tokenPrivs.Privileges[0].Luid))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to open lookup shutdown privilege");
                }

                // add the shutdown privilege to the process token
                if (!AdjustTokenPrivileges(tokenHandle,
                    false,
                    ref tokenPrivs,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to adjust process token privileges");
                }

                // reboot
                if (!ExitWindowsEx(ExitWindows.Reboot,
                        ShutdownReason.MajorApplication |
                ShutdownReason.MinorInstallation |
                ShutdownReason.FlagPlanned))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to reboot system");
                }
            }
            finally
            {
                // close the process token
                if (tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(tokenHandle);
                }
            }
        }

        // everything from here on is from pinvoke.net

        [Flags]
        private enum ExitWindows : uint
        {
            // ONE of the following five:
            LogOff = 0x00,
            ShutDown = 0x01,
            Reboot = 0x02,
            PowerOff = 0x08,
            RestartApps = 0x40,
            // plus AT MOST ONE of the following two:
            Force = 0x04,
            ForceIfHung = 0x10,
        }

        [Flags]
        private enum ShutdownReason : uint
        {
            MajorApplication = 0x00040000,
            MajorHardware = 0x00010000,
            MajorLegacyApi = 0x00070000,
            MajorOperatingSystem = 0x00020000,
            MajorOther = 0x00000000,
            MajorPower = 0x00060000,
            MajorSoftware = 0x00030000,
            MajorSystem = 0x00050000,

            MinorBlueScreen = 0x0000000F,
            MinorCordUnplugged = 0x0000000b,
            MinorDisk = 0x00000007,
            MinorEnvironment = 0x0000000c,
            MinorHardwareDriver = 0x0000000d,
            MinorHotfix = 0x00000011,
            MinorHung = 0x00000005,
            MinorInstallation = 0x00000002,
            MinorMaintenance = 0x00000001,
            MinorMMC = 0x00000019,
            MinorNetworkConnectivity = 0x00000014,
            MinorNetworkCard = 0x00000009,
            MinorOther = 0x00000000,
            MinorOtherDriver = 0x0000000e,
            MinorPowerSupply = 0x0000000a,
            MinorProcessor = 0x00000008,
            MinorReconfig = 0x00000004,
            MinorSecurity = 0x00000013,
            MinorSecurityFix = 0x00000012,
            MinorSecurityFixUninstall = 0x00000018,
            MinorServicePack = 0x00000010,
            MinorServicePackUninstall = 0x00000016,
            MinorTermSrv = 0x00000020,
            MinorUnstable = 0x00000006,
            MinorUpgrade = 0x00000003,
            MinorWMI = 0x00000015,

            FlagUserDefined = 0x40000000,
            FlagPlanned = 0x80000000
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public UInt32 Attributes;
        }

        private struct TOKEN_PRIVILEGES
        {
            public UInt32 PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }

        private const UInt32 TOKEN_QUERY = 0x0008;
        private const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ExitWindowsEx(ExitWindows uFlags,
            ShutdownReason dwReason);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle,
            UInt32 DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeValue(string lpSystemName,
            string lpName,
            out LUID lpLuid);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
            [MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            UInt32 Zero,
            IntPtr Null1,
            IntPtr Null2);
    }

    struct ReturnType
    {
        public const string unknownCmd = "UNKNOWN_CMD";
        public const string noData = "NO_DATA";
        public const string restartOk = "RESTART_OK";
        public const string rebootOk = "REBOOT_OK";
        public const string pingServerOk = "PING_OK";
        public const string readerOk = "READER_OK";
        public const string setLightOk = "LIGHT_OK";
        public const string readerNotReady = "READER_NOT_READY";
        public const string readerNotExist = "READER_NOT_EXIST";
        public const string failedToStartScan = "FAILED_START_SCAN";
        public const string scanStarted = "SCAN_STARTED";
        public const string errorDuringScan = "ERROR_DURING_SCAN";
        public const string failedWaitMode = "FAILED_START_WAIT_MODE";
        public const string waitModeStarted = "WAIT_MODE_STARTED";
        public const string wrongReader = "BAD_READER_TYPE";
        public const string failedWaitModeOff = "FAILED_STOP_WAIT_MODE";
        public const string waitModeStopped = "WAIT_MODE_STOPPED";
        public const string readerNotInWait = "READER_NOT_IN_WAIT_MODE";
        public const string refreshUser = "REFRESH_OK";
        public const string errorDB = "ERROR_OPEN_DB";
        public const string setRemoteOk = "SET_REMOTE_ACCESS_OK";
        public const string setRemoteError = "SET_REMOTE_ACCESS_ERROR";
        public const string unsetRemoteOk = "DELETE_REMOTE_ACCESS_OK";
        public const string unsetRemoteError = "DELETE_REMOTE_ACCESS_ERROR";
        public const string addUserFinger = "ADD_FINGER_OK";
        public const string addUserTemplate = "ADD_TEMPLATE_OK";
        public const string addUserGrant = "ADD_GRANT_OK";
        public const string unknownUser = "UNKNOWN_USER";
        public const string delUserFinger = "DELETE_FINGER_OK";
        public const string delUserTemplate = "DEL_TEMPLATE_OK";
        public const string delUserGrant = "DEL_GRANT_OK";
        public const string delUser = "DELETE_USER_OK";
        public const string addUserBadge = "ADD_BADGE_OK";
        public const string delUserBadge = "DEL_BADGE_OK";
        public const string timeZoneok = "SET_TIME_ZONE_OK";
        public const string timeZoneBad = "ERROR_TIME_ZONE ARG";
        public const string stopAccumulate = "STOP_ACC_OK";
        public const string errorPath = "ERROR_PATH";
        public const string fileNotexist = "FILE_NOT_EXIST";
        public const string flashStarted = "FLASH_STARTED";
        public const string ReaderNotInReadyState = "DEVICE_NOT_IN_READY_STATE";
        public const string Data_Error = "DATA_ERROR";
        public const string SetIP_OK = "SET_IP_OK";
        public const string SetSQL_OK = "SET_SQL_EXPORT_OK";
        public const string GetSQL_OK = "GET_SQL_EXPORT_OK";
        public const string TestSQL_OK = "TEST_SQL_EXPORT_OK";
        public const string SPCE2Available = "SPCE2_AVAILABLE";
        public const string SPCE2Notavailable = "SPCE2_NOT_AVAILABLE";
        public const string SPCE2NoTags = "SPCE2_NO_TAGS"; // lighting command sent but no tags were specified
        public const string SPCE2StartLighting = "SPCE2_START_LIGHTING"; // device started lighting LEDs
        public const string SPCE2StopLighting = "SPCE2_STOP_LIGHTING"; // device stopped lighting LEDs
        public const string WriteIDNotEnoughArgs = "WRITE_ID_NOT_ENOUGH_ARGS"; // device stopped lighting LEDs
    }

    public class TestExportInventory
    {
        public SqlConnection globalCon;
        public string globalConnectionString;
        public ConnectionState conState;
        public string tableName;
        public TestExportInventory(string globalConnectionString, string tableName)
        {
            this.globalConnectionString = globalConnectionString;
            this.tableName = tableName;
            conState = ConnectionState.Closed;
        }

       

        public bool isTableExist()
        {
            bool ret = false;
            SqlCommand cmd = new SqlCommand();
            SqlDataReader rd = null;
            try
            {
                if (globalCon == null) return false;
                if (globalCon.State != System.Data.ConnectionState.Open) OpenDB();

                string sqlQuery = null;
                sqlQuery += "SELECT * FROM sysobjects ";
                sqlQuery += "WHERE name='" + tableName + "' AND Xtype='U'";

                cmd.Connection = globalCon;
                cmd.CommandText = sqlQuery;
                cmd.CommandType = System.Data.CommandType.Text;

                rd = cmd.ExecuteReader();
                ret = rd.HasRows;

            }
            catch (Exception exp)
            {
                // On affiche l'erreur.
                ErrorMessage.ExceptionMessageBox.Show(exp);
            }
            finally
            {
                if (rd != null) rd.Close();
                if (cmd != null) cmd.Dispose();
            }
            return ret;
        }

        public void CloseDB()
        {
            if (globalCon == null) return;
            if (globalCon.State != ConnectionState.Closed)
            {
                globalCon.Close();
                conState = globalCon.State;
                globalCon = null;
            }
        }
        public bool OpenDB()
        {
            globalCon = new SqlConnection();
            globalCon.ConnectionString = globalConnectionString;
            globalCon.Open();
            conState = globalCon.State;
            if (globalCon.State == ConnectionState.Open) return true;
            else return false;
        }
        public bool isOpen()
        {
            if (globalCon == null) return false;
            if (globalCon.State == System.Data.ConnectionState.Open) return true;
            else return false;
        }
    }

    static public class phpExport
    {
        static public bool getLastScanId(string serialRfid, out int lastScanId)
        {
            lastScanId = -1;
            string URL = @"http://localhost/MedStock2/web/app_dev.php/GetLastScanId?XDEBUG_SESSION_START=netbeans-xdebug";
            WebClient webClient = new WebClient();

            NameValueCollection formData = new NameValueCollection();
            formData["serialNumberRfid"] = serialRfid;
            try
            {
                webClient.Headers[HttpRequestHeader.UserAgent] =
                    "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.13) Gecko/20101203 Firefox/3.6.13";
                byte[] responseBytes = webClient.UploadValues(URL, "POST", formData);
                string res = Encoding.UTF8.GetString(responseBytes);
                webClient.Dispose();

                string[] ret = res.Substring(1, res.Length - 2).Split(':');
                if (ret[0] == "lastID")
                {
                    int.TryParse(ret[1], NumberStyles.Number, CultureInfo.InvariantCulture, out lastScanId);
                    return true;
                }
            }
            catch (WebException ew)
            {
                
            }
            catch (Exception exp)
            {
                ErrorMessage.ExceptionMessageBox.Show(exp);
            }
            return false;
        }
        static public bool  exportPhp(InventoryData inv , out string res)
        {
            res = null;
            string URL = @"http://localhost/MedStock2/web/app_dev.php/SetInventory?XDEBUG_SESSION_START=netbeans-xdebug";
            WebClient webClient = new WebClient();

            NameValueCollection formData = new NameValueCollection();
            try
            {

                //formData["badgeId"] = inv.BadgeID;
                formData["serialNumberRfid"] = inv.serialNumberDevice;
                formData["firstname"] = inv.userFirstName;
                formData["lastname"] = inv.userLastName;
                formData["idScanEvent"] = inv.IdScanEvent.ToString(CultureInfo.InvariantCulture);
                formData["eventDate"] = inv.eventDate.ToString("yyyy-MM-dd HH:mm:ss");
                formData["nbTagAdded"] = inv.nbTagAdded.ToString(CultureInfo.InvariantCulture);
                formData["nbTagPresent"] = inv.nbTagPresent.ToString(CultureInfo.InvariantCulture);
                formData["nbTagRemoved"] = inv.nbTagRemoved.ToString(CultureInfo.InvariantCulture);
                formData["nbTagAll"] = inv.nbTagAll.ToString(CultureInfo.InvariantCulture);
                formData["listTagAdded"] = string.Join(";", (string[])inv.listTagAdded.ToArray(typeof(string)));
                formData["listTagPresent"] = string.Join(";", (string[])inv.listTagPresent.ToArray(typeof(string)));
                formData["listTagRemoved"] = string.Join(";", (string[])inv.listTagRemoved.ToArray(typeof(string)));
               // formData["listTagAll"] = string.Join(";", (string[])inv.listTagAll.ToArray(typeof(string)));

          
                webClient.Headers[HttpRequestHeader.UserAgent] =
                    "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.13) Gecko/20101203 Firefox/3.6.13";
                byte[] responseBytes = webClient.UploadValues(URL, "POST", formData);
                res = Encoding.UTF8.GetString(responseBytes);
                webClient.Dispose();
                return true;
            }
            catch (WebException ew)
            {

            }
            catch (Exception exp)
            {
                ErrorMessage.ExceptionMessageBox.Show(exp);
            }
            return false;
        }
    }

}
