using System;
using System.Linq;
using System.Windows.Forms;

namespace Controltest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            rfidReader2.setLedtags(ultraTextEditor2.Text);            
            //new Form2().Show();
        }

        private void rfidReader1_Load(object sender, EventArgs e)
        {
            
        }

        private void rfidReader2_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            rfidReader1.releaseDevice();
        }

        private void ultraButton1_Click(object sender, EventArgs e)
        {
            //ultraTextEditor1.Text = rfidReader1.txtRfidTagList.Text;
            
            
            rfidReader1.setLedtags(ultraTextEditor1.Text);
            txtResult.Text = rfidReader1.txtCurrentRfidTag.Text;
            //
            //rfidReader1.startLED("100715676058,100715676159,100715676649,100715676985,100715677127");
            //MessageBox.Show("led on");
            //rfidReader1.stopLed();
            //rfidReader1.setTagDelegate(tags);
        }



        private void tags(string tags)
        {
            this.Invoke((MethodInvoker)delegate
            {
                ultraTextEditor1.Text += tags + ",";                
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            rfidReader1.CurrentRfidTag_Change += txtResult_TextChanged;
            label1.Text = new Random().Next().ToString();
        }

        private void txtResult_TextChanged(object sender, EventArgs e)
        {
            ModCont.cTextBox x = (ModCont.cTextBox)sender;
            txtResult.Text +=  x .Text+",";
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void ultraTextEditor1_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
