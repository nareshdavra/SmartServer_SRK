using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using log4net;
using TcpIP_class;

namespace ControlLib.Rfid
{
    public delegate void msgDel(string str);
    public class TcpV3
    {
        //public static readonly ILog logger = LogManager.GetLogger(typeof(TcpV3));

        public TcpV3() 
        {
            /*if (!log4net.LogManager.GetRepository().Configured)
            {
                var configFileDirectory = (new DirectoryInfo(Application.ExecutablePath)).Parent; // not the bin folder but up one level
                var configFile = new FileInfo(configFileDirectory.FullName + "\\log4net.config");

                if (!configFile.Exists)
                {
                    //throw new FileLoadException(String.Format("The configuration file {0} does not exist", configFile));
                }

                log4net.Config.XmlConfigurator.Configure(configFile);
            }*/
        }

        List<ArmClient> devList = new List<ArmClient>();
        private Dictionary<ArmClient, string[]> lightStartedList = new Dictionary<ArmClient, string[]>();


        public void FindDevice(String devicelist)
        {
            if (string.IsNullOrEmpty(devicelist))
            {
                return;
            }

            string[] StrIpList = devicelist.Split(',');

            for (int IntRfid = 0; IntRfid < StrIpList.Length; IntRfid++)
            {
                if (StrIpList[IntRfid].Split(':').Length != 2)
                {
                    return;
                }
                try
                {
                    ArmClient armCli = new ArmClient(StrIpList[IntRfid].Split(':')[0], Int32.Parse(StrIpList[IntRfid].Split(':')[1]),this);                    
                    devList.Add(armCli);
                }
                catch (Exception exp)
                {

                }
            }
        }

        public delegate void getCount(string s);
        public getCount getCountDel;

        public event msgDel TagEvent;

        public bool bUnlockAll = true;
        public List<string> lidghtList = new List<string>();

        public HashSet<string> scanResult = new HashSet<string>();

        public List<string> ScanResult
        {
            get { return scanResult.ToList<string>(); }
            //set { scanResult = value; }
        }

        private ModCont.cValueLabel mlblRfidAddress;
        public ModCont.cValueLabel lblRfidAddress
        {
            get { return mlblRfidCount; }
            set { mlblRfidCount = value; }
        }

        private ModCont.cValueLabel mlblRfidCount;
        public ModCont.cValueLabel lblRfidCount
        {
            get { return mlblRfidCount; }
            set { mlblRfidCount = value; }
        }

        private ModCont.cTextBox mtxtCurrentTag;
        public ModCont.cTextBox txtCurrentTag
        {
            get { return mtxtCurrentTag; }
            set { mtxtCurrentTag = value; }
        }

        private ModCont.CButton mstartBtn;
        private ModCont.CButton mstopBtn;

        public ModCont.CButton stopBtn
        {
            get { return mstopBtn; }
            set { mstopBtn = value; }
        }

        private ModCont.CButton mstopLedBtn;

        public ModCont.CButton stopLedBtn
        {
            get { return mstopLedBtn; }
            set { mstopLedBtn = value; }
        }

        private bool mBlnDisableAutoStop;
        /// <summary>
        /// To Disable Auto Stop Rfid Scan
        /// </summary>
        public bool BlnDisableAutoStop
        {
            get { return mBlnDisableAutoStop; }
            set { mBlnDisableAutoStop = value; }
        }


        private List<string> clistforSRK = new List<string>();

        public void ScanDevice()
        {
            mlblRfidCount.Text = "0";

            //logger.Info("Start Scan");
           
            foreach (ArmClient clt in devList)
            {
                clt.scanDevice(bUnlockAll);
            }
        }

        public void StopScanDevice()
        {
            this.scanResult.Clear();
            this.clistforSRK.Clear();
            //logger.Info("Stop Scan");
            foreach (ArmClient clt in devList)
            {
                clt.Dispose();
            }
        }

        public void ReleaseDevice()
        {
            try
            {
                this.scanResult.Clear();
                this.clistforSRK.Clear();
                foreach (ArmClient clt in devList)
                {
                    clt.Dispose();
                }                
            }
            catch (Exception wexer) {
                //logger.Error(wexer);
            }
        }

        public void setLedtags(List<string> tags)
        {
            string[] list = tags.ToArray();
            List<string> myNewTags = new List<string>();
            for (int i = 0; i < list.Length; i++) //string strtaglist in myNewTags)
            {
                if (list[i].Length > 9)
                {
                    myNewTags.Add(list[i]);
                }
            }
            this.lidghtList.Clear();
            this.lidghtList.AddRange(myNewTags);
            //logger.Info("LightList Set" + String.Join(",", tags));
        }

        public void setLedtags(string tags)
        {
            string[] list = tags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> myNewTags = new List<string>();
            for (int i =0 ; i<list.Length;i++) //string strtaglist in myNewTags)
            {
                if (list[i].Length > 9)
                {
                    myNewTags.Add(list[i]);
                }
            }
            this.lidghtList.Clear();
            this.lidghtList.AddRange(myNewTags);
        }

        public void foundLed(ArmClient client, string[] list)
        {
            if (this.stopLedBtn.Enabled == false) {
                stopLedBtn.Invoke((MethodInvoker)delegate
                    {
                        this.stopBtn.Enabled = false;
                        this.stopLedBtn.Enabled = true;
                    });
            }
            this.lightStartedList.Add(client, list);
            int LedOncount = 0; 
            foreach (ArmClient clt in lightStartedList.Keys)
            {
                LedOncount += lightStartedList[clt].Length;                
            }
            stopLedBtn.Invoke((MethodInvoker)delegate
            {
                this.stopLedBtn.Text = LedOncount + ":Off";
            });
            lidghtList = lidghtList.Except(list).ToList();
        }


        public void stopLed()
        {
            Dictionary<ArmClient, string[]> lightdList = new Dictionary<ArmClient, string[]>(lightStartedList);

            foreach (ArmClient clt in lightdList.Keys)
            {
                clt.stopLed();
                this.lightStartedList.Remove(clt);
            }

            stopBtn.Invoke((MethodInvoker)delegate
            {
                this.stopBtn.Enabled = true;
            });
            if (lidghtList != null && lidghtList.Count == 0)
            {
                stopBtn.Invoke((MethodInvoker)delegate
                {
                    stopBtn.PerformClick();
                });
                ReleaseDevice();
            }


            this.stopLedBtn.Text = "Off";
            this.stopLedBtn.Enabled = false;
        }

        public void countShow(String tag)
        {
            try
            {
                mlblRfidCount.Invoke((MethodInvoker)delegate
                {
                    if (tag != "" && !clistforSRK.Contains(tag))
                    {                               
                        mtxtCurrentTag.Text = tag ;
                    }
                    mlblRfidCount.Text = scanResult.Count.ToString();
                });
            }
            catch (Exception ere)
            {

            }
        }


        protected virtual void OnTagFound(String tag)
        {
            if (TagEvent != null)
            {
                TagEvent(tag);//Raise the event
            }
        }

    }
}
