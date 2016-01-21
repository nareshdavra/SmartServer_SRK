using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Net;
using System.Windows.Forms;
using TcpIP_class;
using DataClass;
using System.Threading;

namespace ControlLib.Rfid
{
    public class ArmClient : IDisposable
    {
        private TcpArmDevice _currentTcpArmDevice;        

        public bool bUnlockAll = true;

        private HashSet<string> scanResult = new HashSet<string>();

        private TcpV3 _tcpv3; 
        public TcpV3 tcpv3
        {
            set
            {
                _tcpv3 = value;
            }
        }  

        public List<string> ScanResult
        {
            get { return scanResult.ToList<string>(); }            
        }

        private ArmClient nextScanDevice;

        public ArmClient NextScanDevice
        {
            get
            {
                return nextScanDevice;
            }

            set
            {
                nextScanDevice = value;
            }
        }

        public ArmClient(String IP, int port,TcpV3 v3)
        {
            try
            {
                if (_currentTcpArmDevice != null)
                {
                    _currentTcpArmDevice = null;
                }
                this._tcpv3 = v3;         
                _currentTcpArmDevice = new TcpArmDevice(IP, port);
                _currentTcpArmDevice.DeviceEvent += _currentTcpArmDevice_DeviceEvent;
                if (_tcpv3.lidghtList != null && _tcpv3.lidghtList.Count > 0)
                { 
                    _currentTcpArmDevice.SetLightList(_tcpv3.lidghtList);                    
                }
                this._tcpv3.lblRfidAddress.Text = _currentTcpArmDevice.getLocalEndPoint();
            }
            catch (Exception exp)
            {
                //TcpV3.logger.Error(exp);
                throw exp;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_currentTcpArmDevice != null)
                {
                    _currentTcpArmDevice.Release();                    
                    scanResult.Clear();
                    _currentTcpArmDevice = null;
                    scanResult.Clear();
                }
            }
            catch (Exception exp)
            {
                //TcpV3.logger.Error(exp);
                _currentTcpArmDevice = null;
            }
        }

        public void scanDevice(bool unlockAll)
        {
            if (_currentTcpArmDevice == null) return;
            if (unlockAll)
            {
                _currentTcpArmDevice.RequestScan(true);
            }
            else
            {
                ScanOption[] so = { ScanOption.NO_UNLOCK };
                _currentTcpArmDevice.RequestScan(so);
            }
        }

        public void stopDevice()
        {
            if (_currentTcpArmDevice == null) return;

            _currentTcpArmDevice.StopContinuosScan();
        }

        public void stopLed()
        {
            if (_currentTcpArmDevice == null) return;
            _currentTcpArmDevice.StopLightingTagsLed();
        }

        public DataClass.DeviceStatus getDeviceStatus
        {
            get
            {
                return _currentTcpArmDevice.GetStatus();
            }
        }

        private void _currentTcpArmDevice_DeviceEvent(TcpIP_class.rfidReaderArgs args)
        {
            switch (args.RN_Value)
            {
                // Event when failed to connect          
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_FailedToConnect:
                    break;
                // Event when release the object
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_Disconnected:
                    break;

                //Event when device is connected
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_Client_Connected:
                    break;

                // Event when scan started
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_ScanStarted:
                    break;

                //event when fail to start scan
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_ReaderFailToStartScan:
                    break;
                  
                //event when a new tag is identify
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_TagAdded:                    
                    if (_tcpv3.scanResult.Add(args.Message)) 
                    { _tcpv3.countShow(args.Message); }
                    break;
                case rfidReaderArgs.ReaderNotify.RN_Led_Found:
                    string[] found = args.Message.ToString().Split(new char[]{','},StringSplitOptions.RemoveEmptyEntries);
                    _tcpv3.foundLed(this,found);                    
                    //MessageBox.Show(found.Lehngth + " tags found for"+_currentTcpArmDevice.getLocalEndPoint() + ",Press OK to TurnOff LED");                    
                    //_currentTcpArmDevice.StopLightingTagsLed();
                    
                    break;
                case rfidReaderArgs.ReaderNotify.RN_SetLightListModified:
                    string[] modifiedLightList = args.Message.ToString().Split(new char[]{','},StringSplitOptions.RemoveEmptyEntries);
                    _tcpv3.lidghtList = _tcpv3.lidghtList.Except(modifiedLightList).ToList();                    
                    break;
                    
                // Event when scan completed
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_ScanCompleted:
                    
                    /*
                    List<string> list = null;
                    try
                    {
                        list = _currentTcpArmDevice.GetLastInventory().listTagAll.Cast<string>().ToList();
                        List<string> found = null;

                        if (list.Count > 0 && _tcpv3.lidghtList != null && _tcpv3.lidghtList.Count > 0)
                        {
                            found = list.FindAll(str => _tcpv3.lidghtList.Contains(str)).AsParallel().ToList<string>();
                            _tcpv3.lidghtList = _tcpv3.lidghtList.Except(found).ToList();
                        }
                        if (listTag1.Count < list.Count)
                        {
                            HashSet<string> set = new HashSet<string>(list);
                            if (list != null)
                            {
                                listTag1.UnionWith(set);
                            }
                        }
                        scanResult.UnionWith(listTag1);
                        _tcpv3.countShow("");
                        //if (found != null && found.Count > 0)
                        //{
                            //DeviceStatus deviceStatus = _currentTcpArmDevice.GetStatus();
                            //if (deviceStatus == DeviceStatus.DS_Ready && deviceStatus != DeviceStatus.DS_InScan)
                            //{
                            //lbl:
                                //bool res = _currentTcpArmDevice.StartLightingTagsLed(found);
                                //if (res)
                                //{
                                //    MessageBox.Show("Light On Reader " + _currentTcpArmDevice.GetSerialNumber() + "\nPress Ok to turn Led off and continue scan");
                                //    _currentTcpArmDevice.StopLightingTagsLed();
                                //}
                                //else
                                //{
                                //    _currentTcpArmDevice.StopScan();
                                //    goto lbl;
                                //}
                            //}
                        //}
                        if (_tcpv3.BlnDisableAutoStop)
                        {
                            //scanDevice(bUnlockAll);
                        }
                        else
                        {
                            if (_tcpv3.lidghtList != null && _tcpv3.lidghtList.Count > 0)
                            {
                                //scanDevice(bUnlockAll);
                            }
                            else
                            {
                                _tcpv3.stopBtn.Invoke((MethodInvoker)delegate
                                {
                                    _tcpv3.stopBtn.PerformClick();
                                });
                                _tcpv3.ReleaseDevice();                                
                            }
                        }
                    }
                    catch (Exception ee) { }*/
                    break;

                //error when error during scan
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_ReaderScanTimeout:
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_ErrorDuringScan:

                    ////messageDelegate("Info : Scan has error");
                    break;
                case TcpIP_class.rfidReaderArgs.ReaderNotify.RN_ScanCancelByHost:

                    ////messageDelegate("Info : Scan Stopped");
                    if (_tcpv3.lidghtList != null && _tcpv3.lidghtList.Count > 0)
                    {
                        scanDevice(bUnlockAll);
                    }
                    break;
            }


            Application.DoEvents();
        }
    }
}
