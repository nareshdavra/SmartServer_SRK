using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Val = BusLib.Validation.BOValidation;
using BusLib.Utility.Controls;
using System.Reflection;
//using Solitaire.Solitaire.DAYP;

namespace ControlLib
{
    public partial class UCtrlSendToProcess : UserControl
    {
        private BOSendToProcess ObjBOSendToProcess = new BOSendToProcess();
        //private FrmDAYPEntry frmDAYPEntry = new FrmDAYPEntry();

        #region Property Declaration

        private string mStoneIdList;
        /// <summary>
        /// StoneId List
        /// </summary>
        public string StoneIdList
        {
            get { return mStoneIdList; }
            set { mStoneIdList = value; }
        }

        private int mFormNumber = 0;

        #endregion

        public new event EventHandler SendToProcessClick
        {
            add
            {
                this.Click += value;
                BtnRCUT.Click += value;
            }
            remove
            {
                this.Click -= value;
                BtnRCUT.Click -= value;
            }
        }

        public UCtrlSendToProcess()
        {
            InitializeComponent();
        }

        private void BtnSendToProcess_Click(object sender, EventArgs e)
        {
            BtnSendToProcess.Anchor = AnchorStyles.Top;
            if (BtnSendToProcess.Tag.ToString().Equals("C"))
            {
                //this.Width = BtnSendToProcess.Width + 2;
                //this.Height = PnlSendToProcess.Bottom + 10;
                this.Size = this.MaximumSize;
                BtnSendToProcess.Tag = "O"; 
            }
            else
            {
                this.Size = this.MinimumSize;
                BtnSendToProcess.Tag = "C";
            }
            base.OnClick(e);
        }

        protected override void InitLayout()
        {
            BtnSendToProcess.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.MinimumSize = new Size(122, 29);
            this.MaximumSize = new Size(122, 259);
            base.InitLayout();
        }

        private bool ValSave()
        {
            if (string.IsNullOrEmpty(mStoneIdList))
            {
                Val.Message("Stone Id Selection Is Required");
                return false;
            }
            if (mStoneIdList.Split(',').Length <= 0)
            {
                Val.Message("Stone Id Selection Is Required");
                return false;
            }
            if (Val.Val(mFormNumber) == 0)
            {
                Val.Message("Form Is Not Registerd Or You Do Not Have Permission T");
                return false;
            }
            return true;
        }

        private void BtnDayp_Click(object sender, EventArgs e)
        {
            mStoneIdList = string.Empty;
            base.OnClick(e);
            if (ValSave() == false)
                return;

            ObjBOSendToProcess.CheckStoneID(mStoneIdList);

            if (ObjBOSendToProcess.DataSet.Tables[ObjBOSendToProcess.DaypStoneCheckDet].Rows.Count == 0)
            {
                Val.Message("No Data Found");
                return;
            }

            string RemainStoneList = string.Empty;
            string MainJanStoneList = string.Empty;

            foreach (DataRow DtRow in ObjBOSendToProcess.DataSet.Tables[ObjBOSendToProcess.DaypStoneCheckDet].Rows)
            {
                if (string.IsNullOrEmpty(DtRow["RESULT"].ToString()))
                {
                    if (!string.IsNullOrEmpty(MainJanStoneList)) MainJanStoneList += ",";
                    MainJanStoneList += DtRow["STONEID"].ToString();
                }
                else
                {
                    if (!string.IsNullOrEmpty(RemainStoneList)) RemainStoneList += ",";
                    RemainStoneList += DtRow["STONEID"].ToString();
                }
            }

            string MsgDisplay = string.Empty;
            if (!string.IsNullOrEmpty(MainJanStoneList))
            {
                if (ObjBOSendToProcess.SaveRecord(MainJanStoneList, mFormNumber) == -1)
                {
                    MsgDisplay += "Stone Not Added.";
                }
                else
                {
                    //MsgDisplay += "Jangad Create Successfully. ";
                }
            }

            if (!string.IsNullOrEmpty(RemainStoneList))
            {
                ObjBOSendToProcess.DataSet.Tables[ObjBOSendToProcess.DaypStoneCheckDet].DefaultView.RowFilter = " RESULT <> ''";
                if (ObjBOSendToProcess.DataSet.Tables[ObjBOSendToProcess.DaypStoneCheckDet].DefaultView.ToTable().Rows.Count != 0)
                {
                    //string FileName = BusLib.MyAccount.ClientInfo.MyApplicationPath + "\\StoneID_" + RemainStoneList + ".csv";
                    //string FileName = "D:\\" + "\\StoneID_" + RemainStoneList + ".csv";
                    string FileName = BusLib.MyAccount.ClientInfo.MyApplicationPath + "\\DaypEntry";
                    FileName += "_" + Val.DispDateTime(DateTime.Now.ToString()).Replace("/", "").Replace(":", "").Replace(" ", "_");
                    FileName += ".csv";
                    BusLib.Utility.BOCsvWriter.DataTableToCSV(ObjBOSendToProcess.DataSet.Tables[ObjBOSendToProcess.DaypStoneCheckDet].DefaultView.ToTable(), FileName, true);
                    MsgDisplay += "Remain Csv File Is Generated. Please Check Remain.Csv";
                }
                Val.Message(MsgDisplay);
            }
            mStoneIdList = string.Empty;
        }

        private void BtnViewRequest_Click(object sender, EventArgs e)
        {
            mStoneIdList = string.Empty;
            base.OnClick(e);
            if (ValSave() == false)
                return;
            ModCont.CButton ObjButton = (ModCont.CButton)sender;
            ObjBOSendToProcess.SavePrcMemo(mStoneIdList, ObjButton.Tag.ToString(), mFormNumber);
        }

        private void UCtrlSendToProcess_Click(object sender, EventArgs e)
        {
            if (mFormNumber == 0)
            {
                Form _mForm = (Form)this.ParentForm;
                mFormNumber = Val.ToInt(BusLib.Configuration.BOConfiguration.GetFormDetail(_mForm.Name, "FORMID"));
            }
        }
    }
}
