using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DataClass;
using TcpIP_class;
using Val = BusLib.Validation.BOValidation;

namespace SBR2_Demo
{
    public class TCPFeatures
    {
        int scanIDLast;
        private bool stopScan = false;
        private static bool inScan = false;
        public TcpIpClient.RetCode StartScan(DeviceInfo ethernetDevice,TcpIpClient tcpClient , out InventoryData lastInventoryData)
        {
            stopScan = false;
            lastInventoryData = null; // used to store inventory (once the scan is over)

            if (ethernetDevice == null) return TcpIpClient.RetCode.RC_FailedToConnect;

            string status;

            // try to get device's status
            tcpClient.getStatus(ethernetDevice.IP_Server, ethernetDevice.Port_Server, ethernetDevice.SerialRFID, out status);
                //return TcpIpClient.RetCode.RC_FailedToConnect;  != TcpIpClient.RetCode.RC_Succeed)

            DeviceStatus currentStatus = (DeviceStatus)Enum.Parse(typeof(DeviceStatus), status);
            tcpClient.getLastScanID(ethernetDevice.IP_Server, ethernetDevice.Port_Server, ethernetDevice.SerialRFID, out scanIDLast);


            if (currentStatus == DeviceStatus.DS_InScan)
            {
                goto lable1;
            }

            // check that device is in ready state
            //if (currentStatus != DeviceStatus.DS_Ready)
            //{                
                //return TcpIpClient.RetCode.RC_Device_Not_In_Ready_State;
            //}

            if (tcpClient.requestScan(ethernetDevice.IP_Server, ethernetDevice.Port_Server, ethernetDevice.SerialRFID) != TcpIpClient.RetCode.RC_Succeed) // scan starting has failed
            { return TcpIpClient.RetCode.RC_Failed; }
            //inScan = true;


            lable1:
            int scanIDNew;
            do // will loop until Device doesn't leave "InScan" state
            {
                if (stopScan)
                {                    
                    break;
                }
                Thread.Sleep(500); // wait 500ms before polling device (to get scan result, if scan is over)
                tcpClient.getLastScanID(ethernetDevice.IP_Server, ethernetDevice.Port_Server, ethernetDevice.SerialRFID, out scanIDNew);

                // try to get device's status
                /*if (tcpClient.getStatus(ethernetDevice.IP_Server, ethernetDevice.Port_Server,
                        ethernetDevice.SerialRFID, out status) != TcpIpClient.RetCode.RC_Succeed)
                    return TcpIpClient.RetCode.RC_FailedToConnect;

                currentStatus = (DeviceStatus)Enum.Parse(typeof(DeviceStatus), status);*/

            } while (scanIDNew == scanIDLast);


            //inScan = false;
            if (stopScan)
            { stopScan = false; }
            else
            {if (tcpClient.requestGetLastScan(ethernetDevice.IP_Server,
                        ethernetDevice.Port_Server,
                        ethernetDevice.SerialRFID, out lastInventoryData) != TcpIpClient.RetCode.RC_Succeed) // failed to get last inventorydata
                return TcpIpClient.RetCode.RC_FailedToConnect;
            }
            return lastInventoryData == null ? TcpIpClient.RetCode.RC_UnknownError : TcpIpClient.RetCode.RC_Succeed;
        }


        public TcpIpClient.RetCode StopScan(DeviceInfo ethernetDevice, TcpIpClient tcpClient)
        {
            stopScan = true;
            return TcpIpClient.RetCode.RC_Succeed;
            //if (ethernetDevice == null) return TcpIpClient.RetCode.RC_FailedToConnect;
            //return tcpClient.requestStopScan(ethernetDevice.IP_Server, ethernetDevice.Port_Server, ethernetDevice.SerialRFID);
        }


        public void LedOnAll(DeviceInfo ethernetDevice, TcpIpClient tcpClient, List<string> tagsList)
        {
            if (ethernetDevice == null) return;
           
            int nbTagToLight = tagsList.Count; // initial number of tags

            TcpIpClient.RetCode ret;
            ret = tcpClient.RequestStartLighting(ethernetDevice.IP_Server, ethernetDevice.Port_Server, tagsList);
            if( ret != TcpIpClient.RetCode.RC_Succeed)
            {
                Val.Message("An unexpected error occurred during communication with device : " + ret.ToString());
                tcpClient.RequestStopLighting(ethernetDevice.IP_Server, ethernetDevice.Port_Server);
                return;
            }

            StringBuilder resultMessage = new StringBuilder(String.Format("{0} tags to find : {1} have been found.", nbTagToLight,
                nbTagToLight - tagsList.Count));

            if (tagsList.Count > 0) // some tag UIDs are still in the list : they've not been found
            {
                resultMessage.AppendLine("Missing tags ID :");

                foreach (string missingTag in tagsList)
                    resultMessage.AppendLine(missingTag);
            }

            Val.Message(resultMessage.ToString());

            tcpClient.RequestStopLighting(ethernetDevice.IP_Server, ethernetDevice.Port_Server);
        }


        public WriteCode WriteNewUID(DeviceInfo ethernetDevice, TcpIpClient tcpClient,string oldUID, string newUID , int writeMode)
        {
            WriteCode result;
            tcpClient.RequestWriteBlock(ethernetDevice.IP_Server, ethernetDevice.Port_Server, oldUID, newUID, out result, writeMode);
            return result;
        }
    }
}
