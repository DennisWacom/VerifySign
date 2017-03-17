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
using Newtonsoft.Json;

namespace VerifySign
{
    public partial class SignatureDatabaseForm : Form
    {
        public SignatureDatabase sigDb = null;
        public List<SignatureReference> sigRefList = new List<SignatureReference>();
        public static string sigRefPath = Properties.Settings.Default.SignatureDatabase;

        public SignatureDatabaseForm()
        {
            InitializeComponent();
            
        }

        private ListViewItem CreateListViewItem(SignatureReference sigRef)
        {
            ListViewItem lvi = new ListViewItem();           
            lvi.SubItems.Add(sigRef.Name);
            lvi.SubItems.Add(sigRef.Email);
            if(sigRef.SigText != null && sigRef.SigText.Length > 50)
            {
                lvi.SubItems.Add(sigRef.SigText.Substring(0, 50) + "...");
            }
            else
            {
                lvi.SubItems.Add(" ");
            }
            
            return lvi;
        }

        private void LoadListView(List<SignatureReference> sigRefList)
        {
            if (sigRefList == null) return;
            foreach(SignatureReference sigRef in sigRefList)
            {
                listView1.Items.Add(CreateListViewItem(sigRef));
            }
        }

        public static SignatureDatabase LoadSignatureDatabase(string path)
        {
            if (CreateSigRefJsonFileIfNotExist() == false) return null;
            
            try
            {
                StreamReader reader = new StreamReader(path);
                string json = reader.ReadToEnd();
                reader.Dispose();
                
                SignatureDatabase db = JsonConvert.DeserializeObject<SignatureDatabase>(json);
                return db;
                
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to load signature database from " + path + "." + Environment.NewLine + ex.Message);
                return null;
            }
            
        }

        private static bool CreateSigRefJsonFileIfNotExist()
        {
            if (File.Exists(sigRefPath)) return true;

            try
            {
                FileStream fs = File.Create(sigRefPath);
                fs.Flush();
                fs.Close();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to create signature database at " + sigRefPath + "." + Environment.NewLine + ex.Message);
                return false;
            }   
        }

        private void UpdateListView(SignatureReference sig)
        {
            foreach(ListViewItem lvi in listView1.Items)
            {
                if(lvi.SubItems[0].Text == sig.Name && lvi.SubItems[1].Text == sig.Email)
                {
                    lvi.SubItems[2].Text = sig.SigText;
                    return;
                } 
            }
        }

        private void InsertIntoListView(SignatureReference sig)
        {
            listView1.Items.Add(CreateListViewItem(sig));
        }

        private bool SerializeSigRefListToFile()
        {
            try
            {
                sigDb.List = sigRefList.ToArray();
                string json = JsonConvert.SerializeObject(sigDb, Formatting.Indented);

                StreamWriter sw = new StreamWriter(sigRefPath, false, Encoding.UTF8);
                sw.Write(json);
                sw.Flush();
                sw.Close();
                return true;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to save signature list" + Environment.NewLine + ex.Message);
                return false;
            }
           
        }

        private bool SaveSignature(string name, string email, string sigText)
        {
            foreach (SignatureReference sigRef in sigRefList)
            {
                if (sigRef.Name == name && sigRef.Email == email)
                {
                    sigRef.SigText = sigText;
                    if (SerializeSigRefListToFile())
                    {
                        UpdateListView(sigRef);
                        listView1.Refresh();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                    
                }
            }
            SignatureReference sig = new SignatureReference();
            sig.Name = name;
            sig.Email = email;
            sig.SigText = sigText;
            sigRefList.Add(sig);
            if (SerializeSigRefListToFile())
            {
                InsertIntoListView(sig);
                listView1.Refresh();
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CheckSignatureExist(string name, string email)
        {
            foreach(SignatureReference sigRef in sigRefList)
            {
                if(sigRef.Name == name && sigRef.Email == email)
                {
                    return true;
                }
            }
            return false;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            SignatureCollectorForm collector = new SignatureCollectorForm();
            collector.SignatureExistDelegate = CheckSignatureExist;
            collector.SaveSignatureDelegate = SaveSignature;
            collector.ShowDialog();
        }

        private void SignatureDatabaseForm_Load(object sender, EventArgs e)
        {
            sigDb = LoadSignatureDatabase(sigRefPath);
            if (sigDb != null)
            {
                sigRefList = sigDb.List.ToList();
            }
            else
            {
                sigDb = new SignatureDatabase();
                sigRefList = new List<SignatureReference>();
            }

            LoadListView(sigRefList);
        }

        private void listView1_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    int selIndex = listView1.SelectedItems[0].Index;
                    sigRefList.RemoveAt(selIndex);
                    listView1.Items.RemoveAt(selIndex);
                    listView1.Refresh();
                }
            }
            else if(e.KeyCode == Keys.Enter)
            {
                if(listView1.SelectedItems.Count > 0)
                {
                    SignatureReference sigRef = sigRefList[listView1.SelectedItems[0].Index];
                    SignatureCollectorForm collector = new SignatureCollectorForm();
                    collector.SignatureExistDelegate = CheckSignatureExist;
                    collector.SaveSignatureDelegate = SaveSignature;
                    collector.ShowDialog(sigRef);
                }
            }
        }

        private void SignatureDatabaseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.Owner == null) return;
            if(this.Owner.GetType() == typeof(PDFViewer))
            {
                ((PDFViewer)Owner).updateSigDb(sigDb);
            }
        }
    }
}
