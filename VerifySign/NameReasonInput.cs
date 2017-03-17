using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VerifySign
{
    public partial class NameReasonInput: Form
    {
        private SignatureDatabase _sigDb;
        public string SignName;
        public string SignEmail;
        public string SignReason;

        public NameReasonInput()
        {
            InitializeComponent();
        }
        
        public DialogResult ShowDialog(SignatureDatabase sigDb, string name, string email, string reason)
        {
            _sigDb = sigDb;
            if (_sigDb == null)
            {
                MessageBox.Show("Signature database is empty");
                return this.ShowDialog();
            }
            foreach(SignatureReference sigRef in _sigDb.List)
            {
                cboName.Items.Add(sigRef.Name);
            }
            if(cboName.Items.Count > 0)
            {
                cboName.SelectedIndex = 0;
                lblEmail.Text = _sigDb.List[cboName.SelectedIndex].Email;
            }

            if(name != null && email != null)
            {
                for(int i=0; i<_sigDb.List.Length; i++)
                {
                    SignatureReference sigRef = _sigDb.List[i];
                    if(sigRef.Name == name && sigRef.Email == email)
                    {
                        cboName.SelectedIndex = i;
                        lblEmail.Text = email;
                    }
                }
            }

            if(reason != null)
            {
                txtReason.Text = reason;
            }

            return this.ShowDialog();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            SignName = null;
            SignReason = null;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SignName = cboName.Text;
            SignReason = txtReason.Text;
            SignEmail = lblEmail.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void txtReason_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)Keys.Return)
            {
                if(cboName.Text.Trim().Length > 0 && txtReason.Text.Trim().Length > 0)
                {
                    btnOK_Click(btnOK, null);
                }
            }
        }

        private void cboName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboName.Items.Count > 0)
            {
                lblEmail.Text = _sigDb.List[cboName.SelectedIndex].Email;
            }
        }
    }
}
