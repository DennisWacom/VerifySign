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
using System.IO;

namespace VerifySign
{
    public partial class SignatureCompareForm: Form
    {
        public SignatureCompareForm()
        {
            InitializeComponent();
        }

        public DialogResult ShowDialog(SigObj sigRef, SigObj sigTest, double score)
        {
            return ShowDialog(sigRef, sigTest, score, false);
        }

        public DialogResult ShowDialog(SigObj sigRef, SigObj sigTest, double score, bool demoMode)
        {
            string refFile = Path.GetTempFileName();
            sigRef.RenderBitmap(refFile, picSignRef.Width, picSignRef.Height, "image/png", 0.5f, 0xff0000, 0xffffff, 5.0f, 5.0f, RBFlags.RenderOutputFilename | RBFlags.RenderColor32BPP | RBFlags.RenderColorAntiAlias);
            picSignRef.ImageLocation = refFile;

            string testFile = Path.GetTempFileName();
            sigTest.RenderBitmap(testFile, picSignTest.Width, picSignRef.Height, "image/png", 0.5f, 0xff0000, 0xffffff, 5.0f, 5.0f, RBFlags.RenderOutputFilename | RBFlags.RenderColor32BPP | RBFlags.RenderColorAntiAlias);
            picSignTest.ImageLocation = testFile;

            if (demoMode)
            {
                lblScore.Text = "Score: " + score.ToString() + " (Demo Mode)";
            }
            else
            {
                lblScore.Text = "Score: " + score.ToString();
            }

            
            return this.ShowDialog();
        }

        private void btnReject_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
