using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace SCVProxy
{
    public static class CAHelper
    {
        private const string makeCertParamsRoot = "-r -ss root -n \"CN=SCVProxy, OU=Loye\" -sky signature -cy authority -a sha1 -m 120";
        private const string makeCertParamsEnd = "-pe -ss my -n \"CN={0}, OU=Loye\" -sky exchange -in \"SCVProxy\" -is root -cy end -a sha1 -m 120";
        private const string makeCertSubject = "CN={0}, OU=Loye";
        private const string makeCertRootDomain = "SCVProxy";
        private static readonly string MAKECERT_FILENAME = ConfigurationManager.AppSettings["MakeCertFileName"];
        private static readonly ConcurrentDictionary<string, X509Certificate2> certificateCache = new ConcurrentDictionary<string, X509Certificate2>();
        private static X509Certificate2 rootCert;
        private static readonly ReaderWriterLockSlim caRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public static X509Certificate2 GetCertificate(string host)
        {
            if (String.IsNullOrEmpty(host))
            {
                return null;
            }
            if (certificateCache.ContainsKey(host))
            {
                return certificateCache[host];
            }

            X509Certificate2 domainCert = LoadCertificateFromWindowsStore(host);
            if (domainCert != null)
            {
                return domainCert;
            }
            domainCert = CreateCertificate(host);
            if (domainCert != null)
            {
                certificateCache[host] = domainCert;
                return domainCert;
            }

            return null;
        }

        private static X509Certificate2 LoadCertificateFromWindowsStore(string host, StoreName storeName = StoreName.My)
        {
            X509Store x509Store = new X509Store(storeName, StoreLocation.CurrentUser);
            try
            {
                caRWLock.EnterReadLock();
                x509Store.Open(OpenFlags.ReadOnly);
                string subject = String.Format(makeCertSubject, host);
                foreach (X509Certificate2 cert in x509Store.Certificates)
                {
                    if (String.Equals(cert.Subject, subject, StringComparison.OrdinalIgnoreCase))
                    {
                        x509Store.Close();
                        return cert;
                    }
                }
            }
            finally
            {
                if (x509Store != null)
                {
                    x509Store.Close();
                }
                caRWLock.ExitReadLock();
            }
            return null;
        }

        private static X509Certificate2 CreateCertificate(string host, bool isRoot = false)
        {
            if (String.IsNullOrEmpty(MAKECERT_FILENAME) || !File.Exists(MAKECERT_FILENAME))
            {
                return null;
            }
            X509Certificate2 cert = null;
            if (!isRoot)
            {
                if (rootCert == null)
                {
                    rootCert = LoadCertificateFromWindowsStore(makeCertRootDomain, StoreName.Root);
                    if (rootCert == null)
                    {
                        rootCert = CreateCertificate(makeCertRootDomain, true);
                        if (rootCert == null)
                        {
                            return null;
                        }
                    }
                }
            }
            int exitCode = 999;
            string execute = MAKECERT_FILENAME;
            string parameters = isRoot ? makeCertParamsRoot : String.Format(makeCertParamsEnd, host);
            try
            {
                caRWLock.EnterWriteLock();
                cert = LoadCertificateFromWindowsStore(host);
                if (cert != null)
                {
                    return cert;
                }
                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = execute;
                    process.StartInfo.Arguments = parameters;
                    process.Start();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }
            finally
            {
                caRWLock.ExitWriteLock();
            }
            if (exitCode == 0)
            {
                cert = LoadCertificateFromWindowsStore(host);
                Logger.Message(String.Format("Create Certification: {0}", cert == null ? "Failed" : cert.Subject), 0, ConsoleColor.DarkYellow);
            }
            return cert;
        }
    }
}
