using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf.interfaces;
using iTextSharp.text.io;
using iTextSharp.text.pdf.security;
using Florentis;
using PdfiumViewer;
using wgssDSV;
using System.Threading;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace VerifySign
{
    public partial class PDFViewer : Form
    {
        public PdfReader reader;
        public PdfStamper stamper;
        public string defaultPdf = "Lorem Ipsum.pdf";   
        public string originalFile = "";
        public string currentFile = "";
        private Dictionary<string, string> pdfInfo;
        private SignatureDatabase sigDb;
        private bool DemoMode = Properties.Settings.Default.DemoMode;

        const string ACTION_APPROVE = "Approve";
        const string ACTION_VERIFY = "Verify";
        const string FIELD_APPROVE = "Approval";
        const string FIELD_VERIFY = "Verification";

        string Action = null;
        string ActionSignName = null;
        string ActionSignEmail = null;

        public PDFViewer()
        {
            InitializeComponent();
            sigDb = SignatureDatabaseForm.LoadSignatureDatabase(SignatureDatabaseForm.sigRefPath);
        }

        public DialogResult ApprovePdf(string filename, string signName, string signEmail)
        {
            Action = ACTION_APPROVE;
            ActionSignName = signName;
            ActionSignEmail = signEmail;
            approveToolStripMenuItem.Enabled = true;
            verifyToolStripMenuItem.Enabled = false;
            doneToolStripMenuItem.Visible = true;

            fileInfoToolStripMenuItem.Enabled = false;
            openToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Enabled = false;

            loadPdf(filename);

            return this.ShowDialog();
        }

        public DialogResult VerifyPdf(string filename, string signName, string signEmail)
        {
            Action = ACTION_VERIFY;
            ActionSignName = signName;
            ActionSignEmail = signEmail;
            approveToolStripMenuItem.Enabled = false;
            verifyToolStripMenuItem.Enabled = true;
            doneToolStripMenuItem.Visible = true;

            fileInfoToolStripMenuItem.Enabled = false;
            openToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Enabled = false;

            loadPdf(filename);

            return this.ShowDialog(); 
        }

        private void loadDefaultPDF()
        {
            loadPdf(defaultPdf);
        }

        private void loadPdf(string pdfPath)
        {
            loadPdf(pdfPath, true);
        }

        private void loadPdf(string pdfPath, bool changeOriginal)
        {
            PdfiumViewer.PdfDocument pdfiumDoc = PdfiumViewer.PdfDocument.Load(pdfPath);
            pdfRenderer1.Load(pdfiumDoc);
            pdfRenderer1.Show();
            
            currentFile = pdfPath;
            if (changeOriginal)
            {
                originalFile = pdfPath;
            }
            
            this.Text = "VerifySign - " + originalFile;

            reader = new PdfReader(pdfPath);

        }

        private void loadEncryptedPdf()
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //loadDefaultPDF();
        }

        private void showFileInfo()
        {
            FileInfo fileInfo = new FileInfo();
            fileInfo.reader = reader;
            fileInfo.filePath = currentFile;
            fileInfo.saveFileInfoFn = saveFileInfo;
            fileInfo.ShowDialog();
        }

        public void saveFileInfo(Dictionary<string, string> info)
        {
            pdfInfo = info;
        }

        private void fileInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showFileInfo();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void OpenFile()
        {
            openFileDialog1.Filter = "PDF Documents | *.pdf";
            openFileDialog1.Multiselect = false;
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    loadPdf(openFileDialog1.FileName);
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("File not found");
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
        }

        public void save(string path)
        {
            FileStream fsOut = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            FileStream fsIn = new FileStream(currentFile, FileMode.Open, FileAccess.Read);
            fsIn.CopyTo(fsOut);

            fsIn.Close();
            fsOut.Close();
        }

        public void saveAs()
        {
            saveFileDialog1.Filter = "PDF Documents | *.pdf";
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                save(saveFileDialog1.FileName);
                loadPdf(saveFileDialog1.FileName, true);
            }
        }

        public byte[] RenderSignatureImageBytes(SigObj sigObj, Size size, float inkWidth, uint inkColor, uint backgroundColor)
        {
            try
            {
                byte[] signature = sigObj.RenderBitmap(null, size.Width, size.Height, "image/png", inkWidth, inkColor, backgroundColor, 10.0f, 10.0f, RBFlags.RenderOutputBinary | RBFlags.RenderColor32BPP | RBFlags.RenderEncodeData);
                return signature;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public SigObj CaptureSignature(string name, string reason)
        {
            
            SigCtl sigCtl = new SigCtl();
            DynamicCapture dc = new DynamicCapture();
            DynamicCaptureResult res = dc.Capture(sigCtl, name, reason, null, null);

            if (res == DynamicCaptureResult.DynCaptOK)
            {
                SigObj sigObj = (SigObj)sigCtl.Signature;
                return sigObj;
            }
            
            return null;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveAs();   
        }
        
        private SigObj GetRefSigFromDb(string name, string email)
        {
            if (sigDb == null) return null;

            foreach(SignatureReference sigRef in sigDb.List)
            {
                if(sigRef.Name == name && sigRef.Email == email)
                {
                    SigObj sigObj = new SigObj();
                    if(sigObj.ReadEncodedBitmap(sigRef.SigText) == ReadEncodedBitmapResult.ReadEncodedBitmapOK)
                    {
                        return sigObj;
                    }
                    else
                    {
                        MessageBox.Show("Unable to read signature from database, it may be corrupted");
                        return null;
                    }
                }
            }
            return null;
        }

        private void AddImage(string imagePath, Point location, float scale)
        {

        }

        private void Sign(string fieldName, int signaturePage, iTextSharp.text.Rectangle signatureLocation)
        {
            Sign(fieldName, signaturePage, signatureLocation, null, null, null);
        }
        
        private void Sign(string fieldName, int signaturePage, iTextSharp.text.Rectangle signatureLocation, string defaultSignName, string defaultSignEmail, string defaultSignReason)
        {
            CertificateSelector certSelector = new CertificateSelector();
            if (certSelector.ShowDialog() == DialogResult.Cancel) return;

            string certPath = certSelector.CertificatePath;
            string certPwd = certSelector.CertificatePassPhrase;

            NameReasonInput nameReasonInput = new NameReasonInput();
            DialogResult dialogResult = nameReasonInput.ShowDialog(sigDb, defaultSignName, defaultSignEmail, defaultSignReason);
            if (dialogResult == DialogResult.Cancel) return;
            string name = nameReasonInput.SignName;
            string reason = nameReasonInput.SignReason;
            string email = nameReasonInput.SignEmail;
            double dsvScore = 1.0;

            bool doVerification = true;

            SigObj sigRef = GetRefSigFromDb(name, email);
            if (sigRef == null)
            {
                DialogResult dr = MessageBox.Show("Reference signature not found in database, do you want to proceed without signature verification?", Application.ProductName, MessageBoxButtons.YesNo);
                if(dr == DialogResult.No)
                {
                    return;
                }
                else
                {
                    doVerification = false;
                }
            }

            SigObj sigTest = CaptureSignature(name, reason);
            if (sigTest == null)
            {
                MessageBox.Show("Signature Capture failed");
                return;
            }

            if (DemoMode) doVerification = false;

            if (doVerification)
            {
                SignatureVerifier verifier = new SignatureVerifier();
                CompareResult res = verifier.Verify(sigRef, sigTest);
                if(res == null)
                {
                    if (verifier.VerificationStatus == SignatureVerifier.Status.Error)
                    {
                        if (MessageBox.Show("Signature verification error - " + verifier.ErrorMessage + Environment.NewLine + "Do you want to continue?", Application.ProductName, MessageBoxButtons.YesNo) == DialogResult.No)
                        {
                            return;
                        }
                    }
                }

                SignatureCompareForm sigCompareForm = new SignatureCompareForm();
                dsvScore = res.score;
                if(sigCompareForm.ShowDialog(sigRef, sigTest, res.score) == DialogResult.Cancel)
                {
                    return;
                }

            }else
            {
                SignatureCompareForm sigCompareForm = new SignatureCompareForm();
                if (sigCompareForm.ShowDialog(sigRef, sigTest, 1, true) == DialogResult.Cancel)
                {
                    return;
                }
            }

            byte[] signature = RenderSignatureImageBytes(sigTest, new Size(400, 200), 0.5f, 0xff0000, 0xffffff);
            if (signature == null)
            {
                MessageBox.Show("Render signature fail");
                return;
            }

            string newFile = PdfSign(signature, certPath, certPwd, name, reason,
                    signatureLocation,
                    signaturePage,
                    fieldName, 
                    dsvScore
                );

            if (newFile == null)
            {
                MessageBox.Show("PDF signing procedure failed");
                return;
            }
            loadPdf(newFile, false);

        }
        
        private void signToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }
        
        private string PdfSign(byte[] signature, string certPath, string password, string name, string reason, iTextSharp.text.Rectangle signatureRect, int signaturePage, string fieldName, double dsvScore)
        {
            FileStream ksfs = null;
            Pkcs12Store pk12 = null;

            try
            {
                ksfs = new FileStream(certPath, FileMode.Open);
                pk12 = new Pkcs12Store(ksfs, password.ToCharArray());

                string alias = "";
                foreach (string al in pk12.Aliases)
                {
                    if (pk12.IsKeyEntry(al) && pk12.GetKey(al).Key.IsPrivate)
                    {
                        alias = al;
                        break;
                    }
                }

                Org.BouncyCastle.Pkcs.X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
                ICollection<X509Certificate> chain = new List<X509Certificate>();
                foreach (X509CertificateEntry c in ce)
                {
                    chain.Add(c.Certificate);
                }

                AsymmetricKeyEntry pk = pk12.GetKey(alias);
                RsaPrivateCrtKeyParameters parameters = pk.Key as RsaPrivateCrtKeyParameters;

                string tmpFile = System.IO.Path.GetTempFileName();

                FileStream fs = new FileStream(tmpFile, FileMode.Create);
                PdfStamper stamper = PdfStamper.CreateSignature(reader, fs, '\0');

                PdfContentByte cb = stamper.GetOverContent(signaturePage);
                Image integrityImage = Properties.Resources.integrity_red;
                if(dsvScore > Properties.Settings.Default.GreenThreshold)
                {
                    integrityImage = Properties.Resources.integrity;
                }
                else if(dsvScore > Properties.Settings.Default.YellowThreshold)
                {
                    integrityImage = Properties.Resources.integrity_yellow;
                }
                iTextSharp.text.Image imgIntegrity = iTextSharp.text.Image.GetInstance(integrityImage, ImageFormat.Png);
                imgIntegrity.SetAbsolutePosition(signatureRect.Left, signatureRect.Bottom - 20);
                imgIntegrity.ScalePercent(50.0f);
                cb.AddImage(imgIntegrity);

                iTextSharp.text.Image imgVerified = iTextSharp.text.Image.GetInstance(Properties.Resources.verified, ImageFormat.Png);
                imgVerified.SetAbsolutePosition(signatureRect.Left + 20, signatureRect.Bottom - 20);
                imgVerified.ScalePercent(50.0f);
                cb.AddImage(imgVerified);

                PdfSignatureAppearance appearance = stamper.SignatureAppearance;
                appearance.Reason = reason;
                
                //uncomment this portion only
                //appearance.SignatureGraphic = iTextSharp.text.Image.GetInstance(signature);
                //appearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.GRAPHIC_AND_DESCRIPTION;
                //appearance.SetVisibleSignature(new iTextSharp.text.Rectangle(40, 110, 240, 210), 1, "Signature");
                appearance.SetVisibleSignature(signatureRect, signaturePage, fieldName);
                //appearance.Certificate = chain[0]; to remain commented out

                /*
                PdfTemplate n2 = appearance.GetLayer(2);
                ColumnText ct = new ColumnText(n2);
                ct.SetSimpleColumn(n2.BoundingBox);
                string backgroundText = "Digitally signed by " + Properties.Settings.Default.DefaultName + "\nOn: " + appearance.SignDate.ToString() + "\nReason: " + appearance.Reason;
                iTextSharp.text.Paragraph paragraph = new iTextSharp.text.Paragraph(backgroundText);
                ct.AddElement(paragraph);
                ct.Go();
                */
                string backgroundText = "Digitally signed by " + name + "\nOn: " + appearance.SignDate.ToString() + "\nReason: " + appearance.Reason;
                appearance.Layer2Text = backgroundText;
                appearance.Image = iTextSharp.text.Image.GetInstance(signature);
                
                //appearance.ImageScale = 1;
                
                IExternalSignature pks = new PrivateKeySignature((ICipherParameters)parameters, DigestAlgorithms.SHA256);
                MakeSignature.SignDetached(appearance, pks, chain, null, null, null, 0, CryptoStandard.CADES);

                ksfs.Close();

                //stamper.Close();
                //fs.Close();

                return tmpFile;

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
            
        }

        private void showSignatureInfo()
        {
            SignatureInfo info = new SignatureInfo();
            info.loadSignInfo(currentFile);
            info.ShowDialog();
        }

        private void signatureInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showSignatureInfo();
        }

        private void databaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SignatureDatabaseForm dbForm = new SignatureDatabaseForm();
            dbForm.ShowDialog(this);
        }

        public void updateSigDb(SignatureDatabase db)
        {
            if(db != null)
            {
                sigDb = db;
            }
        }

        private void approveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = null;
            string email = null;
            string reason = ACTION_APPROVE;

            if(ActionSignName != null && ActionSignEmail != null)
            {
                name = ActionSignName;
                email = ActionSignEmail;
            }
            else if(sigDb != null && sigDb.List != null && sigDb.List.Length > 0)
            {
                name = sigDb.List[0].Name;
                email = sigDb.List[0].Email;
            }

            Sign(FIELD_APPROVE, 1, new iTextSharp.text.Rectangle(40, 110, 240, 210), name, email, reason);
        }

        private void verifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = null;
            string email = null;
            string reason = ACTION_VERIFY;

            if(ActionSignName != null && ActionSignEmail != null)
            {
                name = ActionSignName;
                email = ActionSignEmail;
            }
            else if (sigDb != null && sigDb.List != null && sigDb.List.Length > 1)
            {
                name = sigDb.List[1].Name;
                email = sigDb.List[1].Email;
            }
            else if (sigDb != null && sigDb.List != null && sigDb.List.Length > 0)
            {
                name = sigDb.List[0].Name;
                email = sigDb.List[0].Email;
            }

            Sign(FIELD_VERIFY, 1, new iTextSharp.text.Rectangle(340, 110, 540, 210), name, email, reason);
        }

        private void doneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void PDFViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(e.CloseReason == CloseReason.UserClosing && Action != null && reader != null)
            {
                List<string> blankSignatureFields = reader.AcroFields.GetBlankSignatureNames();
                if(Action == ACTION_APPROVE)
                {
                    if(blankSignatureFields != null && blankSignatureFields.Contains(FIELD_APPROVE))
                    {
                        DialogResult = DialogResult.Cancel;
                    }
                    else
                    {
                        DialogResult = DialogResult.OK;
                    }   
                }
                else if(Action == "Verify")
                {
                    if (blankSignatureFields != null && blankSignatureFields.Contains(FIELD_VERIFY))
                    {
                        DialogResult = DialogResult.Cancel;
                    }
                    else
                    {
                        DialogResult = DialogResult.OK;
                    }
                }
                
                DialogResult = DialogResult.Cancel;
            }
        }
    }
}
