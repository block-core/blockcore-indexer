using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Blockcore.Indexer.Core.Client
{
   #region Using Directives

   #endregion

   /// <summary>
   /// The certificate handler.
   /// </summary>
   public class CertificateHandler
   {
      public static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
      {
         return IsValidCryptoRequest(sender) ? IsValidCryptoCertificate(certificate) : sslPolicyErrors == SslPolicyErrors.None;
      }

      private static bool IsValidCryptoCertificate(X509Certificate certificate)
      {
         return true;
         //var certin = certificate as X509Certificate2;
         //if (certin != null)
         //{
         //    X509Certificate2 certOut = null;
         //    if (CertUtil.TryResolveCertificate(StoreName.CertificateAuthority, StoreLocation.LocalMachine, X509FindType.FindByThumbprint, certin.Thumbprint, out certOut))
         //    {
         //        return true;
         //    }

         //    // TODO: this is a temporary fix to allow fiddler certificates to pass validation.
         //    if (CertUtil.TryResolveCertificate(StoreName.My, StoreLocation.CurrentUser, X509FindType.FindByThumbprint, certin.Thumbprint, out certOut))
         //    {
         //        return true;
         //    }
         //}

         //return false;
      }

      private static bool IsValidCryptoRequest(object sender)
      {
         return sender is HttpWebRequest req && req.Headers.AllKeys.Any(key => key == "x-crypto");
      }
   }
}
