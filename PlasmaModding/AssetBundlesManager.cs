using BepInEx.Logging;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PlasmaModding
{
    public static class AssetBundlesManager
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ModsMenu");

        public static T GetObjectFromAssetBundle<T>(string assetBundlePath, string objectName) where T : UnityEngine.Object
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(assetBundlePath))
            {
                if (stream == null)
                {
                    Logger.LogError($"Resource {assetBundlePath} not found!");
                    return default;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    byte[] bundleData = ms.ToArray();

                    AssetBundle bundle = AssetBundle.LoadFromMemory(bundleData);
                    if (bundle == null)
                    {
                        Logger.LogError("Failed to load the bundle from memory!");
                        return default;
                    }

                    return bundle.LoadAsset<T>(objectName);
                }
            }
        }
    }
}
