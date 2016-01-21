using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Controltest
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void ultraButton1_Click(object sender, EventArgs e)
        {
            rfidReader1.setLedtags(ultraTextEditor1.Text);
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            txtResult.Text = rfidReader1.txtCurrentRfidTag.Text;
        }

        private void txtResult_TextChanged(object sender, EventArgs e)
        {
            try
            {
                ModCont.cTextBox x = (ModCont.cTextBox)sender;
                txtResult.Text += x.Text + ",";
            }
            catch(Exception er) 
            { }
        }

        private void rfidReader1_Load(object sender, EventArgs e)
        {

        }
    }
}
