using DataClass;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using TcpIP_class;
using Val = BusLib.Validation.BOValidation;

namespace ControlLib.Rfid
{
    public partial class RfidReader : UserControl
    {
        #region Old RFID Object Declaration

        public delegate void UpdateTextCallback(string text);

        TAGMR.ReadTags ObjReadTags;
        int numTags = 0;
        int mRfidPortNumber = 6901;

        #endregion

        //IpScanner[] sc = null;
        string[] ArrSpaceCodeIP = null;

        #region New Spacecode RFID Declaration

        //private rfidPluggedInfo[] arrayOfPluggedDevice = null;
        //private RFID_Device device = null;
        //private int selectedDevice = 0;
        //private ComboBox comboBoxDevice = new ComboBox();

        TcpV3 v = new TcpV3();
        #endregion

        #region Control Property

        public enum MessagePosition
        {
            Bottom = 0,
            Right = 1
        }

        private MessagePosition mMessageDisplayPosition;
        /// <summary>
        /// Message Display Position
        /// </summary>
        [Browsable(true)]
        public MessagePosition MessageDisplayPosition
        {
            get { return mMessageDisplayPosition; }
            set { mMessageDisplayPosition = value; SetMsgPosition(); }
        }

        private void SetMsgPosition()
        {
            if (mMessageDisplayPosition == MessagePosition.Right)
            {
                PnlRfidSelection.Left = BtnStop.Right + 3;
                PnlRfidSelection.Top = BtnStop.Top + 2;
                //lblMessageDisplay.Left = PnlRfidSelection.Right + 3;
                //lblMessageDisplay.Top = PnlRfidSelection.Top + 2;
                this.Height = BtnStop.Bottom + 3;
                this.Width = PnlRfidSelection.Right + 3;
                    //+ lblMessageDisplay.Width;
            }
            if (mMessageDisplayPosition == MessagePosition.Bottom)
            {
                PnlRfidSelection.Left = BtnStart.Left;
                PnlRfidSelection.Top = BtnStart.Bottom + 3;
                //lblMessageDisplay.Left = PnlRfidSelection.Left;
                //lblMessageDisplay.Top = PnlRfidSelection.Bottom + 3;
                this.Height = PnlRfidSelection.Bottom + 2;
                    //+ lblMessageDisplay.Height;
                this.Width = BtnStop.Right + 3;
            }
        }

        private int mWidthButton;
        /// <summary>
        /// Button Width
        /// </summary>
        public int WidthButton
        {
            get { return BtnStart.Width; }
            set { mWidthButton = value; SetControl(); }
        }

        private int mWidthRfidCount;
        /// <summary>
        /// Rfid Counter Width
        /// </summary>
        public int WidthRfidCount
        {
            get { return lblRfidCount.Width; }
            set { mWidthRfidCount = value; SetControl(); }
        }

        private void SetControl()
        {
            BtnStart.Width = BtnStop.Width = mWidthButton;
            lblRfidCount.Width = mWidthRfidCount;
            BtnStart.Left = 0;
            lblRfidCount.Left = BtnStart.Right + 3;
            BtnStop.Left = lblRfidCount.Right + 3;
            SetMsgPosition();
            //if (mMessageDisplayPosition == MessagePosition.Right)
            //{
            //    lblMessageDisplay.Left = BtnStop.Right + 3;
            //    Width = lblMessageDisplay.Right + 2;
            //}
            //else
            //{
            //    Width = BtnStop.Right + 2;
            //}
        }

        #endregion

        #region Value Assignment Property

        private string mRFIDIPAddress;
        /// <summary>
        /// RFID Ip Address
        /// </summary>
        public string RFIDIPAddress
        {
            get { return mRFIDIPAddress; }
            set { mRFIDIPAddress = value; }
        }

        private string mSpaceCodeIPList;
        /// <summary>
        /// Space Code IP List With Comma(,) Seperated.
        /// </summary>
        public string SpaceCodeIPList
        {
            get { return mSpaceCodeIPList; }
            set { mSpaceCodeIPList = value; }
        }

        private string mRfidTagList;
        /// <summary>
        /// Rfid Tag List
        /// </summary>
        public string RfidTagList
        {
            get { return txtRfidTagList.Text.ToUpper(); }
            set { txtRfidTagList.Text = value; }
        }

