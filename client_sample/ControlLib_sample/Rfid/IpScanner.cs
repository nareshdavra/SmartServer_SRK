using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Val = BusLib.Validation.BOValidation;
using ModCont;

namespace ControlLib.Rfid.MyControl
{
    public delegate void NotifyRemoteResponseDelegate(Object sender, string info);

    class IpScanner
    {
        System.Windows.Forms.Timer myTimeRfidTagCouner = new System.Windows.Forms.Timer();
        public event NotifyRemoteResponseDelegate NotifyRemoteResponse;
        public String ErrorMsg = "";

        public bool InScan = false;
        public enum Commands { SCAN = 0, WAITMODE = 1,LIGHT_TAG = 2 };

        private byte[] data = null;
        int bytesRead = 0;
        string MyIpAddress = string.Empty;
        public RfidData ScanData;
        TcpClient client;
        IPEndPoint serverEndPoint;

        NetworkStream clientStream = null;
        IAsyncResult ar;
        public cValueLabel lblMessageDisplay = new cValueLabel();

        public bool GetScan(String pMyIpAddress)
        {
            try
            {
                client = new TcpClient();
                serverEndPoint = new IPEndPoint(IPAddress.Parse(pMyIpAddress), 3000);
                client.Connect(serverEndPoint);
            }
            catch (Exception Ex)
            {
                return false;
            }
            return true;
        }

        public static bool IsValidIp(string pMyIpAddress)
        {
            IPAddress ip;
            bool valid = !string.IsNullOrEmpty(pMyIpAddress) && IPAddress.TryParse(pMyIpAddress, out ip);
            return valid;
        }
        
        public void RemoteScan()
        {
            try
            {

                NetworkStream clientStream = client.GetStream();
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes("SCAN");

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();

                //myTimeRfidTagCouner.Tick +=new EventHandler(myTimeRfidTagCouner_Tick);
                //myTimeRfidTagCouner.Interval = 100;
                //myTimeRfidTagCouner.Enabled = true;

                data = new byte[client.ReceiveBufferSize];

                Thread t = new Thread(new ParameterizedThreadStart(getScanData));
                t.Start(client);
                InScan = true;
            }
            catch (Exception exp)
            {
                Val.Message(exp.Message);
            }
        }

        //private void myTimeRfidTagCouner_Tick(object sender, EventArgs e)
        //{
            //lblMessageDisplay.Invoke((MethodInvoker)delegate { 
            //    if (ScanData != null)
            //        lblMessageDisplay.Text = ScanData.allTags.Count.ToString(); 
            //});
        //}

        private void getScanData(Object clt)
        {
            TcpClient tcpClient = (TcpClient)clt;
            clientStream = tcpClient.GetStream();
            
            try
            {
                bytesRead = clientStream.Read(data, 0, System.Convert.ToInt32(tcpClient.ReceiveBufferSize));
                RfidData list = null;

                list = (RfidData)new JavaScriptSerializer().Deserialize(System.Text.Encoding.ASCII.GetString(data, 0, bytesRead), typeof(RfidData));

                if (list != null)
                {
                    ScanData = list;
                }
            }
            catch (Exception e2)
            {
                ErrorMsg = System.Text.Encoding.ASCII.GetString(data, 0, bytesRead);
            }
            finally
            {
                InScan = false;
            }
            if (NotifyRemoteResponse != null) NotifyRemoteResponse(this, ErrorMsg);


        }

        public void StopScan()
        {
            if (clientStream != null)
            {
                clientStream.Flush();
                clientStream.Close();
                clientStream.Dispose();
            }
            InScan = false;
        }
    }
}
