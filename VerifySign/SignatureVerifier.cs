using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using wgssDSV;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Florentis;
using System.ComponentModel;
using System.Threading;

namespace VerifySign
{
    public class SignatureVerifier
    {
        public EventHandler VerificationCompleteEvent;
        
        float PASSING_SCORE = Properties.Settings.Default.PassingScore;
        string DsvLicense = "";
        bool DemoMode = Properties.Settings.Default.DemoMode;
        bool DemoResult = true;
        public SigObj SigRef = null;
        public SigObj SigTest = null;

        public enum Status { Pending, Completed, Error }
        public Status VerificationStatus;
        public bool VerificationPass;
        public CompareResult VerificationResult;
        public string ErrorMessage;
        
        public SignatureVerifier()
        {
            if (DemoMode) return;

            DsvLicense = readDSVLicence();
            if(DsvLicense == null)
            {
                MessageBox.Show("DSV license not found");
            }
            
        }

        private string readDSVLicence()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wacom\DSV");

            if (key != null)
            {
                return (string)key.GetValue("Licence");
            }
            return null;
        }

        private Options readOptions()
        {
            try
            {
                string asmDir = Path.GetDirectoryName(Application.StartupPath);
                string cfgFile = Path.Combine(Application.StartupPath, "dsv.cfg");
                using (StreamReader sr = new StreamReader(cfgFile))
                {
                    Options opt = new OptionsClass();
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine().Trim();
                        if (line != "")
                        {
                            string[] values = line.Split('=');

                            switch (values[0])
                            {
                                case "ncycles": opt.cycles = uint.Parse(values[1]); break;
                                case "nsteps": opt.steps = uint.Parse(values[1]); break;
                                case "nIterations": opt.iterations = int.Parse(values[1]); break;
                                case "temperature": opt.temperature = double.Parse(values[1]); break;
                                case "minTemperature": opt.minTemperature = double.Parse(values[1]); break;
                                case "reduction": opt.reduction = double.Parse(values[1]); break;
                                case "xpos": opt.weightingX = double.Parse(values[1]); break;
                                case "ypos": opt.weightingY = double.Parse(values[1]); break;
                                case "direction": opt.weightingDirection = double.Parse(values[1]); break;
                                case "curvature": opt.weightingCurvature = double.Parse(values[1]); break;
                                case "unlinked": opt.weightingUnlinked = double.Parse(values[1]); break;
                                case "removelink": opt.removeLink = double.Parse(values[1]); break;
                                case "linkRange": opt.linkRange = double.Parse(values[1]); break;

                                default:
                                    throw new Exception("Unknown option value");
                            }

                        }
                    }

                    return opt;

                }

            }
            catch (Exception)
            {

            }

            return null;
        }

        public CompareResult Verify(SigObj sigRef, SigObj sigTest)
        {
            try
            {
                wgssDSV.SignatureVerifier verifier = new wgssDSV.SignatureVerifier();
                wgssDSV.Options opt = new wgssDSV.Options();
                opt = readOptions();

                verifier.license = DsvLicense;
                verifier.setReferenceSignature(sigRef);
                verifier.setTestSignature(sigTest);

                return verifier.compare();
            }
            catch (Exception ex)
            {
                VerificationPass = false;
                VerificationResult = null;
                VerificationStatus = Status.Error;
                ErrorMessage = ex.Message;
                if (VerificationCompleteEvent != null)
                {
                    VerificationCompleteEvent(this, null);
                }
                return null;
            }

        }

        private void DoCompare(Object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (DemoMode)
            {
                e.Result = DemoResult;
            }
            else
            {
                try
                {
                    wgssDSV.SignatureVerifier verifier = new wgssDSV.SignatureVerifier();
                    wgssDSV.Options opt = (wgssDSV.Options)e.Argument;

                    verifier.license = DsvLicense;
                    verifier.setReferenceSignature(SigRef);
                    verifier.setTestSignature(SigTest);
                    
                    e.Result = verifier.compare();
                }
                catch (Exception ex)
                {
                    e.Result = ex;
                    VerificationPass = false;
                    VerificationResult = null;
                    VerificationStatus = Status.Error;
                    ErrorMessage = ex.Message;
                    if(VerificationCompleteEvent != null)
                    {
                        VerificationCompleteEvent(this, null);
                    }
                }
            }
            
        }

        private void DoneCompare(Object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if(e.Result.GetType() == typeof(CompareResultClass))
            {
                VerificationStatus = Status.Completed;
                VerificationResult = e.Result as CompareResult;
                Console.Write(VerificationResult.score);
                VerificationPass = VerificationResult.score > PASSING_SCORE ? true : false;
                ErrorMessage = "";
            }
            else if(e.Result.GetType() == typeof(bool))
            {
                VerificationStatus = Status.Completed;
                VerificationResult = null;
                VerificationPass = DemoResult;
            }
            else if(e.Result.GetType() == typeof(Exception))
            {
                VerificationStatus = Status.Error;
                VerificationResult = null;
                VerificationPass = false;
                Exception ex = e.Result as Exception;
                ErrorMessage = ex.Message;
            }
            else
            {
                VerificationStatus = Status.Error;
                VerificationResult = null;
                VerificationPass = false;
                ErrorMessage = "Unknown verification error";
            }

            if (VerificationCompleteEvent != null)
            {
                VerificationCompleteEvent(this, null);
            }

        }

        public void CompareSignatures(SigObj sigRef, SigObj sigTest)
        {
            try
            {
                wgssDSV.Options opt = new wgssDSV.Options();
                opt = readOptions();

                SigRef = sigRef;
                SigTest = sigTest;

                VerificationStatus = Status.Pending;
                VerificationResult = null;
                VerificationPass = false;
                ErrorMessage = null;
                
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(DoCompare);
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DoneCompare);

                worker.RunWorkerAsync(opt);
            }
            catch (Exception ex)
            {
                VerificationStatus = Status.Error;
                VerificationResult = null;
                VerificationPass = false;
                ErrorMessage = ex.Message;
                if (VerificationCompleteEvent != null)
                {
                    VerificationCompleteEvent(this, null);
                }
            }
        }

    }
    
}
