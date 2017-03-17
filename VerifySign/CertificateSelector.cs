using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace VerifySign
{
    public partial class CertificateSelector : Form
    {
        public string CertFolder = Path.Combine(Application.StartupPath, "Certificates");
        public string CertificatePath = "";
        public string CertificatePassPhrase = "";
        private string[] Certificates;

        public CertificateSelector()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (cboCertificate.Items.Count == 0) return;
            if (txtPassPhrase.Text.Trim().Length == 0) return;

            CertificatePath = Certificates[cboCertificate.SelectedIndex];
            CertificatePassPhrase = txtPassPhrase.Text;

            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void CertificateSelector_Load(object sender, EventArgs e)
        {
            txtCertFolder.Text = CertFolder;
            Certificates = Directory.GetFiles(CertFolder, "*.pfx");
            foreach(string cert in Certificates)
            {
                string certFileName = Path.GetFileName(cert);
                cboCertificate.Items.Add(certFileName);
            }
            if(Certificates.Length > 0)
            {
                cboCertificate.SelectedIndex = 0;
                string defaultCertPassword = Properties.Settings.Default.DefaultCertPassword;
                if(defaultCertPassword != null && defaultCertPassword != "")
                {
                    txtPassPhrase.Text = defaultCertPassword;
                }

                txtPassPhrase.Select();
            }

            
        }

        private void txtPassPhrase_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)Keys.Return)
            {
                if(txtPassPhrase.Text.Trim().Length > 0)
                {
                    btnOK_Click(btnOK, null);
                }
            }
        }
    }
}
