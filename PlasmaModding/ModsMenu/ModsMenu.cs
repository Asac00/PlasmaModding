using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using Visor;
using UnityEngine;
using TMPro;
using TheraBytes.BetterUi;
using BepInEx.Bootstrap;
using BepInEx;
using System.IO;
using UnityEngine.UI;
using PlasmaModding.ModsMenuScripts;

namespace PlasmaModding
{
    public static class ModsMenu
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ModsMenu");

        private static GameObject modsMenuObject;

        ///
        /// PauseMenu patch
        /// 

        [HarmonyPatch(typeof(PauseMenu), "Setup")]
        private class SetupPatch
        {
            public static void Postfix(PauseMenu __instance)
            {
                if (__instance.transform.Find("Menu").childCount == 8)
                {
                    // Add mods menu button
                    GameObject referenceButton = __instance.transform.Find("Menu").GetChild(6).gameObject;
                    RectTransform lineTransform = __instance.transform.Find("Menu").GetChild(7).GetComponent<RectTransform>();
                    GameObject modsButton = UnityEngine.Object.Instantiate(referenceButton, referenceButton.transform.parent);
                    modsButton.transform.SetSiblingIndex(6);

                    modsButton.name = "Mods Button";
                    modsButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(595, -450);
                    referenceButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(595, -590);
                    modsButton.GetComponentInChildren<TextMeshProUGUI>().text = "MODS";
                    BetterButton button = modsButton.GetComponentInChildren<BetterButton>();
                    button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                    button.onClick.AddListener(() => { OpenModsMenu(__instance); });

                    lineTransform.anchoredPosition = new Vector2(300, -150);
                    lineTransform.sizeDelta = new Vector2(6, 1180);
                    lineTransform.Find("Arc/Logo/Version").GetComponent<TextMeshProUGUI>().text = "MODDED VERSION";

                    // Mods menu setup
                    /*var assembly = Assembly.GetExecutingAssembly();
                    string resourceName = "PlasmaModding.Resources.Prefabs.plasma_modding";
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            Logger.LogError($"Resource {resourceName} not found!");
                            return;
                        }

                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            byte[] bundleData = ms.ToArray();

                            AssetBundle bundle = AssetBundle.LoadFromMemory(bundleData);
                            if (bundle == null)
                            {
                                Logger.LogError("Failed to load the bundle from memory!");
                                return;
                            }

                            GameObject modsMenuPrefab = bundle.LoadAsset<GameObject>("Mod Selector Popup");
                            if (modsMenuPrefab != null)
                            {
                                modsMenuObject = UnityEngine.Object.Instantiate(modsMenuPrefab);
                            }
                        }
                    }*/
                    GameObject modsMenuPrefab = AssetBundlesManager.GetObjectFromAssetBundle<GameObject>("PlasmaModding.Resources.Prefabs.plasma_modding", "Mod Selector Popup");
                    if (modsMenuPrefab != null)
                    {
                        modsMenuObject = UnityEngine.Object.Instantiate(modsMenuPrefab);
                    }

                    modsMenuObject.SetActive(false);

                    // Add scripts to the prefab
                    GameObject modSelectorMask = modsMenuObject.transform.Find("Background/Border/Middle/Mod Selector Mask").gameObject;
                    GameObject modSelectorGroup = modsMenuObject.transform.Find("Background/Border/Middle/Mod Selector Mask/Mod Selector Group").gameObject;
                    Scrollbar scrollbar = modsMenuObject.transform.Find("Background/Border/Middle").GetComponentInChildren<Scrollbar>();

                    GridScroller gridScroller = (GridScroller)modSelectorMask.AddComponent<GridScroller>();
                    gridScroller.content = modSelectorGroup.GetComponent<RectTransform>();
                    gridScroller.viewport = modSelectorMask.GetComponent<RectTransform>();
                    gridScroller.grid = modSelectorGroup.GetComponent<GridLayoutGroup>();
                    gridScroller.scrollbar = scrollbar;

                    GameObject modItemReference = modSelectorGroup.transform.GetChild(0).gameObject;
                    TextMeshProUGUI text = modItemReference.GetComponentInChildren<TextMeshProUGUI>();
                    Toggle toggle = modItemReference.GetComponentInChildren<Toggle>();
                    GameObject toggleHighlight = modItemReference.transform.Find("Toggle/Background/Toggle Highlight").gameObject;

                    ToggleBehavior toggleBehavior = (ToggleBehavior)modItemReference.AddComponent<ToggleBehavior>();
                    toggleBehavior.text = text;
                    toggleBehavior.toggle = toggle;
                    toggleBehavior.toggleHighlight = toggleHighlight;

                    // Add a button for each mod
                    var activePlugins = Chainloader.PluginInfos;

                    foreach (var kvp in activePlugins)
                    {
                        if (kvp.Key == "com.plasmamodding") { continue; }
                        GameObject modItem = UnityEngine.GameObject.Instantiate(modItemReference, modItemReference.transform.parent);
                        modItem.name = kvp.Key;
                        modItem.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = kvp.Value.Metadata.Name;
                    }

                    modItemReference.SetActive(false);

                    // Setup the colose button behaviour
                    Button closeButton = modsMenuObject.transform.Find("Background/Border/Footer").GetComponentInChildren<Button>();
                    closeButton.onClick.AddListener(() => { CloseModsMenu(__instance); });
                }
            }
        }

