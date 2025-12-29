using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PlasmaModding
{
    public static class AssetBundlesManager
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("AssetBundlesManager");

        private static readonly Dictionary<string, AssetBundle> _loadedBundles = new Dictionary<string, AssetBundle>();

        public static T GetObjectFromAssetBundle<T>(string assetBundlePath, string objectName) where T : UnityEngine.Object
        {
            try
            {
                AssetBundle bundle;

                // Reuse already loaded bundle
                if (!_loadedBundles.TryGetValue(assetBundlePath, out bundle) || bundle == null)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    Stream stream = assembly.GetManifestResourceStream(assetBundlePath);

                    if (stream == null)
                    {
                        Logger.LogError($"Embedded resource '{assetBundlePath}' not found.");
                        return default(T);
                    }

                    using (stream)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            byte[] bundleData = ms.ToArray();

                            bundle = AssetBundle.LoadFromMemory(bundleData);
                            if (bundle == null)
                            {
                                Logger.LogError($"Failed to load AssetBundle from resource '{assetBundlePath}'.");
                                return default(T);
                            }

                            _loadedBundles[assetBundlePath] = bundle;
                        }
                    }
                }

                T asset = bundle.LoadAsset<T>(objectName);
                if (asset == null)
                {
                    Logger.LogError($"Asset '{objectName}' not found in AssetBundle '{assetBundlePath}'.");
                }

                return asset;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    $"Exception while loading asset '{objectName}' from bundle '{assetBundlePath}':\n{ex}");
                return default(T);
            }
        }
    }
}
