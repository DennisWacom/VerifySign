using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace VerifySign
{
    public class WebManager
    {
        public delegate void LogDelegate(string msg, int alertType);
        public LogDelegate SendLog;

        public static string ACCESS_CONTROL_ALLOW_ORIGIN = "Access-Control-Allow-Origin";
        public static string ACCESS_CONTROL_ALLOW_CREDENTIALS = "Access-Control-Allow-Credentials";
        public static string ACCESS_CONTROL_EXPOSE_HEADERS = "Access-Control-Expose-Headers";

        public static string QUERY_NAME = "name";
        public static string QUERY_EMAIL = "email";
        public static string QUERY_FILE = "file";
        
        private const string ACTION_APPROVE = "approve";
        private const string ACTION_VERIFY = "verify";
        private const string ACTION_STATUS = "status";
        
        protected string _rootDirectory;
        protected int _port;
        protected WorkflowManager _workflowManager;
        

        private Thread _serverThread;
        private HttpListener _listener;
        //public HttpListenerContext currentContext;
        private bool _threadStarted = false;
        //private int _webManagerTimeout = 5000;

        private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
        #region extension to MIME type list
        {".asf", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".cco", "application/x-cocoa"},
        {".crt", "application/x-x509-ca-cert"},
        {".css", "text/css"},
        {".deb", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dll", "application/octet-stream"},
        {".dmg", "application/octet-stream"},
        {".ear", "application/java-archive"},
        {".eot", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".flv", "video/x-flv"},
        {".gif", "image/gif"},
        {".hqx", "application/mac-binhex40"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".iso", "application/octet-stream"},
        {".jar", "application/java-archive"},
        {".jardiff", "application/x-java-archive-diff"},
        {".jng", "image/x-jng"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".mml", "text/mathml"},
        {".mng", "video/x-mng"},
        {".mov", "video/quicktime"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".msi", "application/octet-stream"},
        {".msm", "application/octet-stream"},
        {".msp", "application/octet-stream"},
        {".pdb", "application/x-pilot"},
        {".pdf", "application/pdf"},
        {".pem", "application/x-x509-ca-cert"},
        {".pl", "application/x-perl"},
        {".pm", "application/x-perl"},
        {".png", "image/png"},
        {".prc", "application/x-pilot"},
        {".ra", "audio/x-realaudio"},
        {".rar", "application/x-rar-compressed"},
        {".rpm", "application/x-redhat-package-manager"},
        {".rss", "text/xml"},
        {".run", "application/x-makeself"},
        {".sea", "application/x-sea"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".swf", "application/x-shockwave-flash"},
        {".tcl", "application/x-tcl"},
        {".tk", "application/x-tcl"},
        {".txt", "text/plain"},
        {".war", "application/java-archive"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wmv", "video/x-ms-wmv"},
        {".xml", "text/xml"},
        {".xpi", "application/x-xpinstall"},
        {".zip", "application/zip"},
        #endregion
    };

        public bool Running
        {
            get { return _threadStarted; }
        }

        public WebManager(string rootDir, int port, WorkflowManager workflowManager)
        {
            _rootDirectory = rootDir;
            _port = port;
            _workflowManager = workflowManager;
        }

        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
            }
        }

        public string RootDirectory
        {
            get { return _rootDirectory; }
        }

        protected void Log(string msg)
        {
            Log(msg, 0);
        }

        protected void Log(string msg, int alertType)
        {
            if(SendLog != null)
            {
                SendLog(msg, alertType);
            }
        }

        public void Start()
        {
            Log("Start Webmanager");

            if(Properties.Settings.Default.PortRegistered == false)
            {
                Log("Register port " + Properties.Settings.Default.WebManagerPort.ToString());
                RegisterPort(Properties.Settings.Default.WebManagerPort);
                Properties.Settings.Default.PortRegistered = true;
                Properties.Settings.Default.Save();
            }

            try
            {
                _serverThread = new Thread(Listen);
                _serverThread.Start();
                _threadStarted = true;
                Log("WebManager started with root dir = " + _rootDirectory + " on Port " + _port.ToString());

            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        private void Listen()
        {
            //need to run "netsh http add urlacl url=http://*:8008/ user=Dennis listen=yes" with admin privilege
            //else calling Start() will prompt exception "Access Denied"
            Log("Listener initialised with prefix - http://*:" + _port.ToString() + "/");
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");

            try
            {
                _listener.Start();
            }
            catch (Exception ex)
            {
                Log(ex.Message, 1);
                _threadStarted = false;
            }

            try
            {
                _listener.BeginGetContext(Process, null);
                Log("BeginGetContext");
            }
            catch (Exception ex)
            {
                Log("Error getting context asynchronously");
                Log(ex.Message);
            }
            
        }

        private void ProcessApprove(HttpListenerContext context, string name, string email, string file)
        {
            Log("Approve PDF");
            
            try
            {
                file = Path.Combine(_rootDirectory, file);
                string doneFile = (string)_workflowManager.Invoke(_workflowManager.ApproveFn, new object[] { name, email, file });
                if(doneFile != null)
                {
                    byte[] fileBytes = Encoding.UTF8.GetBytes(doneFile);
                    sendResponse(context, fileBytes, "text/plain", HttpStatusCode.OK);
                }
                else
                {
                    byte[] fileBytes = Encoding.UTF8.GetBytes("");
                    sendResponse(context, fileBytes, "text/plain", HttpStatusCode.OK);
                }
            }
            catch (Exception)
            {
                Log("Error approving pdf", 1);
                sendErrorResponse(context, "Error approving");
            }
        }

        private void ProcessVerify(HttpListenerContext context, string name, string email, string file)
        {
            Log("Verify PDF");

            try
            {
                file = Path.Combine(_rootDirectory, file);
                string doneFile = (string)_workflowManager.Invoke(_workflowManager.VerifyFn, new object[] { name, email, file });
                if (doneFile != null)
                {
                    byte[] fileBytes = Encoding.UTF8.GetBytes(doneFile);
                    sendResponse(context, fileBytes, "text/plain", HttpStatusCode.OK);
                }
                else
                {
                    byte[] fileBytes = Encoding.UTF8.GetBytes("");
                    sendResponse(context, fileBytes, "text/plain", HttpStatusCode.OK);
                }
            }
            catch (Exception)
            {
                Log("Error verifying pdf", 1);
                sendErrorResponse(context, "Error verifiying");
            }
        }

        private void ProcessStatus(HttpListenerContext context, string file)
        {
            Log("Status File");

            try
            {
                string status = "File not found";
                
                string file_ext = Path.GetExtension(file);
                string filenameDir = Path.GetDirectoryName(file); //expecting docs
                string filenameWoDocDir = Path.GetFileNameWithoutExtension(file); //expecting DocumentA
                string approved_file = filenameWoDocDir + "_approved" + file_ext;
                approved_file = Path.Combine(_rootDirectory, filenameDir, approved_file);
                string verified_file = filenameWoDocDir + "_approved_verified" + file_ext;
                verified_file = Path.Combine(_rootDirectory, filenameDir, verified_file);
                string pending_file = Path.Combine(_rootDirectory, filenameDir, filenameWoDocDir + file_ext);

                if (File.Exists(verified_file))
                {
                    status = "Verified";
                }
                else if (File.Exists(approved_file))
                {
                    status = "Approved";
                }else if (File.Exists(pending_file))
                {
                    status = "Pending";
                }

                byte[] fileBytes = Encoding.UTF8.GetBytes(status);
                sendResponse(context, fileBytes, "text/plain", HttpStatusCode.OK);
                
            }
            catch (Exception)
            {
                Log("Error getting status of file", 1);
                sendErrorResponse(context, "Error getting status of file");
            }
        }

        private void Process(IAsyncResult asyncResult)
        {
            Log("End get context");
            HttpListenerContext context = _listener.EndGetContext(asyncResult);
            //_listener.BeginGetContext(Process, null);
            
            string filename = context.Request.Url.AbsolutePath;
            filename = filename.Substring(1);

            Log("Received request for " + filename);

            /*
            if (currentContext != null)
            {
                Thread.Sleep(_webManagerTimeout);
                currentContext = null;
            }
            currentContext = context;
            */

            Log("Processing request for url - " + filename + " from " + context.Request.RemoteEndPoint.Address + " on Port :" + context.Request.RemoteEndPoint.Port);
            
            NameValueCollection query = context.Request.QueryString;
            string name = query[QUERY_NAME];
            string email = query[QUERY_EMAIL];
            string file = query[QUERY_FILE];
            
            Dictionary<string, string> Variables = new Dictionary<string, string>();
            string[] keys = query.AllKeys;
            foreach(string key in keys)
            {
                if(key != null && key != QUERY_NAME && key != QUERY_EMAIL)
                {
                    Variables.Add(key, query[key]);
                }
            }
            
            if (filename.Equals(ACTION_APPROVE))
            {
                ProcessApprove(context, name, email, file);
            }
            else if (filename.Equals(ACTION_VERIFY))
            {
                ProcessVerify(context, name, email, file);
            }
            else if (filename.Equals(ACTION_STATUS))
            {
                ProcessStatus(context, file);
            }
            else if(File.Exists(Path.Combine(_rootDirectory, filename)))
            {
                string path = filename.Replace('/', '\\');
                path = Path.Combine(_rootDirectory, path);
                serveFile(context, path);
            }
            else
            {
                Log("Invalid request");
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.OutputStream.Close();
            }

            //currentContext = null;
            _listener.BeginGetContext(Process, null);
            Log("BeginGetContext");
        }
        
        private void serveFile(HttpListenerContext context, string filename)
        {
            Log("Serve file - " + filename);

            try
            {
                Stream input = new FileStream(filename, FileMode.Open);

                //Adding permanent http response headers
                string mime;
                context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                context.Response.ContentLength64 = input.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

                byte[] buffer = new byte[1024 * 16];
                int nbytes;
                while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, nbytes);
                input.Close();

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Flush();
            }
            catch (Exception)
            {
                Log("Exception serving file");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                sendErrorResponse(context, "Exception serving file");
            }
        }

        private void sendErrorResponse(HttpListenerContext context, string msg)
        {
            Log("Send error response - " + msg);
            byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
            sendResponse(context, msgBytes, "text/plain", HttpStatusCode.InternalServerError);
        }

        private void sendResponseServerBusy(HttpListenerContext context)
        {
            Log("Send error response - Server busy");
            byte[] msgBytes = Encoding.UTF8.GetBytes("Server Busy");
            sendResponse(context, msgBytes, "text/plain", HttpStatusCode.InternalServerError);
        }

        private void sendResponse(HttpListenerContext context, byte[] data, string mimeType, HttpStatusCode statusCode)
        {
            Log("Send response");
            try
            {
                context.Response.AddHeader(ACCESS_CONTROL_ALLOW_ORIGIN, "*");
                context.Response.AddHeader(ACCESS_CONTROL_ALLOW_CREDENTIALS, "true");

                context.Response.SendChunked = false;

                if (mimeType != null)
                {
                    context.Response.ContentType = mimeType;
                }

                if (data != null && data.Length > 0)
                {
                    context.Response.ContentLength64 = data.Length;

                    Stream ms = new MemoryStream(data);
                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = ms.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    ms.Close();
                }

                context.Response.StatusCode = (int)statusCode;
                context.Response.OutputStream.Flush();

                context.Response.OutputStream.Close();
            }
            catch(Exception ex)
            {
                Log("Exception during send response - " + ex.Message, 1);
            }
            
        }
        
        private void RegisterPort(int portNo)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.Verb = "runas";
            info.FileName = "cmd.exe";
            info.Arguments = "/C netsh http add urlacl url=http://*:" + portNo.ToString() + "/ user=everyone";
            info.UseShellExecute = true;
            Process process = new Process();
            process.StartInfo = info;
            process.Start();
        }

        public bool Stop()
        {
            Log("Stopping Webmanager");

            try
            {
                _serverThread.Abort();
                Log("Thread aborted");

                _listener.Stop();
                Log("Listener Stopped");
                
                _threadStarted = false;

                return true;
            }
            catch (Exception)
            {
                Log("Stop webmanager failed", 1);
                return false;
            }

            
        }
    }
    
}
