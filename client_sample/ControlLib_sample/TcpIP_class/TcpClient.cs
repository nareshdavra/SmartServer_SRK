using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Threading;
using DataClass;
using DBClass;
using MySql.Data.MySqlClient;
using SDK_SC_RfidReader;
using System.Data;

namespace TcpIP_class
{
    public class TcpIpClient
    {
        public enum RetCode
        {
            RC_Data_Error = -5,
            RC_Device_Not_In_Ready_State = -4,
            RC_MissingArg = -3,
            RC_UnknownError = -2,
            RC_FailedToConnect = -1,
            RC_Succeed = 1,
            RC_Failed = 0,
        }
        #region arm Declaration
        private EventWaitHandle eventEndScan = new AutoResetEvent(false);
        private EventWaitHandle eventStartScan = new AutoResetEvent(false);
        public delegate void DeviceEventHandlerTcp(rfidReaderArgs args);
        public event DeviceEventHandlerTcp DeviceEventTcp;
        public  enum CpuKind
        {
            Unknown = 0x00,
            IsArm = 0x01,
            IsWindows = 0x02,
        }
        private CpuKind _cpuKind = CpuKind.Unknown;

        public CpuKind CpuKindValue
        {
            get { return _cpuKind; }
            set { _cpuKind = value; }
        }

        public TcpArmDevice _tcpArmDevice;
        private DeviceStatus _tcpArmDeviceStatus = DeviceStatus.DS_NotReady;
        private  CpuKind GetDeviceKind(string ip,int port)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                CreateArmDevice(ip);
            }
            if (_cpuKind == CpuKind.Unknown)
            {
                try
                {
                    TcpClient tcpclnt = new TcpClient();
                    string Data = null;
                    try
                    {
                        tcpclnt.Connect(ip, port);
                    }
                    catch
                    {
                       
                    }

                    string cmd = "PING?";
                    try
                    {
                        SendData(tcpclnt, cmd);

                        if (GetData(tcpclnt, out Data) == 1)
                        {
                            retCodeStr = Data;
                            if (Data.Equals(ReturnType.pingServerOk))
                            {
                                _cpuKind = CpuKind.IsWindows;
                               
                            }
                        }

                    }
                    catch
                    {
                       
                    }
                    finally
                    {
                        tcpclnt.Close();
                    }
                
                }
                catch
                {

                }
            }
           