        private ModCont.cTextBox mSingleRfidTag;
        /// <summary>
        /// Get Rfid Machine Reading Tag At Scanning Time.
        /// </summary>
        public ModCont.cTextBox SingleRfidTag
        {
            get { return mSingleRfidTag; }
            set { mSingleRfidTag = value; }
        }

        private bool mBlnDisableAutoStop;
        /// <summary>
        /// To Disable Auto Stop Rfid Scan
        /// </summary>
        public bool DisableAutoStop
        {
            get { return mBlnDisableAutoStop; }
            set { mBlnDisableAutoStop = value; }
        }

        #endregion

        public RfidReader()
        {
            ObjReadTags = new TAGMR.ReadTags();
            ObjReadTags.CallBackFunction = new TAGMR.readsDelegate(displayReads);

            InitializeComponent();

            mWidthButton = BtnStart.Width;
            mWidthRfidCount = lblRfidCount.Width;
            SetControl();
            lblMessageDisplay.Text = string.Empty;
        }

        public new event EventHandler StopClick
        {
            add
            {
                BtnStop.Click += value;
            }
            remove
            {
                BtnStop.Click -= value;
            }
        }

        public new event EventHandler StartClick
        {
            add
            {
                BtnStart.Click += value;
            }
            remove
            {
                BtnStart.Click -= value;
            }
        }

        public new event EventHandler CurrentRfidTag_Change
        {
            add
            {
                txtCurrentRfidTag.TextChanged += value;
            }
            remove
            {
                txtCurrentRfidTag.TextChanged -= value;
            }
        }

        #region Start Stop Events

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                SpaceCodeIPList = "127.0.0.1:8080";
                txtRfidTagList.Text = string.Empty; lblRfidCount.Text = string.Empty;
                if (!RadOldRfid.Checked
                    //&& !RadNewRfid.Checked 
                    && !RadIpRfid.Checked
                    && !RadEth.Checked)
                {
                    Val.Message("Select Rfid To Scan Device");
                    return;
                }

                if (RadOldRfid.Checked)
                {
                    if (string.IsNullOrEmpty(RFIDIPAddress))
                    {
                        lblMessageDisplay.Text = "Old RFID Ip Not Config.";
                    }
                    else
                    {
                        StartStopEnable("S");
                        ObjReadTags.startReading(RFIDIPAddress.Trim(), "0");
                        lblMessageDisplay.Text = "Start Reading RFID";
                        numTags = 0;
                    }
                }
                //else if (RadNewRfid.Checked)
                //{
                    //AttachRfidDevice();
                    //StartDevice();
                    //lblMessageDisplay.Text = "Attach And Start Device";
                    //if (device != null)
                    //{
                    //    lblRfidCount.Clear();
                    //    txtRfidTagList.Clear();
                    //    Thread.Sleep(3000);
                    //    if ((device.ConnectionStatus == ConnectionStatus.CS_Connected) && (device.DeviceStatus == DeviceStatus.DS_Ready))
                    //    {
                    //        device.ScanDevice();
                    //    }
                    //    else
                    //    {
                    //        RfidReader_Disposed(null, null);
                    //        lblMessageDisplay.Text = "Device Not Ready Or Not Connected";
                    //    }
                    //}

