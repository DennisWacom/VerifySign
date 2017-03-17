using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace VerifySign
{
    public partial class WorkflowManager : Form
    {
        WebManager webManager;
        NotifyIcon ni;
        ContextMenuStrip cms;

        public delegate string ApproveDelegate(string name, string email, string filename);
        public delegate string VerifyDelegate(string name, string email, string filename);
        public ApproveDelegate ApproveFn;
        public VerifyDelegate VerifyFn;

        public delegate void LogDelegate(string msg, int alertType);
        public LogDelegate logfn;

        string docDir;

        public WorkflowManager()
        {
            InitializeComponent();
            this.ApproveFn = ApprovePdf;
            this.VerifyFn = VerifyPdf;

            ShowNotifyIcon();
            logfn = Log;

            string rootDir = Path.Combine(Application.StartupPath, "Portal");
            webManager = new WebManager(rootDir, Properties.Settings.Default.WebManagerPort, this);
            webManager.SendLog = WebManagerLog;
            webManager.Start();


            //string portal = Path.Combine(Application.StartupPath, "Portal\\index.html");
            string portal = "http://localhost:" + Properties.Settings.Default.WebManagerPort.ToString() + "/index.html";
            docDir = Path.Combine(Application.StartupPath, "Portal\\docs");

            Process.Start(portal);
        }

        protected void WebManagerLog(string msg, int alertType)
        {
            Log("[WebManager] " + msg, alertType);
        }

        protected void Log(string msg, int alertType)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(logfn, new object[] { msg, alertType});
            }
            else
            {
                txtLog.AppendText(msg + Environment.NewLine);
                txtLog.ScrollToCaret();
            }
            
        }
        
        protected void ShowNotifyIcon()
        {
            ni = new NotifyIcon();
            ni.Icon = Properties.Resources.blue_sign;
            ni.Text = "Signature Workflow";
            ni.MouseClick += Ni_MouseClick;
            ni.Visible = true;

            cms = new ContextMenuStrip();
            cms.Items.Add("Show Portal", null, ShowPortalClicked);
            cms.Items.Add("Exit", null, ExitClicked);

            ni.ContextMenuStrip = cms;
        }

        private void ShowPortalClicked(object sender, EventArgs args)
        {
            string portalFile = Path.Combine(Application.StartupPath, "Portal/index.html");
            Process.Start(portalFile);
        }

        private void ExitClicked(object sender, EventArgs args)
        {
            Exit();
        }

        private void Exit()
        {
            Application.Exit();
        }

        private void Ni_MouseClick(object sender, MouseEventArgs e)
        {

        }

        public string ApprovePdf(string signName, string signEmail, string filename)
        {
            //if (File.Exists(filename) == false) return null;

            PDFViewer pdfViewer = new PDFViewer();
            DialogResult result = pdfViewer.ApprovePdf(filename, signName, signEmail);
            if (result == DialogResult.OK)
            {
                try
                {
                    string newFilename = Path.GetFileNameWithoutExtension(filename) + "_approved" + Path.GetExtension(filename);
                    newFilename = Path.Combine(docDir, newFilename);
                    File.Copy(pdfViewer.currentFile, newFilename);
                    return newFilename;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public string VerifyPdf(string signName, string signEmail, string filename)
        {
            //if (File.Exists(filename) == false) return null;

            PDFViewer pdfViewer = new PDFViewer();
            DialogResult result = pdfViewer.VerifyPdf(filename, signName, signEmail);
            if (result == DialogResult.OK)
            {
                try
                {
                    string newFilename = Path.GetFileNameWithoutExtension(filename) + "_verified" + Path.GetExtension(filename);
                    newFilename = Path.Combine(docDir, newFilename);
                    File.Copy(filename, newFilename);
                    return newFilename;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private void WorkflowManager_Load(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