            return _cpuKind;
        }
        public void CreateArmDevice(string ip)
        {
             try
            {
                if (_tcpArmDevice != null)
                {
                    _tcpArmDevice.Release();
                    _tcpArmDevice = null;
                }
                // create a TcpDevice: 
                // - initialize a TcpClient (connection)
                // - initialize the device instance (get basic details)
                _tcpArmDevice = new TcpArmDevice(ip);
                _cpuKind = CpuKind.IsArm;
                _tcpArmDeviceStatus = DeviceStatus.DS_Ready;

                // Subscribe to RFID device event (C# SDK)
                _tcpArmDevice.DeviceEvent += OnTcpDeviceEvent;

            }
            catch
            {

            }
        }
        private int ScanId = 0;
        private void OnTcpDeviceEvent(rfidReaderArgs args)
        {
            switch (args.RN_Value)
            {
                case rfidReaderArgs.ReaderNotify.RN_Connected:
                     _tcpArmDeviceStatus = DeviceStatus.DS_Ready;
                    break;

                case rfidReaderArgs.ReaderNotify.RN_Disconnected:
                    _tcpArmDeviceStatus = DeviceStatus.DS_NotReady;
                    break;

                case rfidReaderArgs.ReaderNotify.RN_FailedToConnect:
                    _tcpArmDeviceStatus = DeviceStatus.DS_NotReady;
                    break;

                case rfidReaderArgs.ReaderNotify.RN_ScanStarted:
                    _tcpArmDeviceStatus = DeviceStatus.DS_InScan;
                    if (eventStartScan != null)
                        eventStartScan.Set();
                    break;

                case rfidReaderArgs.ReaderNotify.RN_ReaderFailToStartScan:
                    _tcpArmDeviceStatus = DeviceStatus.DS_DoorOpen;
                    break;

                case rfidReaderArgs.ReaderNotify.RN_ScanCancelByHost:
                    if (eventEndScan != null)
                        eventEndScan.Set();
                    _tcpArmDeviceStatus = DeviceStatus.DS_Ready;
                    break;

                case rfidReaderArgs.ReaderNotify.RN_ScanCompleted:
                    _tcpArmDeviceStatus = DeviceStatus.DS_Ready;
                    break;

                case rfidReaderArgs.ReaderNotify.RN_TagAdded:
                    break;

                case rfidReaderArgs.ReaderNotify.RN_ErrorDuringScan:
                case rfidReaderArgs.ReaderNotify.RN_ReaderScanTimeout:
                    _tcpArmDeviceStatus = DeviceStatus.DS_InError;
                    break;

                case rfidReaderArgs.ReaderNotify.RN_Door_Opened:
                    _tcpArmDeviceStatus = DeviceStatus.DS_DoorOpen;
                    break;

                case rfidReaderArgs.ReaderNotify.RN_Door_Closed:
                    _tcpArmDeviceStatus = DeviceStatus.DS_DoorClose;
                    break;

                case rfidReaderArgs.ReaderNotify.RN_DoorOpenTooLong:
                   
                    break;
            }
            var handler = DeviceEventTcp;
            if (handler != null)
            {

                DeviceEventTcp(args);
            }
        }

        #endregion
       
        // Client API
        public RetCode renewFP(string strIP, int port)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                const string cmd = "RENEWFP?";
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.restartOk))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode getSystemTime(string strIP, int port, out DateTime dt)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            dt = DateTime.MinValue;
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                const string cmd = "GET_SYSTEM_ZONE?";
                try
                {
                    SendData(tcpclnt, cmd);

                    string Data = null;
                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        if (DateTime.TryParse(Data, out dt))
                        {
                            ret = RetCode.RC_Succeed;
                        }
                        else
                            ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }

            return ret;
        }
        public RetCode restartDevice(string strIP, int port)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                const string cmd = "RESTART?";
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.restartOk))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode rebootDevice(string strIP, int port)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "REBOOT?";
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.rebootOk))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
     
        #region ARM Support
        
        public RetCode RequestIsUsingTcpNotification(string strIP, int port)
        {
            RetCode ret = RetCode.RC_UnknownError;
            string response = string.Empty;
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "IS_USING_TCP_NOTIFICATION?";
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out response) == 1)
                    {
                        if (response.Equals(ReturnType.readerOk))
                        {
                            ret = RetCode.RC_Succeed;
                        }
                    }
                }
                catch (Exception)
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                ret = RetCode.RC_UnknownError;
            }
            #endregion

            return ret;
        }
        public RetCode pingServer(string strIP, int port)
        {
            Console.WriteLine("Ping server " + strIP);
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "PING?";
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.pingServerOk))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                try
                {
                    DeviceStatus StatDev = DeviceStatus.DS_InError;
                    if (_tcpArmDevice != null)
                        StatDev = _tcpArmDevice.GetImmediateStatus();
                        ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_Failed;
                }
            } 
            #endregion
            return ret;

        }
        public RetCode getDevice(string strIP, int port, out rfidPluggedInfo[] pluggedDevice)
        {
            RetCode ret = RetCode.RC_UnknownError;
            pluggedDevice = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                string cmd;
                TcpClient tcpclnt = new TcpClient();
                string[] invDev = null;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                cmd = "GET_DEVICE_STR?";
                SendData(tcpclnt, cmd);
                try
                {
                    string strNbDev;
                    int nbdev = 0;
                    GetData(tcpclnt, out strNbDev);
                    int.TryParse(strNbDev, out nbdev);
                    if (nbdev > 0)
                    {
                        invDev = new string[nbdev];

                        for (int loop = 0; loop < nbdev; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                invDev[loop] = data;
                            }
                            else
                                invDev[loop] = null;
                        }
                        pluggedDevice = new rfidPluggedInfo[invDev.Length];
                        int nIndex = 0;
                        foreach (string s in invDev)
                        {
                            string[] tmp = s.Split(';');
                            if (tmp.Length == 2)
                            {
                                pluggedDevice[nIndex] = new rfidPluggedInfo();
                                pluggedDevice[nIndex].SerialRFID = tmp[0];
                                pluggedDevice[nIndex++].deviceType = (DeviceType)int.Parse(tmp[1]);
                            }
                            else if (tmp.Length == 4)
                            {
                                pluggedDevice[nIndex] = new rfidPluggedInfo();
                                pluggedDevice[nIndex].SerialRFID = tmp[0];
                                pluggedDevice[nIndex].deviceType = (DeviceType)int.Parse(tmp[1]);
                                pluggedDevice[nIndex].SoftwareVersion = tmp[2];
                                pluggedDevice[nIndex++].HardwareVersion = tmp[3];
                            }
                            else
                            {
                                pluggedDevice[nIndex++] = null;
                            }
                        }
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                if (_tcpArmDevice != null)
                {
                    pluggedDevice = new rfidPluggedInfo[1];
                    pluggedDevice[0] = new rfidPluggedInfo();
                    pluggedDevice[0].SerialRFID = _tcpArmDevice.GetSerialNumber();
                    pluggedDevice[0].deviceType = _tcpArmDevice.GetType();
                    pluggedDevice[0].SoftwareVersion = _tcpArmDevice.GetSoftwareVersion();
                    pluggedDevice[0].HardwareVersion = _tcpArmDevice.GetHardwareVersion();
                    ret = RetCode.RC_Succeed;

                }
                else
                {
                    retCodeStr = ReturnType.noData;
                    ret = RetCode.RC_Failed;
                }
            } 
            #endregion
            return ret;
        }
        public RetCode pingDevice(string strIP, int port, string serialRFID)
        {
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string cmd;
                string Data = null;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "PINGDEVICE?;" + serialRFID;
                else
                    cmd = "PINGDEVICE?";
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.readerOk))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                if (_tcpArmDevice != null)
                    ret = RetCode.RC_Succeed;
                else
                    ret = RetCode.RC_Failed;
            } 
            #endregion
            return ret;

        }  
        public RetCode getStatus(string strIP, int port, string serialRFID, out string status)
        {
            RetCode ret = RetCode.RC_UnknownError;
            status = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                string cmd;
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "STATUS?;" + serialRFID;
                else
                    cmd = "STATUS?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    status = Data;
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                status = _tcpArmDeviceStatus.ToString();
                ret = RetCode.RC_Succeed;
            } 
            #endregion
            return ret;

        }
        public RetCode getStatusWithNumberOfTag(string strIP, int port, string serialRFID, out string status, out int nbTag)
        {
            RetCode ret = RetCode.RC_UnknownError;
            status = null;
            nbTag = 0;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                string cmd;
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "STATUS_AND_TAG?;" + serialRFID;
                else
                    cmd = "STATUS_AND_TAG?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    string[] tmp = Data.Split(';');

                    if (tmp.Length == 2)
                    {
                        status = tmp[0];
                        nbTag = int.Parse(tmp[1]);
                    }
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                status = _tcpArmDeviceStatus.ToString();
                nbTag = _tcpArmDevice.CptTag;
                ret = RetCode.RC_Succeed;
            } 
            #endregion
            return ret;
        }
        public RetCode requestScan(string strIP, int port, string serialRFID)
        {
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "SCAN?;" + serialRFID;
                else
                    cmd = "SCAN?";
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (Data.Equals(ReturnType.scanStarted))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                eventStartScan.Reset();
                _tcpArmDevice.RequestScan(false);
                ret = RetCode.RC_Failed;
                if (eventStartScan.WaitOne(2500,false))
                   ret = RetCode.RC_Succeed;
            } 
            #endregion
            return ret;
        }
        public RetCode requestScanAndWait(string strIP, int port, string serialRFID, out InventoryData ScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            ScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "SCAN_AND_WAIT?;" + serialRFID;
                else
                    cmd = "SCAN_AND_WAIT?";
                try
                {
                    SendData(tcpclnt, cmd);
                    string Data = null;
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    BinaryFormatter bf = new BinaryFormatter();
                    MemoryStream mem = new MemoryStream(Convert.FromBase64String(Data));
                    StoredInventoryData siv = new StoredInventoryData();
                    siv = (StoredInventoryData)bf.Deserialize(mem);
                    InventoryData dt = ConvertInventory.ConvertForUse(siv, null);
                    ScanResult = dt;
                    ret = RetCode.RC_Succeed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                eventStartScan.Reset();
                _tcpArmDevice.RequestScan(false);
                ret = RetCode.RC_Failed;
                if (eventStartScan.WaitOne(2500, false))
                {
                    if (eventEndScan.WaitOne(180000, false))
                    {
                        ScanResult = _tcpArmDevice.GetLastInventory();
                        if (ScanResult != null)
                        {

                            ScanResult.serialNumberDevice = _tcpArmDevice.GetSerialNumber();
                            ret = RetCode.RC_Succeed;
                        }
                        else
                        {
                            ret = RetCode.RC_Data_Error;
                        }
                    }
                    else
                    {
                        ret = RetCode.RC_UnknownError;
                    }
                }
                else
                {
                    ret = RetCode.RC_Device_Not_In_Ready_State;
                }
            } 
            #endregion
            return ret;
        }
        public RetCode requestScanAndWaitWithDB(string strIP, int port, string serialRFID, out InventoryData ScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            ScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();

                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "SCAN_AND_WAIT?;" + serialRFID;
                else
                    cmd = "SCAN_AND_WAIT?";
                try
                {
                    Hashtable ColumnInfo = null;
                    MainDBClass db = new MainDBClass();

                    if (db.OpenDB())
                    {
                        ColumnInfo = db.GetColumnInfo();

                        SendData(tcpclnt, cmd);
                        string Data = null;
                        GetData(tcpclnt, out Data);
                        retCodeStr = Data;
                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream mem = new MemoryStream(Convert.FromBase64String(Data));
                        StoredInventoryData siv = new StoredInventoryData();
                        siv = (StoredInventoryData)bf.Deserialize(mem);
                        InventoryData dt = ConvertInventory.ConvertForUse(siv, ColumnInfo);
                        ScanResult = dt;
                        ret = RetCode.RC_Succeed;

                    }
                    db.CloseDB();
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                eventStartScan.Reset();
                _tcpArmDevice.RequestScan(false);
                ret = RetCode.RC_Failed;
                if (eventStartScan.WaitOne(2500, false))
                {
                    if (eventEndScan.WaitOne(180000, false))
                    {
                        ScanResult = _tcpArmDevice.GetLastInventory();
                        if (ScanResult != null)
                        {
                            ScanResult.serialNumberDevice = _tcpArmDevice.GetSerialNumber();
                            ret = RetCode.RC_Succeed;
                        }
                        else
                        {
                            ret = RetCode.RC_Data_Error;
                        }
                    }
                    else
                    {
                        ret = RetCode.RC_UnknownError;
                    }
                }
                else
                {
                    ret = RetCode.RC_Device_Not_In_Ready_State;
                }
            }
            #endregion
            return ret;

        }
        public RetCode requestScanAndWaitStr(string strIP, int port, string serialRFID, out string StrScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            StrScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "SCAN_AND_WAIT_STR?;" + serialRFID;
                else
                    cmd = "SCAN_AND_WAIT_STR?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    StrScanResult = Data;
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                eventStartScan.Reset();
                _tcpArmDevice.RequestScan(false);
                ret = RetCode.RC_Failed;
                if (eventStartScan.WaitOne(2500, false))
                {
                    if (eventEndScan.WaitOne(180000, false))
                    {
                        InventoryData invTmp = _tcpArmDevice.GetLastInventory();
                        if (invTmp != null)
                        {
                            invTmp.serialNumberDevice = _tcpArmDevice.GetSerialNumber();
                            StrScanResult = "OK;";
                            StrScanResult += invTmp.eventDate.ToString("u") + ";";
                            StrScanResult += invTmp.userFirstName + ";";
                            StrScanResult += invTmp.userLastName + ";";
                            StrScanResult += invTmp.nbTagAll.ToString();
                            foreach (string uid in invTmp.listTagAll)
                            {
                                StrScanResult += ";" + uid;
                            }
                            ret = RetCode.RC_Succeed;
                        }
                        else
                        ret = RetCode.RC_Failed;
                    }
                    else
                    {
                        ret = RetCode.RC_UnknownError;
                    }
                }
                else
                {
                    ret = RetCode.RC_Device_Not_In_Ready_State;
                }
            }
            #endregion
            return ret;
        }
        public RetCode requestStopScan(string strIP, int port, string serialRFID)
        {
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "STOP_SCAN?;" + serialRFID;
                else
                    cmd = "STOP_SCAN?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                eventEndScan.Reset();
                _tcpArmDevice.StopScan();
                ret = RetCode.RC_Failed;
                if (eventEndScan.WaitOne(2500, false))
                    ret = RetCode.RC_Succeed;
            } 
            #endregion
            return ret;

        }
        public RetCode requestGetLastScanWithDB(string strIP, int port, string serialRFID, out InventoryData ScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            ScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                InventoryData dt = null;
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_LAST_SCAN?;" + serialRFID;
                else
                    cmd = "GET_LAST_SCAN?";

                try
                {
                    Hashtable ColumnInfo = null;
                    MainDBClass db = new MainDBClass();

                    if (db.OpenDB())
                    {
                        ColumnInfo = db.GetColumnInfo();
                        SendData(tcpclnt, cmd);
                        GetData(tcpclnt, out Data);
                        retCodeStr = Data;
                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream mem = new MemoryStream(Convert.FromBase64String(Data));
                        StoredInventoryData siv = new StoredInventoryData();
                        siv = (StoredInventoryData)bf.Deserialize(mem);
                        dt = ConvertInventory.ConvertForUse(siv, ColumnInfo);
                        ScanResult = dt;
                        ret = RetCode.RC_Succeed;

                    }
                    db.CloseDB();
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                try
                {
                    ScanResult = _tcpArmDevice.GetLastInventory();
                    if (ScanResult != null)
                    {
                        ScanResult.serialNumberDevice = _tcpArmDevice.GetSerialNumber();
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        ret = RetCode.RC_Data_Error;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
            } 
            #endregion
            return ret;
        }
        public RetCode requestGetLastScan(string strIP, int port, string serialRFID, out InventoryData ScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            ScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                InventoryData dt = null;
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_LAST_SCAN?;" + serialRFID;
                else
                    cmd = "GET_LAST_SCAN?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    if (Data.Equals(ReturnType.Data_Error))
                        ret = RetCode.RC_Data_Error;
                    else if (Data.Equals(ReturnType.ReaderNotInReadyState))
                        ret = RetCode.RC_Device_Not_In_Ready_State;
                    else
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream mem = new MemoryStream(Convert.FromBase64String(Data));
                        StoredInventoryData siv = new StoredInventoryData();
                        siv = (StoredInventoryData)bf.Deserialize(mem);
                        dt = ConvertInventory.ConvertForUse(siv, null);
                        ScanResult = dt;
                        ret = RetCode.RC_Succeed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                ScanResult = _tcpArmDevice.GetLastInventory();
                if (ScanResult != null)
                {
                    ScanResult.serialNumberDevice = _tcpArmDevice.GetSerialNumber();
                    ret = RetCode.RC_Succeed;
                }
                else
                {
                    ret = RetCode.RC_Data_Error;
                }
            } 
            #endregion
            return ret;
        }
        public RetCode getLastDateScanStr(string strIP, int port, string serialRFID, out string strLastDateScan)
        {
            RetCode ret = RetCode.RC_UnknownError;
            strLastDateScan = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
        
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                DateTime dt = DateTime.MaxValue;
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_LAST_SCAN_DATE?;" + serialRFID;
                else
                    cmd = "GET_LAST_SCAN_DATE?";


                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);

                    dt = DateTime.ParseExact(Data, "yyyy-MM-dd HH:mm:ssZ",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AdjustToUniversal);

                    retCodeStr = Data;
                    strLastDateScan = dt.ToString("u");
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                if (_cpuKind == CpuKind.IsArm)
                {
                    try
                    {
                        InventoryData ScanResult = _tcpArmDevice.GetLastInventory();
                        if (ScanResult != null)
                        {
                            strLastDateScan = ScanResult.eventDate.ToString("u"); ;
                            ret = RetCode.RC_Succeed;
                        }
                        else
                        {
                            ret = RetCode.RC_Data_Error;
                        }
                    }
                    catch
                    {
                        strLastDateScan = DateTime.MinValue.ToString("u");
                        ret = RetCode.RC_UnknownError;
                    }
                }
            } 
            #endregion
            return ret;
        }
        public RetCode getLastDateScan(string strIP, int port, string serialRFID, out DateTime LastDateScan)
        {
            RetCode ret = RetCode.RC_UnknownError;
            LastDateScan = DateTime.MaxValue;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                DateTime dt = DateTime.MaxValue;
                string Data = null;
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_LAST_SCAN_DATE?;" + serialRFID;
                else
                    cmd = "GET_LAST_SCAN_DATE?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    //dt = DateTime.Parse(Data);            
                    dt = DateTime.ParseExact(Data, "yyyy-MM-dd HH:mm:ssZ",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AdjustToUniversal);
                    LastDateScan = dt;
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                try
                {
                     InventoryData ScanResult = _tcpArmDevice.GetLastInventory();
                    if (ScanResult != null)
                    {
                        LastDateScan = ScanResult.eventDate;
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        LastDateScan = DateTime.MinValue;
                        ret = RetCode.RC_Data_Error;
                    }
                    
                }
                catch
                {
                    LastDateScan = DateTime.MinValue;
                    ret = RetCode.RC_UnknownError;
                }
            } 
            #endregion
            return ret;
        }
        public RetCode getLastScanID(string strIP, int port, string serialRFID, out int IdScan)
        {
            RetCode ret = RetCode.RC_UnknownError;
            IdScan = -1;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                DateTime dt = DateTime.MaxValue;
                string Data = null;
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_LAST_SCAN_ID?;" + serialRFID;
                else
                    cmd = "GET_LAST_SCAN_ID?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (!Data.Equals(ReturnType.unknownCmd))
                        IdScan = int.Parse(Data);
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                try
                {
                    InventoryData ScanResult = _tcpArmDevice.GetLastInventory();
                    if (ScanResult != null)
                    {
                        IdScan = ScanResult.IdScanEvent;
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        ret = RetCode.RC_Data_Error;
                    }

                    
                }
                catch
                {
                    IdScan = -1;
                    ret = RetCode.RC_UnknownError;
                }
            } 
            #endregion
            return ret;
        }
        public int getLastScanIDFromDb(string strIP)
        {
            MySql.Data.MySqlClient.MySqlConnection cn = null;
            try{
                string myConnectString = "Server=" + strIP + ";Database=smartserver;Uid=spacecode;Pwd=mySuperPassword;";
                cn = new MySql.Data.MySqlClient.MySqlConnection(myConnectString);
                cn.Open();
                MySql.Data.MySqlClient.MySqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "select max(id) from sc_inventory;";
                MySql.Data.MySqlClient.MySqlDataAdapter da = new MySql.Data.MySqlClient.MySqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.Fill(ds);
                int ret = int.Parse(ds.Tables[0].Rows[0].ItemArray[0].ToString());
                cn.Close();
                return ret;
            }
            catch(Exception exp)
            {
                cn.Close();
                return -1;
            }
        }
        public RetCode requestGetScanFromIdEvent(string strIP, int port, string serialRFID, int IdEvent, out InventoryData ScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            ScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string Data = null;
                InventoryData dt = null;
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_SCAN_FROM_ID?;" + serialRFID + ";" + IdEvent;
                else
                    cmd = "GET_SCAN_FROM_ID?;" + IdEvent;
                ;

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    if (Data.Equals(ReturnType.Data_Error))
                        ret = RetCode.RC_Data_Error;
                    else if (Data.Equals(ReturnType.ReaderNotInReadyState))
                        ret = RetCode.RC_Device_Not_In_Ready_State;
                    else
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream mem = new MemoryStream(Convert.FromBase64String(Data));
                        StoredInventoryData siv = new StoredInventoryData();
                        siv = (StoredInventoryData)bf.Deserialize(mem);
                        dt = ConvertInventory.ConvertForUse(siv, null);
                        ScanResult = dt;
                        ret = RetCode.RC_Succeed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                try
                {
                    ScanResult = _tcpArmDevice.GetInventoryById(IdEvent);
                    ScanResult.serialNumberDevice = _tcpArmDevice.GetSerialNumber();
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ScanResult = null;
                    ret = RetCode.RC_UnknownError;
                }
            } 
            #endregion
            return ret;
        }
        public RetCode getScanFromDateWithDB(string strIP, int port, string serialRFID, DateTime Date, out InventoryData[] ScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            ScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_SCAN_FROM_DATE?;" + serialRFID + ";" + Date.ToString("u");
                else
                    cmd = "GET_SCAN_FROM_DATE?;" + Date.ToString("u");

                SendData(tcpclnt, cmd);
                try
                {
                    string strNbInv;
                    int nbinv = 0;
                    GetData(tcpclnt, out strNbInv);
                    int.TryParse(strNbInv, out nbinv);
                    if (nbinv > 0)
                    {
                        InventoryData[] invScan = new InventoryData[nbinv];

                        for (int loop = 0; loop < nbinv; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                try
                                {
                                    Hashtable ColumnInfo = null;
                                    MainDBClass db = new MainDBClass();

                                    if (db.OpenDB())
                                    {
                                        ColumnInfo = db.GetColumnInfo();
                                        StoredInventoryData sid = new StoredInventoryData();
                                        BinaryFormatter bf = new BinaryFormatter();
                                        MemoryStream mem = new MemoryStream(Convert.FromBase64String(data));
                                        sid = (StoredInventoryData)bf.Deserialize(mem);
                                        invScan[loop] = ConvertInventory.ConvertForUse(sid, ColumnInfo);
                                        db.CloseDB();
                                    }

                                }
                                catch
                                {

                                }
                            }
                            else
                                invScan[loop] = null;
                        }
                        ScanResult = new InventoryData[invScan.Length];
                        invScan.CopyTo(ScanResult, 0);
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                List<InventoryData> lstInv = _tcpArmDevice.GetInventories(Date.ToUniversalTime(), DateTime.Now.AddDays(1).ToUniversalTime());
                if ((lstInv != null) && lstInv.Count > 0)
                {
                    foreach (InventoryData inv in lstInv)
                    {
                        inv.serialNumberDevice = _tcpArmDevice.GetSerialNumber();
                    }

                    ScanResult = lstInv.ToArray();
                    ret = RetCode.RC_Succeed;
                }
                else
                {
                    retCodeStr = ReturnType.noData;
                    ret = RetCode.RC_Failed;
                } 
            #endregion
            }
            return ret;
        }
        public RetCode getScanFromDate(string strIP, int port, string serialRFID, DateTime Date, out InventoryData[] ScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            ScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }

            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();

                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_SCAN_FROM_DATE?;" + serialRFID + ";" + Date.ToString("u");
                else
                    cmd = "GET_SCAN_FROM_DATE?;" + Date.ToString("u");

                SendData(tcpclnt, cmd);
                try
                {
                    string strNbInv;
                    int nbinv = 0;
                    GetData(tcpclnt, out strNbInv);
                    int.TryParse(strNbInv, out nbinv);
                    if (nbinv > 0)
                    {
                        InventoryData[] invScan = new InventoryData[nbinv];

                        for (int loop = 0; loop < nbinv; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                try
                                {

                                    StoredInventoryData sid = new StoredInventoryData();
                                    BinaryFormatter bf = new BinaryFormatter();
                                    MemoryStream mem = new MemoryStream(Convert.FromBase64String(data));
                                    sid = (StoredInventoryData)bf.Deserialize(mem);
                                    invScan[loop] = ConvertInventory.ConvertForUse(sid, null);
                                }
                                catch
                                {

                                }
                            }
                            else
                                invScan[loop] = null;
                        }
                        ScanResult = new InventoryData[invScan.Length];
                        invScan.CopyTo(ScanResult, 0);
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            if (_cpuKind == CpuKind.IsArm)
            {
                List<InventoryData> lstInv = _tcpArmDevice.GetInventories(Date.ToUniversalTime(), DateTime.Now.AddDays(1).ToUniversalTime());
                if ((lstInv != null) && (lstInv.Count > 0))
                    ScanResult = lstInv.ToArray();
            }
            return ret;
        }
        public RetCode requestGetLastScanStr(string strIP, int port, string serialRFID, out string StrScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            StrScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_LAST_SCAN_STR?;" + serialRFID;
                else
                    cmd = "GET_LAST_SCAN_STR?";

                try
                {
                    SendData(tcpclnt, cmd);
                    string data = null;
                    GetData(tcpclnt, out data);
                    retCodeStr = data;
                    StrScanResult = data;
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                InventoryData invTmp = _tcpArmDevice.GetLastInventory();
                if (invTmp != null)
                {
                    invTmp.serialNumberDevice = _tcpArmDevice.GetSerialNumber();
                    StrScanResult = "OK;";
                    StrScanResult += invTmp.eventDate.ToString("u") + ";";
                    StrScanResult += invTmp.userFirstName + ";";
                    StrScanResult += invTmp.userLastName + ";";
                    StrScanResult += invTmp.nbTagAll.ToString();
                    foreach (string uid in invTmp.listTagAll)
                    {
                        StrScanResult += ";" + uid;
                    }
                    ret = RetCode.RC_Succeed;
                }
                else
                {
                    ret = RetCode.RC_Data_Error;
                }
            }
            #endregion
            return ret;

        }
        /// <summary>
        /// Ask the TCP device if it's able to light tags' led
        /// </summary>
        /// <param name="strIP">device IP Address</param>
        /// <param name="port">device Port</param>
        /// <param name="available">bool variable given by client to store answer</param>
        /// <returns></returns>
        public RetCode RequestIsSPCE2Available(string strIP, int port, out bool available)
        {
            RetCode ret = RetCode.RC_UnknownError;
            available = false;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string response = string.Empty;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "IS_SPCE2_AVAILABLE?";
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out response) == 1)
                    {
                        if (response.Equals(ReturnType.SPCE2Available))
                        {
                            ret = RetCode.RC_Succeed;
                            available = true;
                        }

                        else if (response.Equals(ReturnType.readerNotReady))
                            ret = RetCode.RC_Failed;
                    }
                }
                catch (Exception)
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {

                if (_tcpArmDevice.GetSoftwareVersion().StartsWith("1"))
                    ret = RetCode.RC_Failed;
                else
                {
                    available = true;
                    ret = RetCode.RC_Succeed;
                }
            } 
            #endregion
            return ret;
        }
        /// <summary>
        /// Ask the TCP device if it's able to light tags' led
        /// </summary>
        /// <param name="strIP">device IP Address</param>
        /// <param name="port">device Port</param>
        /// <param name="available">bool variable given by client to store answer</param>
        /// <returns></returns>
        public RetCode RequestFirmwareVersion(string strIP, int port, out double fv)
        {
            fv = 0.0;
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string response = string.Empty;
                fv = 0.0;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "GET_FIRMWARE_VERSION?";
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out response) == 1)
                    {
                        double.TryParse(response.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture,
                            out fv);
                        ret = RetCode.RC_Succeed;
                    }
                }
                catch (Exception)
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                double.TryParse(_tcpArmDevice.GetSoftwareVersion().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out fv);
                ret = RetCode.RC_Succeed;
            } 
            #endregion
            return ret;
        }
        /// <summary>
        /// Ask the TCP device to light leds of a list of tags
        /// </summary>
        /// <param name="strIP">device IP Address</param>
        /// <param name="port">device Port</param>
        /// <param name="tagsToLight">List of tag IDs to light on (successfully lighted tags will be removed from the list)</param>
        /// <returns></returns>
        public RetCode RequestStartLighting(string strIP, int port, List<String> tagsToLight)
        {
            var ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                var tcpclnt = new TcpClient();
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch (Exception)
                {
                    return RetCode.RC_FailedToConnect;
                }

                var cmdBuilder = new StringBuilder("START_LIGHTING_LED?");

                foreach (string tagId in tagsToLight)
                    cmdBuilder.Append(tcpUtils.TCPDelimiter).Append(tagId);

                try
                {
                    SendData(tcpclnt, cmdBuilder.ToString());

                    string response;

                    if (GetData(tcpclnt, out response) == 1)
                    {
                        string[] responseFragments = response.Split(tcpUtils.TCPDelimiter);

                        if (responseFragments.Length == 0)
                        {
                            //tcpClient will be closed in finally clause.
                            return RetCode.RC_UnknownError;
                        }

                        if (responseFragments.Length > 1)
                        // add each optional parameters (= tag ID(s) not found / not lighted) in a new list (that the client will use after this call)
                        {
                            tagsToLight = new List<string>();

                            // start at index 1, as index 0 is response code
                            for (int i = 1; i < responseFragments.Length; ++i)
                            {
                                tagsToLight.Add(responseFragments[i].Trim());
                            }
                        }

                        else
                        {
                            // all tags have been found : empty tag IDs list
                            tagsToLight.Clear();
                        }

                        switch (responseFragments[0].Trim())
                        {
                            case ReturnType.noData:
                                ret = RetCode.RC_Data_Error;
                                break;
                            case ReturnType.SPCE2StartLighting:
                                ret = RetCode.RC_Succeed;
                                break;
                            case ReturnType.SPCE2Notavailable:
                            case ReturnType.readerNotReady:
                            case ReturnType.SPCE2NoTags:
                                ret = RetCode.RC_Failed;
                                break;


                        }
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                _tcpArmDevice.StartLightingTagsLed(tagsToLight);
                ret = RetCode.RC_Succeed;
            } 
            #endregion
            return ret;
        }
        /// <summary>
        /// Ask the TCP device to stop lighting leds
        /// </summary>
        /// <param name="strIP">device IP Address</param>
        /// <param name="port">device Port</param>
        /// <returns></returns>
        public RetCode RequestStopLighting(string strIP, int port)
        {
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string response = string.Empty;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string command = "STOP_LIGHTING_LED?";

                try
                {
                    SendData(tcpclnt, command);

                    if (GetData(tcpclnt, out response) == 1)
                        ret = RetCode.RC_Succeed;
                }
                catch (Exception)
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                _tcpArmDevice.StopLightingTagsLed();
            } 
            #endregion

            return ret;
        }
        /// <summary>
        /// Ask the TCP device to rewrite a tag ID
        /// </summary>
        /// <param name="strIP">device IP Address</param>
        /// <param name="port">device Port</param>
        /// <param name="oldTagId">Tag ID before this one get renamed</param>
        /// <param name="newTagId">New tag ID for the given "oldTag"</param>
        /// <param name="resultCode">Code result from Server</param>
        /// <param name="writeModeType"> Writemode type :   0 - old method
        ///                                                 1 - Write RW
        ///                                                 2 - Write Decimal</param>
        /// <returns></returns>
        public RetCode RequestWriteBlock(string strIP, int port, string oldTagId, string newTagId, out WriteCode resultCode, int writeModeType = 0)
        {
            resultCode = WriteCode.WC_Error;
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string response = string.Empty;
              
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string command = String.Format("WRITE_BLOCK?{2}{0}{2}{1}{2}{3}", oldTagId, newTagId,
                    tcpUtils.TCPDelimiter, writeModeType);

                try
                {
                    SendData(tcpclnt, command);

                    if (GetData(tcpclnt, out response) == 1)
                    {
                        if (String.IsNullOrEmpty(response)) return RetCode.RC_UnknownError;

                        int resultCodeValue;
                        if (!int.TryParse(response, out resultCodeValue)) return RetCode.RC_UnknownError;

                        resultCode = (WriteCode)resultCodeValue;

                        ret = RetCode.RC_Succeed;
                    }
                }
                catch (Exception)
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            } 
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                resultCode = _tcpArmDevice.RewriteUid(oldTagId, newTagId);
                ret = RetCode.RC_Succeed;
            } 
            #endregion
            return ret;
        }

        public RetCode getFridgeCurrentTemp(string strIP, int port, string serialRFID, out tempInfo tmpInfo)
        {
            tmpInfo = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_FRIDGE_CURRENT_TEMP?;" + serialRFID;
                else
                    cmd = "GET_FRIDGE_CURRENT_TEMP?";

                SendData(tcpclnt, cmd);
                try
                {
                    string IdTemp;
                    GetData(tcpclnt, out IdTemp);

                    if ((!IdTemp.Equals(ReturnType.wrongReader)) || (!IdTemp.Equals(ReturnType.readerNotExist)) ||
                        (!IdTemp.Equals(ReturnType.noData)))
                    {
                        tempInfo usTmp = null;
                        try
                        {
                            usTmp = new tempInfo();
                            BinaryFormatter bf = new BinaryFormatter();
                            MemoryStream mem = new MemoryStream(Convert.FromBase64String(IdTemp));
                            usTmp = (tempInfo)bf.Deserialize(mem);

                        }
                        catch
                        {
                            usTmp = null;
                        }

                        tmpInfo = usTmp;
                        tcpclnt.Close();

                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        return RetCode.RC_Failed;
                    }
                }
                catch
                {
                    return RetCode.RC_UnknownError;
                }
            }
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                Dictionary<DateTime, double> lstTemp = new Dictionary<DateTime, double>();
                if (_tcpArmDevice != null)
                    lstTemp = _tcpArmDevice.GetTemperatureMeasures(DateTime.Now.AddHours(-3.0), DateTime.Now.AddHours(1));

                if ((lstTemp == null) || (lstTemp.Count == 0))
                    return RetCode.RC_Data_Error;

                tempInfo currentTemp = new tempInfo();
                DateTime recordDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 15);
                currentTemp.CreationDate = recordDate.ToUniversalTime();

                List<DateTime> lstDates = new List<DateTime>(lstTemp.Keys);
                int nIndex = 0;

                DateTime lastMeasureTime = lstDates[0];
                double lastMeasure = lstTemp[lastMeasureTime];

                //Record current point utc time

                if (DateTime.Now.Minute == 0)
                {
                    // Add point for minute 0;
                    currentTemp.lastTempValue = lastMeasure;
                    currentTemp.lastTempAcq = currentTemp.CreationDate;
                }
                else
                for (int loop = 0; loop < DateTime.Now.Minute; loop++)
                {
                    // find good point

                    currentTemp.lastTempAcq = currentTemp.CreationDate.AddMinutes(loop);
                    while (currentTemp.lastTempAcq.ToLocalTime() > lastMeasureTime)
                    {
                        if (nIndex < lstDates.Count - 1)
                        {
                            nIndex++;
                            lastMeasureTime = lstDates[nIndex];
                            lastMeasure = lstTemp[lastMeasureTime];
                        }
                        else
                        {
                            break; // no more point
                        }
                    }

                    currentTemp.lastTempValue = lastMeasure;

                    currentTemp.tempArray.Add(loop, lastMeasure);
                    currentTemp.tempBottle.Add(loop, lastMeasure);
                    currentTemp.tempChamber.Add(loop, lastMeasure);

                    currentTemp.nbValueTemp++;
                    if (currentTemp.max < lastMeasure) currentTemp.max = lastMeasure;
                    if (currentTemp.min > lastMeasure) currentTemp.min = lastMeasure;

                    currentTemp.sumTemp += lastMeasure;
                    currentTemp.mean = currentTemp.sumTemp / currentTemp.nbValueTemp;
                }
                tmpInfo = currentTemp;
            }
            #endregion
            return RetCode.RC_Succeed;
        }
        public RetCode getFridgeTempFromDate(string strIP, int port, string serialRFID, DateTime Date, out tempInfo[] TempFridge)
        {
            TempFridge = null;
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }


            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                string[] tempVal = null;
                TcpClient tcpclnt = new TcpClient();
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_TEMP_FROM_DATE?;" + serialRFID + ";" +
                          Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ");
                else
                    cmd = "GET_TEMP_FROM_DATE?;" + Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ");

                SendData(tcpclnt, cmd);
                try
                {
                    string strNbInv;
                    int nbinv = 0;
                    GetData(tcpclnt, out strNbInv);
                    int.TryParse(strNbInv, out nbinv);
                    if (nbinv > 0)
                    {
                        tempVal = new string[nbinv];

                        for (int loop = 0; loop < nbinv; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                tempVal[loop] = data;
                            }
                            else
                                tempVal[loop] = null;
                        }
                        TempFridge = new tempInfo[tempVal.Length];
                        int nIndex = 0;
                        foreach (string str in tempVal)
                        {
                            BinaryFormatter bf = new BinaryFormatter();
                            MemoryStream mem = new MemoryStream(Convert.FromBase64String(str));
                            TempFridge[nIndex++] = (tempInfo)bf.Deserialize(mem);
                        }

                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                Dictionary<DateTime, double> lstTemp = new Dictionary<DateTime, double>();
                DateTime datefrom = Date.AddHours(-3.0);
                if (_tcpArmDevice != null)
                    lstTemp = _tcpArmDevice.GetTemperatureMeasures(datefrom, DateTime.Now.AddHours(1));

                if ((lstTemp == null) || (lstTemp.Count == 0))
                    return RetCode.RC_Data_Error;

                List<DateTime>  lstDates = new List<DateTime>(lstTemp.Keys);
                int nIndex = 0;
                DateTime  lastMeasureTime = lstDates[0];
                double lastMeasure = lstTemp[lastMeasureTime];

                List<tempInfo> lstTempInfos = new List<tempInfo>();
                tempInfo currTemp = new tempInfo();

                DateTime initTime = new DateTime(datefrom.Year, datefrom.Month, datefrom.Day, datefrom.Hour, 59, 15);
                DateTime currTime = initTime;

                int loopTemp = 0;

                while (currTime < DateTime.Now)
                {
                    currTime = currTime.AddMinutes(1);
                    loopTemp++;
                    if (currTime.Minute == 0)
                    {
                        loopTemp = 0;
                        currTemp = new tempInfo();
                        currTemp.CreationDate = currTime.ToUniversalTime();
                    }

                    currTemp.lastTempAcq = currTemp.CreationDate.AddMinutes(loopTemp);
                    while (currTemp.lastTempAcq.ToLocalTime() > lastMeasureTime)
                    {
                        if (nIndex < lstDates.Count - 1)
                        {
                            nIndex++;
                            lastMeasureTime = lstDates[nIndex];
                            lastMeasure = lstTemp[lastMeasureTime];
                        }
                        else
                        {
                            break; // no more point
                        }
                    }

                    currTemp.lastTempValue = lastMeasure;

                    currTemp.tempArray.Add(loopTemp, lastMeasure);
                    currTemp.tempBottle.Add(loopTemp, lastMeasure);
                    currTemp.tempChamber.Add(loopTemp, lastMeasure);

                    currTemp.nbValueTemp++;
                    if (currTemp.max < lastMeasure) currTemp.max = lastMeasure;
                    if (currTemp.min > lastMeasure) currTemp.min = lastMeasure;

                    currTemp.sumTemp += lastMeasure;
                    currTemp.mean = currTemp.sumTemp / currTemp.nbValueTemp;

                    if (loopTemp == 59) //tempInfo completed
                        lstTempInfos.Add(currTemp);

                }
                if (currTemp != null) // add last not completed
                    lstTempInfos.Add(currTemp);

                TempFridge = new tempInfo[lstTempInfos.Count];
                nIndex = 0;
                foreach (tempInfo tpi in lstTempInfos)
                {
                    TempFridge[nIndex++] = tpi;
                }

                ret = RetCode.RC_Succeed;

            }
            #endregion
            return ret;
        }
      
        #endregion
        #region Not ARM Support

        // Function to get the last badge read - Use to get badge ID from a device when create a user.
        public RetCode getLastBadge(string strIP, int port, out string lastBadgeRead)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            lastBadgeRead = null;
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "GET_LAST_BADGE?";
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        lastBadgeRead = Data;
                        ret = RetCode.RC_Succeed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }

        // Function develop for Medireport
        public RetCode requestScanFromSpareData(string strIP, int port, string serialRFID, string spareData1, string spareData2, out InventoryData[] ScanResult)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            InventoryData[] invScan = null;
            TcpClient tcpclnt = new TcpClient();
            ScanResult = null;
            string cmd;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_SCAN_FROM_SPAREDATA?;" + serialRFID + ";" + spareData1 + ";" + spareData2;
                else
                    cmd = "GET_SCAN_FROM_SPAREDATA?;" + spareData1 + ";" + spareData2;

                SendData(tcpclnt, cmd);
                try
                {
                    string strNbInv;
                    int nbinv = 0;
                    GetData(tcpclnt, out strNbInv);
                    int.TryParse(strNbInv, out nbinv);
                    if (nbinv > 0)
                    {
                        invScan = new InventoryData[nbinv];

                        for (int loop = 0; loop < nbinv; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                try
                                {

                                    StoredInventoryData sid = new StoredInventoryData();
                                    BinaryFormatter bf = new BinaryFormatter();
                                    MemoryStream mem = new MemoryStream(Convert.FromBase64String(data));
                                    sid = (StoredInventoryData)bf.Deserialize(mem);
                                    invScan[loop] = ConvertInventory.ConvertForUse(sid, null);
                                }
                                catch
                                {

                                }
                            }
                            else
                                invScan[loop] = null;
                        }
                        ScanResult = new InventoryData[invScan.Length];
                        invScan.CopyTo(ScanResult, 0);
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode setReservedData(string strIP, int port, string spareData1, string spareData2, string BadgeID = null)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (string.IsNullOrEmpty(BadgeID))
                {
                    cmd = "SET_RESERVED_DATA?;";
                    cmd += spareData1 + ";";
                    cmd += spareData2;
                }
                else
                {
                    cmd = "SET_RESERVED_DATA?;";
                    cmd += spareData1 + ";";
                    cmd += spareData2 + ";";
                    cmd += BadgeID;
                }

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    if (Data.Equals("SET_OK"))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }

        //Server de notification a start for medireport used by other now
        public RetCode GetTcpServerNotificationInfo(string strIP, int port, out bool bEnable, out string hostIp, out int hostPort)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;

            bEnable = false;
            hostIp = null;
            hostPort = -1;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "GET_TCP_NOTIFICATION?;";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if ((Data.Equals(ReturnType.Data_Error)) || (Data.Equals(ReturnType.noData)))
                        ret = RetCode.RC_Failed;
                    else
                    {
                        string[] strArray = Data.Split(';');
                        if (strArray.Length != 3)
                        {
                            ret = RetCode.RC_Data_Error;
                        }
                        else
                        {
                            if (strArray[0] == "1")
                                bEnable = true;
                            else
                                bEnable = false;
                            hostIp = strArray[1];
                            int.TryParse(strArray[2], out hostPort);

                            ret = RetCode.RC_Succeed;
                        }
                    }

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode SetTcpServerNotificationInfo(string strIP, int port, bool bEnable, string tcpServerIp, int tcpServerPort)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "SET_TCP_NOTIFICATION?;";
                cmd += Convert.ToInt32(bEnable).ToString() + ";";
                cmd += tcpServerIp + ";";
                cmd += tcpServerPort;
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    if (Data.Equals(ReturnType.SetSQL_OK))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;

        }
        public RetCode SetTcpServerNotificationOnOff(string strIP, int port, bool bEnable)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (bEnable)
                    cmd = "SET_TCP_NOTIFICATION_ONOFF?;1";
                else
                    cmd = "SET_TCP_NOTIFICATION_ONOFF?;0";
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    if (Data.Equals(ReturnType.TestSQL_OK))
                    {
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        ret = RetCode.RC_Failed;
                    }

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode TestTcpServerNotification(string strIP, int port, out bool bTestResult, out string ExceptionMessageError)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            ExceptionMessageError = null;
            bTestResult = false;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "TEST_TCP_NOTIFICATION?;";
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    if (Data.Equals(ReturnType.TestSQL_OK))
                    {
                        ret = RetCode.RC_Succeed;
                        bTestResult = true;
                    }
                    else
                    {
                        ExceptionMessageError = Data;
                        ret = RetCode.RC_Failed;
                    }

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }

        // Fridge function 
        public RetCode getFridgeFanemInfo(string strIP, int port, string serialRFID, out DataFanemInfo dfi)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            TcpClient tcpclnt = new TcpClient();
            dfi = null;
            string cmd;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_FANEM_INFO?;" + serialRFID;
                else
                    cmd = "GET_FANEM_INFO?";

                SendData(tcpclnt, cmd);
                try
                {
                    string idTemp;
                    GetData(tcpclnt, out idTemp);

                    if ((!idTemp.Equals(ReturnType.wrongReader)) || (!idTemp.Equals(ReturnType.readerNotExist)) ||
                        (!idTemp.Equals(ReturnType.noData)))
                    {
                        DataFanemInfo usTmp = null;
                        try
                        {
                            usTmp = new DataFanemInfo();
                            BinaryFormatter bf = new BinaryFormatter();
                            MemoryStream mem = new MemoryStream(Convert.FromBase64String(idTemp));
                            usTmp = (DataFanemInfo)bf.Deserialize(mem);

                        }
                        catch
                        {
                            usTmp = null;
                        }

                        dfi = usTmp;
                        tcpclnt.Close();

                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        return RetCode.RC_Failed;
                    }
                }
                catch
                {
                    return RetCode.RC_UnknownError;
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return RetCode.RC_Succeed;
        }

        //user function
        public RetCode getUserListWithGrant(string strIP, int port, string serialRFID, out DeviceGrant[] user)
        {
            user = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_USER_WITH_GRANT?;" + serialRFID;
                else
                    cmd = "GET_USER_WITH_GRANT?";

                SendData(tcpclnt, cmd);
                try
                {
                    string strNbUser;
                    int nbus = 0;
                    GetData(tcpclnt, out strNbUser);
                    int.TryParse(strNbUser, out nbus);

                    if (nbus > 0)
                    {
                        DeviceGrant[] usTmp = new DeviceGrant[nbus];
                        for (int loop = 0; loop < nbus; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                try
                                {
                                    DeviceGrant tmpdev = new DeviceGrant();
                                    BinaryFormatter bf = new BinaryFormatter();
                                    MemoryStream mem = new MemoryStream(Convert.FromBase64String(data));
                                    tmpdev = (DeviceGrant)bf.Deserialize(mem);
                                    usTmp[loop] = tmpdev;
                                }
                                catch
                                {
                                    usTmp[loop] = null;
                                }
                            }
                        }
                        tcpclnt.Close();
                        user = usTmp;
                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        return RetCode.RC_Failed;
                    }
                }
                catch
                {
                    return RetCode.RC_UnknownError;
                }
            }
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                /*List<DeviceGrant> lstUsersGranted = _tcpArmDevice.GetUsersList();
                if ((lstUsersGranted != null) && (lstUsersGranted.Count> 0))
                {
                      DeviceGrant[] usTmp = new DeviceGrant[lstUsersGranted.Count];
                      for (int loop = 0; loop < lstUsersGranted.Count; loop++)
                        {
                            usTmp[loop] = lstUsersGranted[loop];
                        }
                      user = usTmp;
                }
                else
                {
                     retCodeStr = ReturnType.noData;
                     return RetCode.RC_Failed;
                }*/
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            #endregion
            return RetCode.RC_Succeed;
        }
        public RetCode getUserList(string strIP, int port, string serialRFID, out UserClassTemplate[] user)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            TcpClient tcpclnt = new TcpClient();
            user = null;
            string cmd;
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_USER?;" + serialRFID;
                else
                    cmd = "GET_USER?";

                SendData(tcpclnt, cmd);
                try
                {
                    string strNbUser;
                    int nbus = 0;
                    GetData(tcpclnt, out strNbUser);
                    int.TryParse(strNbUser, out nbus);

                    if (nbus > 0)
                    {
                        UserClassTemplate[] usTmp = new UserClassTemplate[nbus];
                        for (int loop = 0; loop < nbus; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                try
                                {
                                    UserClassTemplate tmpdev = new UserClassTemplate();
                                    BinaryFormatter bf = new BinaryFormatter();
                                    MemoryStream mem = new MemoryStream(Convert.FromBase64String(data));
                                    tmpdev = (UserClassTemplate)bf.Deserialize(mem);
                                    usTmp[loop] = tmpdev;
                                }
                                catch
                                {
                                    usTmp[loop] = null;
                                }
                            }
                        }
                        tcpclnt.Close();
                        user = usTmp;
                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        return RetCode.RC_Failed;
                    }
                }
                catch
                {
                    return RetCode.RC_UnknownError;
                }
            }
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                /* List<DeviceGrant> lstusers = _tcpArmDevice.GetUsersList();
                 if ((lstusers != null) && (lstusers.Count > 0))
                 {
                     UserClassTemplate[] usTmp = new UserClassTemplate[lstusers.Count];
                     for (int loop = 0; loop < lstusers.Count; loop++)
                     {
                         usTmp[loop] = lstusers[loop].user;
                     }
                     user = usTmp;
                 }
                 else
                 {
                     retCodeStr = ReturnType.noData;
                     return RetCode.RC_Failed;
                 }*/
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            #endregion
            return RetCode.RC_Succeed;
        }
        public RetCode deleteUser(string strIP, int port, string FirstName, string LastName, string serialRFID)
        {
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                string Data = null;
                TcpClient tcpclnt = new TcpClient();
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "DEL_USER?;" + FirstName + ";" + LastName + ";" + serialRFID;
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.delUser))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                    tcpclnt.Close();
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                /* string login = FirstName + "_" + LastName;
                 if (_tcpArmDevice.RemoveUser(login))
                     ret = RetCode.RC_Succeed;
                 else
                     ret = RetCode.RC_Failed;*/
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            #endregion
            return ret;
        }
        public RetCode addUserFromTemplate(string strIP, int port, string FirstName, string LastName, string template, string ReaderBadgeID = null)
        {
            RetCode ret = RetCode.RC_UnknownError;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                string Data = null;
                TcpClient tcpclnt = new TcpClient();
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                string cmd;
                if (string.IsNullOrEmpty(ReaderBadgeID))
                    cmd = "ADD_USER_FROM_TEMPLATE?;" + FirstName + ";" + LastName + ";" + template;
                else
                    cmd = "ADD_USER_FROM_TEMPLATE?;" + FirstName + ";" + LastName + ";" + template + ";" + ReaderBadgeID;
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.addUserTemplate))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                    tcpclnt.Close();
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            #endregion
            #region ARM
            if (_cpuKind == CpuKind.IsArm)
            {
                // TO keep compatibility ,  impose to recreate a device grant to reinitialize uer.
                // Impose to update each fingerprint as at creation idf fingerprint already exist in local db it will remain active.
                // Impose to update permision for SAS and SmartDrawer as device grant created with all permission.
                /* DeviceGrant dg = new DeviceGrant();
                 dg.user = new UserClassTemplate();
                 dg.user.firstName = FirstName;
                 dg.user.lastName = LastName;
                 dg.user.BadgeReaderID = ReaderBadgeID;
                 dg.userGrant = UserGrant.UG_MASTER_AND_SLAVE;
                 dg.user.template = template;
                 if (_tcpArmDevice.AddUser(dg))
                 {
                     //Todo deserialize template, update all.
                     ret = RetCode.RC_Succeed;
                 }
                 else
                 {
                     ret = RetCode.RC_Failed;
                 }*/

                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            #endregion
            return ret;
        }
        public RetCode addUserFinger(string strIP, int port, string FirstName, string LastName, int fingerNumber, byte[] template)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string fingerHexa = tcpUtils.ByteArrayToHexString(template);

                string cmd = "ADD_USER_FINGER?;" + FirstName + ";" + LastName + ";" + fingerNumber.ToString() + ";" +
                             fingerHexa;
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.addUserFinger))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                    tcpclnt.Close();
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode addUserGrant(string strIP, int port, string FirstName, string LastName, string serialRFID, UserGrant userGrant)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "ADD_USER_GRANT?;" + FirstName + ";" + LastName + ";" + serialRFID + ";" + (int)userGrant;
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.addUserGrant))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                    tcpclnt.Close();
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode deleteUserGrant(string strIP, int port, string FirstName, string LastName, string serialRFID)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "DEL_USER_GRANT?;" + FirstName + ";" + LastName + ";" + serialRFID;
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.delUserGrant))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                    tcpclnt.Close();
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode addUserBadge(string strIP, int port, string FirstName, string LastName, string BadgeID)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "ADD_USER_BADGE?;" + FirstName + ";" + LastName + ";" + BadgeID;
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.addUserBadge))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                    tcpclnt.Close();
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode deleteUserBadge(string strIP, int port, string FirstName, string LastName)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "DEL_USER_BADGE?;" + FirstName + ";" + LastName;
                try
                {
                    SendData(tcpclnt, cmd);

                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        retCodeStr = Data;
                        if (Data.Equals(ReturnType.delUserBadge))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                    tcpclnt.Close();
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
      

        //internal function - Not Used to flash firmware over TCP
        public RetCode FlashFirmware(string strIP, int port, string serialRFID, string filename)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "FLASH_FIRMWARE?;" + serialRFID + ";" + filename;

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (Data.Equals(ReturnType.flashStarted))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }

        // Date in string - Not used
        public RetCode getScanFromDateStr(string strIP, int port, string serialRFID, string Date, out string[] StrScanResult)
        {
            RetCode ret = RetCode.RC_UnknownError;
            StrScanResult = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            #region Windows
            if (_cpuKind == CpuKind.IsWindows)
            {
                TcpClient tcpclnt = new TcpClient();
                string cmd;
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_SCAN_FROM_DATE_STR?;" + serialRFID + ";" + Date;
                else
                    cmd = "GET_SCAN_FROM_DATE_STR?;" + Date;

                SendData(tcpclnt, cmd);
                try
                {
                    string strNbInv;
                    int nbinv = 0;
                    GetData(tcpclnt, out strNbInv);
                    int.TryParse(strNbInv, out nbinv);
                    if (nbinv > 0)
                    {
                        string[] invScan = new string[nbinv];

                        for (int loop = 0; loop < nbinv; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                invScan[loop] = data;
                            }
                            else
                                invScan[loop] = null;
                        }
                        StrScanResult = new string[invScan.Length];
                        invScan.CopyTo(StrScanResult, 0);
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            #endregion
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }

        // Scan function and SBR mode 
        public RetCode requestSetWaitMode(string strIP, int port, string serialRFID)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "SET_WAIT_MODE?;" + serialRFID;
                else
                    cmd = "SET_WAIT_MODE?";
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (Data.Equals(ReturnType.waitModeStarted))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;

        }
        public RetCode requestUnSetWaitMode(string strIP, int port, string serialRFID)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "UNSET_WAIT_MODE?;" + serialRFID;
                else
                    cmd = "UNSET_WAIT_MODE?";
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (Data.Equals(ReturnType.waitModeStopped))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode requestScanAccumulate(string strIP, int port, string serialRFID)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "ACCUMULATE_SCAN?;" + serialRFID;
                else
                    cmd = "ACCUMULATE_SCAN?";
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (Data.Equals(ReturnType.scanStarted))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;

        }
        public RetCode requestStopAccumulteScan(string strIP, int port, string serialRFID)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "STOP_ACCUMULATE_SCAN?;" + serialRFID;
                else
                    cmd = "STOP_ACCUMULATE_SCAN?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;

        }

        // Light function
        public RetCode getLightValue(string strIP, int port, string serialRFID, out int lightValue)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string cmd;
            int power;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            lightValue = 0;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_LIGHT_VALUE?;" + serialRFID;
                else
                    cmd = "GET_LIGHT_VALUE?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (!int.TryParse(Data, out power))
                        power = 0;
                    lightValue = power;
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode setLightValue(string strIP, int port, string serialRFID, int lightValue)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "SET_LIGHT_VALUE?;" + serialRFID + ";" + lightValue.ToString();
                else
                    cmd = "SET_LIGHT_VALUE?;" + lightValue.ToString();

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (Data.Equals(ReturnType.setLightOk))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }

        // Not used
        public RetCode getTagAtIndex(string strIP, int port, string serialRFID, uint nIndex, out List<String> uidList)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string cmd;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            uidList = null;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {

                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_TAG_AT_INDEX?" + tcpUtils.TCPDelimiter + serialRFID + tcpUtils.TCPDelimiter +
                          nIndex.ToString();
                else
                {

                    return RetCode.RC_MissingArg;
                }

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    if (!Data.Equals(ReturnType.noData))
                    {
                        uidList = new List<string>(Data.Split(tcpUtils.TCPDelimiter));
                        ret = RetCode.RC_Succeed;
                    }
                    else
                        ret = RetCode.RC_Failed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }

        public RetCode getDeviceStr(string strIP, int port, out string[] pluggedDevice)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            RetCode ret = RetCode.RC_UnknownError;
            string cmd;
            TcpClient tcpclnt = new TcpClient();
            pluggedDevice = null;
            string[] invDev = null;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                cmd = "GET_DEVICE_STR?";
                SendData(tcpclnt, cmd);
                try
                {
                    string strNbDev;
                    int nbdev = 0;
                    GetData(tcpclnt, out strNbDev);
                    int.TryParse(strNbDev, out nbdev);
                    if (nbdev > 0)
                    {
                        invDev = new string[nbdev];

                        for (int loop = 0; loop < nbdev; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                invDev[loop] = data;
                            }
                            else
                                invDev[loop] = null;
                        }
                        pluggedDevice = new string[invDev.Length];
                        invDev.CopyTo(pluggedDevice, 0);
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        retCodeStr = ReturnType.noData;
                        ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }

        // Ejada (neve used)
        public RetCode GetSqlExportInfo(string strIP, int port, out bool bEnable, out string sqlHost, out string sqlLogin, out string sqlPwd, out string sqlDbName, out string sqlTableName)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;

            bEnable = false;
            sqlHost = null;
            sqlLogin = null;
            sqlLogin = null;
            sqlPwd = null;
            sqlDbName = null;
            sqlTableName = null;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "GET_SQL_EXPORT?;";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if ((Data.Equals(ReturnType.Data_Error)) || (Data.Equals(ReturnType.noData)))
                        ret = RetCode.RC_Failed;
                    else
                    {
                        string[] strArray = Data.Split(';');
                        if (strArray.Length != 6)
                        {
                            ret = RetCode.RC_Data_Error;
                        }
                        else
                        {
                            if (strArray[0] == "1")
                                bEnable = true;
                            else
                                bEnable = false;
                            sqlHost = strArray[1];
                            sqlDbName = strArray[2];
                            sqlLogin = strArray[3];
                            sqlPwd = strArray[4];
                            sqlTableName = strArray[5];
                            ret = RetCode.RC_Succeed;
                        }
                    }

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode SetSqlExportInfo(string strIP, int port, bool bEnable, string sqlHost, string sqlLogin, string sqlPwd, string sqlDbName, string sqlTableName)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "SET_SQL_EXPORT?;";
                cmd += Convert.ToInt32(bEnable).ToString() + ";";
                cmd += sqlHost + ";";
                cmd += sqlLogin + ";";
                cmd += sqlPwd + ";";
                cmd += sqlDbName + ";";
                cmd += sqlTableName;
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    if (Data.Equals(ReturnType.SetSQL_OK))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;

        }
        public RetCode SetSqlExportOnOff(string strIP, int port, bool bEnable)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (bEnable)
                    cmd = "SET_SQL_ONOFF?;1";
                else
                    cmd = "SET_SQL_ONOFF?;0";
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    if (Data.Equals(ReturnType.TestSQL_OK))
                    {
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        ret = RetCode.RC_Failed;
                    }

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode TestSqlExportConnection(string strIP, int port, out bool bTestResult, out string ExceptionMessageError)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            ExceptionMessageError = null;
            bTestResult = false;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "TEST_SQL_EXPORT?;";
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    if (Data.Equals(ReturnType.TestSQL_OK))
                    {
                        ret = RetCode.RC_Succeed;
                        bTestResult = true;
                    }
                    else
                    {
                        ExceptionMessageError = Data;
                        ret = RetCode.RC_Failed;
                    }

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }

        // SmartTracker function
        public RetCode storeInventory(string strIP, int port, string serialRFID)
        {
            RetCode ret = RetCode.RC_UnknownError;
            string[] invData = null;
            TcpClient tcpclnt = new TcpClient();

            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                string cmd = "GET_INVENTORY?;" + serialRFID;

                try
                {

                    SendData(tcpclnt, cmd);

                    string LastStoreScan;
                    GetData(tcpclnt, out LastStoreScan);

                    MainDBClass db = new MainDBClass();

                    if (db.OpenDB())
                    {
                        string[] tmpData = db.GetInventory(serialRFID, LastStoreScan);
                        if (tmpData != null)
                        {
                            invData = new string[tmpData.Length];
                            tmpData.CopyTo(invData, 0);
                        }
                        db.CloseDB();
                    }

                    if (invData != null)
                    {
                        SendData(tcpclnt, invData.Length.ToString());

                        foreach (string inv in invData)
                        {
                            SendData(tcpclnt, inv);
                        }

                        ret = RetCode.RC_Succeed;
                    }
                    ret = RetCode.RC_Failed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }

            return ret;

        }
        public RetCode setUserRefresh(string strIP, int port)
        {
            RetCode ret = RetCode.RC_UnknownError;
            string Data = null;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                string cmd = "REFRESH_USER?";
                try
                {
                    SendData(tcpclnt, cmd);
                    if (GetData(tcpclnt, out Data) == 1)
                    {
                        if (Data.Equals(ReturnType.refreshUser))
                            ret = RetCode.RC_Succeed;
                        else
                            ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }

            return ret;

        }
        public RetCode getUser(string strIP, int port, string serialRFID)
        {
            TcpClient tcpclnt = new TcpClient();
            UserClassTemplate[] user;
            string cmd;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }
                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_USER?;" + serialRFID;
                else
                    cmd = "GET_USER?";

                SendData(tcpclnt, cmd);
                try
                {
                    string strNbUser;
                    int nbus = 0;
                    GetData(tcpclnt, out strNbUser);
                    int.TryParse(strNbUser, out nbus);

                    if (nbus > 0)
                    {
                        UserClassTemplate[] usTmp = new UserClassTemplate[nbus];
                        for (int loop = 0; loop < nbus; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                try
                                {
                                    UserClassTemplate tmpdev = new UserClassTemplate();
                                    BinaryFormatter bf = new BinaryFormatter();
                                    MemoryStream mem = new MemoryStream(Convert.FromBase64String(data));
                                    tmpdev = (UserClassTemplate)bf.Deserialize(mem);
                                    usTmp[loop] = tmpdev;
                                }
                                catch
                                {
                                    usTmp[loop] = null;
                                }
                            }
                        }
                        tcpclnt.Close();
                        user = usTmp;

                        MainDBClass db = new MainDBClass();

                        if (db.OpenDB())
                        {
                            //db.DeleteAllUser();
                            foreach (UserClassTemplate us in user)
                            {
                                if (us != null)
                                {
                                    db.StoreUser(us);
                                    db.StoreGrant(us, serialRFID, strIP, UserGrant.UG_MASTER_AND_SLAVE);
                                }
                            }
                            db.CloseDB();
                        }
                    }
                }
                catch
                {
                    return RetCode.RC_UnknownError;
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return RetCode.RC_Succeed;
        }
        public RetCode getDevice(string strIP, int port, out DeviceInfo[] device)
        {
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            TcpClient tcpclnt = new TcpClient();
            device = null;
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "GET_DEVICE?";
                SendData(tcpclnt, cmd);
                try
                {
                    string strNbDev;
                    int nbdev = 0;
                    GetData(tcpclnt, out strNbDev);
                    int.TryParse(strNbDev, out nbdev);

                    if (nbdev > 0)
                    {
                        DeviceInfo[] dev = new DeviceInfo[nbdev];
                        for (int loop = 0; loop < nbdev; loop++)
                        {
                            string data;
                            if (GetData(tcpclnt, out data) == 1)
                            {
                                DeviceInfo tmpdev = new DeviceInfo();
                                BinaryFormatter bf = new BinaryFormatter();
                                MemoryStream mem = new MemoryStream(Convert.FromBase64String(data));
                                tmpdev = (DeviceInfo)bf.Deserialize(mem);
                                dev[loop] = tmpdev;
                            }
                        }
                        tcpclnt.Close();
                        device = dev;
                    }
                }
                catch
                {
                    return RetCode.RC_UnknownError;
                }
            }
            if (_cpuKind == CpuKind.IsArm)
            {
                if (_tcpArmDevice != null)
                {
                    DeviceInfo[] dev = new DeviceInfo[1];
                    dev[0] = new DeviceInfo();
                    dev[0].IP_Server = strIP;
                    dev[0].Port_Server = 8080;
                    dev[0].DeviceName = "Odroid_" + strIP;
                    dev[0].SerialRFID = _tcpArmDevice.GetSerialNumber();
                    dev[0].deviceType = _tcpArmDevice.GetType();
                    dev[0].bLocal = 0;
                    device = dev;
                    return RetCode.RC_Succeed;

                }
                else
                {
                    retCodeStr = ReturnType.noData;
                    return RetCode.RC_Failed;
                }
            }
            return RetCode.RC_Succeed;
        }
        public RetCode enableRemoteAccess(string strIP, int port, DeviceInfo device)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "SET_REMOTE_ACCESS?";
                cmd += ";" + device.SerialRFID;
                cmd += ";" + device.IP_Server;
                cmd += ";" + device.Port_Server.ToString();

                SendData(tcpclnt, cmd);
                string Data;

                GetData(tcpclnt, out Data);
                tcpclnt.Close();

                if (Data.Equals(ReturnType.setRemoteOk))
                    ret = RetCode.RC_Succeed;
                else
                    ret = RetCode.RC_Failed;
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode disableRemoteAccess(string strIP, int port, DeviceInfo device)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                string cmd = "DELETE_REMOTE_ACCESS?";
                cmd += ";" + device.SerialRFID;
                cmd += ";" + TcpIP_class.tcpUtils.getLocalIp().ToString();

                SendData(tcpclnt, cmd);

                string Data;
                GetData(tcpclnt, out Data);
                tcpclnt.Close();

                if (Data.Equals("DELETE_REMOTE_ACCESS OK"))
                    ret = RetCode.RC_Succeed;
                else
                    ret = RetCode.RC_Failed;
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode getTimeZone(string strIP, int port, out double timeZone)
        {
            RetCode ret = RetCode.RC_UnknownError;
            string cmd;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            timeZone = 0.0;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "GET_TIME_ZONE?";

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    timeZone = double.Parse(Data);
                    ret = RetCode.RC_Succeed;
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode setTimeZone(string strIP, int port, double timeZone)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "SET_TIME_ZONE?;" + timeZone.ToString();

                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (Data.Equals(ReturnType.timeZoneok))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode setIP(string strIP, int port, int dhcp, string network, string ip, string mask, string gateway = null)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                cmd = "SET_IP?;" + network + ";" + dhcp.ToString() + ";" + ip + ";" + mask;
                if (gateway != null)
                    cmd += ";" + gateway;
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;
                    if (Data.Equals(ReturnType.SetIP_OK))
                        ret = RetCode.RC_Succeed;
                    else
                        ret = RetCode.RC_Failed;

                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        public RetCode GetAllDeviceInfo(string strIP, int port, string serialRFID, out bool pingOK, out DeviceStatus status, out DateTime lastDateScan)
        {
            RetCode ret = RetCode.RC_UnknownError;
            TcpClient tcpclnt = new TcpClient();
            string Data = null;
            string cmd;

            pingOK = false;
            status = DeviceStatus.DS_NotReady;
            lastDateScan = DateTime.MaxValue;
            DateTime dt = DateTime.MaxValue;
            if (_cpuKind == CpuKind.Unknown)
            {
                GetDeviceKind(strIP, port);
            }
            if (_cpuKind == CpuKind.IsWindows)
            {
                try
                {
                    tcpclnt.Connect(strIP, port);
                }
                catch
                {
                    return RetCode.RC_FailedToConnect;
                }

                if (!string.IsNullOrEmpty(serialRFID))
                    cmd = "GET_DEVICE_INFO?;" + serialRFID;
                else
                    cmd = "GET_DEVICE_INFO?";
                try
                {
                    SendData(tcpclnt, cmd);
                    GetData(tcpclnt, out Data);
                    retCodeStr = Data;

                    string[] strData = Data.Split(';');

                    if (strData.Length == 3)
                    {
                        if (strData[0].ToUpper() == "TRUE") pingOK = true;
                        status = (DeviceStatus)Enum.Parse(typeof(DeviceStatus), strData[1]);
                        dt = DateTime.ParseExact(strData[2], "yyyy-MM-dd HH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);
                        lastDateScan = dt;
                        ret = RetCode.RC_Succeed;
                    }
                    else
                    {
                        ret = RetCode.RC_Failed;
                    }
                }
                catch
                {
                    ret = RetCode.RC_UnknownError;
                }
                finally
                {
                    tcpclnt.Close();
                }
            }

            if (_cpuKind == CpuKind.IsArm)
            {
                throw new Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + " Function not Allowed with odroid");
            }
            return ret;
        }
        #endregion
        #region Internal Need
        private string retCodeStr;
        public string ReceivedData { get { return retCodeStr; } }
        private void SendData(TcpClient tcpclnt, string Data)
        {
            Stream stm = tcpclnt.GetStream();
            ASCIIEncoding asen = new ASCIIEncoding();
            byte[] data = asen.GetBytes(tcpUtils.EncodeString(Data));
            int len = data.Length;
            byte[] prefix = BitConverter.GetBytes(len);
            stm.Write(prefix, 0, prefix.Length); // fixed 4 bytes
            stm.Write(data, 0, data.Length);

        }
        private int GetData(TcpClient tcpclnt, out string Data)
        {
            Data = null;
            try
            {
                Stream stm = tcpclnt.GetStream();

                byte[] readMsgLen = new byte[4];
                stm.Read(readMsgLen, 0, 4);

                int dataLen = BitConverter.ToInt32(readMsgLen, 0);
                byte[] readMsgData = new byte[dataLen];

                int dataRead = 0;
                do
                {
                    dataRead += stm.Read(readMsgData, dataRead, dataLen - dataRead);

                } while (dataRead < dataLen);
                Data = tcpUtils.DecodeString(System.Text.Encoding.ASCII.GetString(readMsgData, 0, dataLen));
                return 1;
            }
            catch
            {
                return 0;
            }


        }
        #endregion
    }
}
