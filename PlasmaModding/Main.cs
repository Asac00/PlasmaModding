using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlasmaModding
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "com.plasmamodding";
        public const string pluginName = "Plasma Modding";
        public const string pluginVersion = "1.0.0";

        private static Harmony harmony;

        void Awake()
        {
            if (harmony != null)
                return;

            harmony = new Harmony("com.plasmamodding");
            harmony.PatchAll();
        }
    }
}