        private static void OpenModsMenu(PauseMenu pauseMenu)
        {
            MethodInfo obscureMethod = AccessTools.Method(typeof(PauseMenu), "Obscure");
            obscureMethod.Invoke(pauseMenu, null);

            Visor.Visor visor = (Visor.Visor)AccessTools.Field(typeof(PauseMenu), "_visor").GetValue(pauseMenu);
            
            modsMenuObject.SetActive(true);

            modsMenuObject.transform.SetParent(visor.popupCanvas.transform, false);

            AccessTools.Field(typeof(Visor.Visor), "_popupControlsMouseVisibility").SetValue(visor, false);
            visor.popupCanvas.gameObject.SetActive(true);

            GraphicRaycaster mainCanvasRaycaster = (GraphicRaycaster)AccessTools.Field(typeof(Visor.Visor), "_mainCanvasRaycaster").GetValue(visor);
            AccessTools.Field(typeof(Visor.Visor), "_mainCanvasOriginalState").SetValue(visor, mainCanvasRaycaster.enabled);
            mainCanvasRaycaster.enabled = false;

            GenericWindow genericWindow = (GenericWindow)AccessTools.Field(typeof(Visor.Visor), "_genericWindow").GetValue(visor);
            if (genericWindow != null)
            {
                genericWindow.Obscure();
            }
        }

        private static void CloseModsMenu(PauseMenu pauseMenu)
        {
            Visor.Visor visor = (Visor.Visor)AccessTools.Field(typeof(PauseMenu), "_visor").GetValue(pauseMenu);

            GraphicRaycaster mainCanvasRaycaster = (GraphicRaycaster)AccessTools.Field(typeof(Visor.Visor), "_mainCanvasRaycaster").GetValue(visor);
            mainCanvasRaycaster.enabled = (bool)AccessTools.Field(typeof(Visor.Visor), "_mainCanvasOriginalState").GetValue(visor);
            modsMenuObject.SetActive(false);
            visor.popupCanvas.gameObject.SetActive(false);
            AccessTools.Field(typeof(Visor.Visor), "_closingGenericPopup").SetValue(visor, false);

            MethodInfo unobscureMethod = AccessTools.Method(typeof(PauseMenu), "Unobscure");
            unobscureMethod.Invoke(pauseMenu, null);

            var pluginPath = Paths.PluginPath;
            var disabledPath = Path.Combine(pluginPath, "disabled");
            Directory.CreateDirectory(disabledPath);

            Transform modSelectorGroupTransform = modsMenuObject.transform.Find("Background/Border/Middle/Mod Selector Mask/Mod Selector Group");

            /*int i = 0;
            foreach (var plugin in Chainloader.PluginInfos)
            {
                if (plugin.Key == "com.plasmamodding") { continue; }
                var pluginFile = plugin.Value.Location;
                if (modSelectorGroupTransform.GetChild(i).GetComponentInChildren<Toggle>().isOn)
                {
                    var targetFile = Path.Combine(pluginPath, Path.GetFileName(pluginFile));
                    File.Move(pluginFile, targetFile);
                }
                else
                {
                    var targetFile = Path.Combine(disabledPath, Path.GetFileName(pluginFile));
                    File.Move(pluginFile, targetFile);
                }
                i++;
            }*/
        }
    }
}
