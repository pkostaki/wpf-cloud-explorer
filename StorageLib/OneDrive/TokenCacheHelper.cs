using Microsoft.Identity.Client;
using System.IO;
using System.Security.Cryptography;

namespace StorageLib.OneDrive
{
    static class TokenCacheHelper
    {
        // Todo review this tutorial code.
         //https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/token-cache-serialization
        static TokenCacheHelper()
        {
            try
            {
                // For packaged desktop apps (MSIX packages, also called desktop bridge) the executing assembly folder is read-only. 
                // In that case we need to use Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path + "\msalcache.bin" 
                // which is a per-app read/write folder for packaged apps.
                // See https://docs.microsoft.com/windows/msix/desktop/desktop-to-uwp-behind-the-scenes

                CacheFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".msalcache.bin3");
            }
            catch (System.InvalidOperationException)
            {
                // Fall back for an unpackaged desktop app
                CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin3";
            }

        }

        /// <summary>
        /// Path to the token cache
        /// </summary>
        private static string CacheFilePath { get; set; }

        private static readonly object FileLock = new object();
        private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                        ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath),
                                                 null,
                                                 DataProtectionScope.CurrentUser)
                        : null);
            }
        }

        private static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changesgs in the persistent store
                    File.WriteAllBytes(CacheFilePath,
                                       ProtectedData.Protect(args.TokenCache.SerializeMsalV3(),
                                                             null,
                                                             DataProtectionScope.CurrentUser));
                }
            }
        }

        internal static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        internal static void ClearCacheAsync()
        {
            lock (FileLock)
            {
                try
                {
                    File.Delete(CacheFilePath);
                }
                catch
                {
                    // do nothing
                }
            }
        }
    }
}
