using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SCVProxy
{
    public static class Helper
    {
        private static readonly string CA_ROOTCERT_FILENAME = ConfigurationManager.AppSettings["CARootCertFileName"];
        private static readonly string CA_ROOTCERT_PWD_FILENAME = ConfigurationManager.AppSettings["CARootCertPWDFileName"];
        private static readonly string CA_CERTS_PATH = ConfigurationManager.AppSettings["CACertsPath"];
        private static readonly string MAKECERT_FILENAME = ConfigurationManager.AppSettings["MakeCertFileName"];

        private static X509Certificate2 rootCert;
        private static string rootCertPWD;

        public static X509Certificate2 RootCert
        {
            get
            {
                if (rootCert == null)
                {
                    rootCert = new X509Certificate2(CA_ROOTCERT_FILENAME);
                }
                return rootCert;
            }
        }
        public static string RootCertPWD
        {

            get
            {
                if (rootCertPWD == null)
                {
                    rootCertPWD = File.ReadAllText(CA_ROOTCERT_PWD_FILENAME);
                }
                return rootCertPWD;
            }
        }

        public static X509Certificate2 GetCertificate(string host)
        {
            X509Certificate2 domainCert = null;
            if (!String.IsNullOrEmpty(host))
            {
                string caFileName = String.Format(@"{0}\{1}.pfx", CA_CERTS_PATH, host);
                if (File.Exists(caFileName))
                {
                    domainCert = new X509Certificate2(caFileName);
                    Logger.Info("Subject: " + domainCert.Subject, ConsoleColor.Green);
                }
                else
                {
                    //makecert.exe
                    //string x509Name = "CN=" + host;
                    //string param = " -pe -ss my -n \"" + x509Name + "\" ";
                    //Process p = Process.Start(MAKECERT_FILENAME, param);
                    //p.WaitForExit();
                    //p.Close();

                }
            }
            return domainCert;
        }


        public static string GetExecutableOutput(string sExecute, string sParams, out int iExitCode)
        {
            iExitCode = -999;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Results from " + sExecute + " " + sParams + "\r\n\r\n");
            try
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = sExecute;
                process.StartInfo.Arguments = sParams;
                process.Start();
                string str1;
                while ((str1 = process.StandardOutput.ReadLine()) != null)
                {
                    string str2 = str1.TrimEnd(new char[0]);
                    if (str2 != string.Empty)
                        stringBuilder.Append(str2 + "\r\n");
                }
                iExitCode = process.ExitCode;
                process.Dispose();
            }
            catch (Exception ex)
            {
                stringBuilder.Append("Exception thrown: " + ((object)ex).ToString() + "\r\n" + ((object)ex.StackTrace).ToString());
            }
            stringBuilder.Append("-------------------------------------------\r\n");
            return ((object)stringBuilder).ToString();
        }
    }
}
