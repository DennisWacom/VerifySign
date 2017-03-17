using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Florentis;

namespace VerifySign
{
    public partial class SignatureCollectorForm : Form
    {
        public delegate bool CheckSignatureExist(string name, string email);
        public delegate bool SaveSignature(string name, string email, string sigText);

        public CheckSignatureExist SignatureExistDelegate;
        public SaveSignature SaveSignatureDelegate;

        public string SignName;
        public string SignEmail;
        public string SigText;

        public SignatureCollectorForm()
        {
            InitializeComponent();
        }
        
        public void ShowDialog(SignatureReference sigRef)
        {
            txtName.Text = sigRef.Name;
            txtEmail.Text = sigRef.Email;
            if(sigRef.SigText != null && sigRef.SigText.Length > 50)
            {
                txtSigText.Text = sigRef.SigText.Substring(0, 50) + "..";
            }
            
            txtName.Enabled = false;
            txtEmail.Enabled = false;
        }

        private void btnSign_Click(object sender, EventArgs e)
        {
            if(txtName.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please enter name");
                return;
            }

            if (txtEmail.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please enter email");
                return;
            }

            SigCtl sigCtl = new SigCtl();
            DynamicCapture dc = new DynamicCapture();
            DynamicCaptureResult res = dc.Capture(sigCtl, txtName.Text, "Reference", null, null);

            if (res == DynamicCaptureResult.DynCaptOK)
            {
                SigObj sigObj = (SigObj)sigCtl.Signature;

                
                try
                {
                    SigText = sigObj.RenderBitmap(null, 400, 200, "image/png", 0.5f, 0xff0000, 0xffffff, 10.0f, 10.0f, RBFlags.RenderOutputBase64 | RBFlags.RenderColor32BPP | RBFlags.RenderEncodeData);
                    txtSigText.Text = SigText.Substring(0, 50) + "..";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (txtName.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please enter name");
                return;
            }

            if (txtEmail.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please enter email");
                return;
            }

            if(txtSigText.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please sign");
                return;
            }

            bool sigExist = false;
            if (SignatureExistDelegate != null)
            {
                sigExist = SignatureExistDelegate(txtName.Text, txtEmail.Text);
            }
            else
            {
                MessageBox.Show("Check signature function missing");
                return;
            }

            if (sigExist)
            {
                if(MessageBox.Show("Overwrite existing signature?", "Signature Database", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    return;
                }
            }

            if (SaveSignatureDelegate != null)
            {
                bool result = SaveSignatureDelegate(txtName.Text, txtEmail.Text, SigText);
                if (result)
                {
                    MessageBox.Show("Save Success");
                    DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Unable to save signature reference");
                }
            }
            else
            {
                MessageBox.Show("Save signature function missing");
                return;
            }

        }
    }
}