                //}
                else if (RadIpRfid.Checked)
                {
                    StartStopEnable("S");
                    ScanIpDevice();
                }
                else if (RadEth.Checked)
                {
                    if (string.IsNullOrEmpty(SpaceCodeIPList))
                    {
                        Val.Message("Rfid Ip Address Not Config.");
                        return;
                    }
                    v.lblRfidAddress = lbladdress;
                    v.FindDevice(SpaceCodeIPList);
                    StartStopEnable("S");

                    v.getCountDel = contNewRFID;
                    v.lblRfidCount = lblRfidCount;
                    v.BlnDisableAutoStop = mBlnDisableAutoStop;
                    v.txtCurrentTag = txtCurrentRfidTag;
                    v.stopBtn = BtnStop;
                    v.stopLedBtn = btnLedOff;
                    v.ScanDevice();
                    
                }
                else
                {
                    Val.Message("No Rfid Found");
                    return;
                }
            }
            catch (Exception Ex)
            {
                lblMessageDisplay.Text = Ex.Message;
                StartStopEnable("T");
            }
        }

        private void contNewRFID(string count)
        {
            this.Invoke((MethodInvoker)delegate
            {
                lblRfidCount.Text = count;
            });
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            try
            {
                ObjReadTags.stopReading();

                //if (sc != null)
                //{
                //    for (int IntRfid = 0; IntRfid < sc.Length; IntRfid++)
                //    {
                //        if (sc[IntRfid] != null)
                //        {
                //            sc[IntRfid].StopScan();
                //            sc[IntRfid] = null;
                //        }
                //    }

                //    ArrSpaceCodeIP = null;
                //    sc = null;
                //}

                if (RadEth.Checked)
                {
                    //GET data from new RFID 
                    v.lblRfidCount = null;
                    txtRfidTagList.Text = String.Join(",", v.ScanResult.ToArray());
                    v.StopScanDevice();
                    //v.getCountDel = null;
                    //TCPFeatures.StopScan(_currentEthernetDevice, tcpClient);

                    //if (ArrSpaceCodeIP != null)
                    //{
                    //    for (int IntIPCheck = 0; IntIPCheck < mSpaceCodeIPList.Split(',').Length; IntIPCheck++)
                    //    {
                    //        if (!TryParseIpPort(ArrSpaceCodeIP[IntIPCheck].ToString(), mRfidPortNumber))
                    //            continue;

                    //        //if (deviceInfo[IntIPCheck] != null && tcpClient != null)
                    //        //{
                    //        //    objTCPFeatures.StopScan(deviceInfo[IntIPCheck], tcpClient);
                    //        //}
                    //    }
                    //}
                }
                lblMessageDisplay.Text = string.Empty;
                myTimeRfidTagCounter.Stop();
                myTimeRfidTagCounter.Enabled = false;

                StartStopEnable("T");
            }
            catch (Exception Ex)
            {
                lblMessageDisplay.Text = Ex.Message;
                Val.Message(Ex.Message);

                StartStopEnable("T");
            }
        }

        #endregion

        #region Old Rfid Code

        public void displayReads(String tagReads)
        {
            txtRfidTagList.Invoke((MethodInvoker)delegate
            {
                txtRfidTagList.Text += tagReads.ToUpper();
            });
            lblMessageDisplay.Invoke((MethodInvoker)delegate { lblMessageDisplay.Text = "Rfid Tag Reading..."; });
        }

        #endregion

        #region IP Rfid Scanning

        private void StartStopEnable(string pEvent)
        {
            if (pEvent.Equals("S"))
            {
                BtnStart.Enabled = false;
                BtnStop.Enabled = true;
                BtnStop.ForeColor = System.Drawing.Color.Red;
                //RadNewRfid.Enabled = false;
                RadIpRfid.Enabled = false;
                RadOldRfid.Enabled = false;
            }
            else
            {
                BtnStart.Enabled = true;
                BtnStop.Enabled = false;
                BtnStart.ForeColor = System.Drawing.Color.Navy;
                //RadNewRfid.Enabled = false;
                RadIpRfid.Enabled = true;
                RadOldRfid.Enabled = true;
            }
        }

        private void ScanIpDevice()
        {
            if (string.IsNullOrEmpty(mSpaceCodeIPList))
            {
                lblMessageDisplay.Text = "Ip Address Required To Scan";
                StartStopEnable("T");
                return;
            }
            ArrSpaceCodeIP = null;
            //sc = null;
            ArrSpaceCodeIP = mSpaceCodeIPList.Split(',');
            //sc = new IpScanner[ArrSpaceCodeIP.Length];

            myTimeRfidTagCounter.Tick += new EventHandler(myTimeRfidTagCouner_Tick);
            myTimeRfidTagCounter.Interval = 1000;
            myTimeRfidTagCounter.Enabled = true;
            myTimeRfidTagCounter.Start();

            for (int IntIPCheck = 0; IntIPCheck < mSpaceCodeIPList.Split(',').Length; IntIPCheck++)
            {
                //if (IpScanner.IsValidIp(ArrSpaceCodeIP[IntIPCheck].ToString()) == false)
                //{
                //    lblMessageDisplay.Invoke((MethodInvoker)delegate { lblMessageDisplay.Text = mSpaceCodeIPList[IntIPCheck].ToString() + " Ip Is Invalid."; });
                //    return;
                //}

                //sc = new IpScanner[IntIPCheck];
                //sc[IntIPCheck] = new IpScanner();
                //sc[IntIPCheck].lblMessageDisplay = lblMessageDisplay;

                //Thread mySpaceCode = new Thread(new ParameterizedThreadStart(ScanningRfid));
                //mySpaceCode.Start(IntIPCheck);
                //ScanningRfid(IntIPCheck);
            }
            lblMessageDisplay.Invoke((MethodInvoker)delegate { lblMessageDisplay.Text = "Rfid Tag Reading Started."; });
        }

        private void ScanEtherNetDevice()
        {
            ArrSpaceCodeIP = null;
            deviceInfo = null;
            ArrSpaceCodeIP = mSpaceCodeIPList.Split(',');
            deviceInfo = new DeviceInfo[ArrSpaceCodeIP.Length];
            for (int IntIPCheck = 0; IntIPCheck < mSpaceCodeIPList.Split(',').Length; IntIPCheck++)
            {
                if (!TryParseIpPort(ArrSpaceCodeIP[IntIPCheck].ToString(), mRfidPortNumber))
                    continue;

                Thread mySpaceCode = new Thread(new ParameterizedThreadStart(ScanningEtherNetRfid));
                mySpaceCode.Start(IntIPCheck);
                //ScanningRfid(IntIPCheck);
            }
        }
        DeviceInfo[] deviceInfo = null;
        TcpIpClient tcpClient = new TcpIpClient();
        //TCPFeatures objTCPFeatures = new TCPFeatures();
        private void ScanningEtherNetRfid(object pIntIndex)
        {
            if (!TryParseIpPort(ArrSpaceCodeIP[(int)pIntIndex].ToString(), mRfidPortNumber)) return;

            deviceInfo[(int)pIntIndex] = new DeviceInfo();
            deviceInfo[(int)pIntIndex].IP_Server = ArrSpaceCodeIP[(int)pIntIndex].ToString();
            deviceInfo[(int)pIntIndex].Port_Server = mRfidPortNumber;

            //InventoryData lastScanInventory;
            //if (objTCPFeatures.StartScan(deviceInfo[(int)pIntIndex], tcpClient, out lastScanInventory) != TcpIpClient.RetCode.RC_Succeed)
            //{
            //    UpdateStatusBar("Ethernet Device scanning has failed");
            //    return;
            //}

            UpdateStatusBar("Ethernet Device scanning completed.");
            //Invoke((MethodInvoker)delegate
            //{
            //    foreach (string tagUID in lastScanInventory.listTagAll)
            //        txtRfidTagList.Text += tagUID + ",";
            //});
        }

        private bool TryParseIpPort(string givenIp, int portNumber)
        {
            if (String.IsNullOrEmpty(givenIp) || String.IsNullOrEmpty(portNumber.ToString()))
            {
                UpdateStatusBar("Please fill IP Address and Port textfields.");
                return false;
            }

            IPAddress ipAddress;

            if (!IPAddress.TryParse(givenIp, out ipAddress)) // check if IP address is valid
            {
                UpdateStatusBar("IP Address Not Valid.");
                return false;
            }

            if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) // check if IP is IPv4
            {
                UpdateStatusBar("Not Valid IPv4 address.");
                return false;
            }
            return true;
        }

        private void UpdateStatusBar(string pMsgDisplay)
        {
            lblMessageDisplay.Invoke((MethodInvoker)delegate { lblMessageDisplay.Text = pMsgDisplay; });
        }

        //private void ScanningRfid(object pRfidIndex)
        //{
            //if (ArrSpaceCodeIP[(int)pRfidIndex] != null && sc[(int)pRfidIndex] != null)
            //{
            //    //sc = new IpScanner();
            //    if (sc[(int)pRfidIndex].GetScan((string)ArrSpaceCodeIP[(int)pRfidIndex].ToString()))
            //    {
            //        //lblMessageDisplay.Invoke((MethodInvoker)delegate { lblMessageDisplay.Text = "Rfid Tag Reading..."; });
            //        sc[(int)pRfidIndex].RemoteScan();
            //        sc[(int)pRfidIndex].NotifyRemoteResponse += new NotifyRemoteResponseDelegate(ReomteResponceArrived);
            //    }
            //}
            //_wait.Set();
        //}

        //private static ManualResetEvent _wait = new ManualResetEvent(false);
        System.Windows.Forms.Timer myTimeRfidTagCounter = new System.Windows.Forms.Timer();

        private void myTimeRfidTagCouner_Tick(object sender, EventArgs e)
        {
            GetRfidStatus();
        }

        private void GetRfidStatus()
        {
            if (this.ParentForm == null)
            {
                myTimeRfidTagCounter.Enabled = false;
                this.Dispose();
                return;
            }
            bool BlnInScan = false;
            //if (sc == null)
            //{
            //    if (lblMessageDisplay.IsDisposed)
            //        return;
            //    lblMessageDisplay.Invoke((MethodInvoker)delegate { lblMessageDisplay.Text = "Rfid Machine Not Found"; });
            //    return;
            //}
            //for (int IntCheck = 0; IntCheck < sc.Length; IntCheck++)
            //{
            //    BlnInScan = sc[IntCheck].InScan;
            //    if (BlnInScan) break;
            //}
            if (BlnInScan)
            {
                if (lblMessageDisplay.IsDisposed)
                    return;
                lblMessageDisplay.Invoke((MethodInvoker)delegate { lblMessageDisplay.Text = "Rfid Tag Scanning...."; });
            }
            else
            {
                if (lblMessageDisplay.IsDisposed)
                    return;
                lblMessageDisplay.Invoke((MethodInvoker)delegate { lblMessageDisplay.Text = "Rfid Tag Reading Completed."; });
                //myTimeRfidTagCouner.Stop();
            }
        }

        //private void ReomteResponceArrived(Object sender, string msg)
        //{
            //IpScanner a = (IpScanner)sender;
            //if (a.InScan == false)
            //{
            //    //GetRfidStatus();
            //    lblMessageDisplay.Invoke((MethodInvoker)delegate
            //    {
            //        if (a.ScanData != null)
            //        {
            //            if (a.ScanData.allTags != null)
            //            {
            //                for (int IntTagList = 0; IntTagList < a.ScanData.allTags.Count; IntTagList++)
            //                {
            //                    txtRfidTagList.Text += a.ScanData.allTags[IntTagList].ToString() + ",";
            //                }
            //            }
            //            else
            //            {
            //                //lblMessageDisplay.Text = msg;
            //            }
            //        }
            //        else
            //        {
            //            //lblMessageDisplay.Text = msg;
            //        }
            //    });


            //}
            //else if (a.InScan)
            //{
            //lblMessageDisplay.Invoke((MethodInvoker)delegate { lblMessageDisplay.Text = a.ScanData.allTags.Count.ToString(); });
            //}
        //}

        #endregion


        #region Events

        private void txtRfidTagList_TextChanged(object sender, EventArgs e)
        {
            lblRfidCount.Invoke((MethodInvoker)delegate
            {

                String[] strSplitter = txtRfidTagList.Text.Split(',');
                //numTags = strSplitter.GetUpperBound(0);
                ///--temp comment by riki for rfid count wrong in new.
                if (RadEth.Checked)
                {
                    numTags = strSplitter.Length;
                }
                if (RadOldRfid.Checked)
                {
                    numTags = strSplitter.GetUpperBound(0);
                }
                lblRfidCount.Text = numTags.ToString();
            });
        }

        private void lblMessageDisplay_SizeChanged(object sender, EventArgs e)
        {
            //if (MessageDisplayPosition == MessagePosition.Right)
            //{
            //    this.Width = lblMessageDisplay.Right + 3;
            //}
        }

        private void lblRfidCount_DoubleClick(object sender, EventArgs e)
        {
            Clear();
        }

        public void Clear()
        {
            lblRfidCount.Clear();
            txtRfidTagList.Clear();
            mRfidTagList = string.Empty;
        }

        private void RfidReader_Load(object sender, EventArgs e)
        {
            //AttachRfidDevice();
        }

        private void RfidCheckedChanged(object sender, EventArgs e)
        {
            txtRfidTagList.Text = string.Empty;
            lblRfidCount.Text = string.Empty;
        }

        #endregion

        private void rfidTagListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtRfidTagList.Visible = true;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void RfidReader_Click(object sender, EventArgs e)
        {
            txtRfidTagList.Visible = false;
        }

        private void RadEth_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            v.bUnlockAll = (chkSpeedRead.Checked) ? false : true;
        }

        public void releaseDevice()
        {
            if (v != null)
            {
                v.ReleaseDevice();
            }
        }

        public void setLedtags(List<string> tags)
        {
            v.setLedtags(tags);
        }

        public void setLedtags(string tags)
        {
            v.setLedtags(tags);
        }

        public void setTagDelegate(msgDel method)
        {
            v.TagEvent += method;
        }

        public void removeTagDelegate(msgDel method)
        {
            v.TagEvent -= method;
        }

        private void btnLedOff_Click(object sender, EventArgs e)
        {
            v.stopLed();
        }
    }
}
