using System.Security.Cryptography.X509Certificates;

namespace X509Helper
{
    public static class X509
    {
        private static string storeName = "WebHosting";

        public static X509Certificate2 Get(string thumbprint)
        {
            using (var certStore = new X509Store(storeName, StoreLocation.CurrentUser))
            {
                try
                {
                    certStore.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                    if (certCollection.Count > 0)
                        return certCollection[0];
                }
                finally
                {
                    certStore.Close();
                }
            }
            return null;
        }

        public static X509Certificate2 Get(string path, string password)
        {
            return new X509Certificate2(
                path,
                password,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable
            );
        }

        public static void Save(X509Certificate2 cert)
        {
            var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
        }
    }
}
