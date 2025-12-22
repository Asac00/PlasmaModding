using Behavior;
using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Visor;

namespace PlasmaModding
{
    [BepInPlugin("fr.example.testmod", "Test mod", "1.0.0")]
    public class TestMod : BaseUnityPlugin
    {
        // Token: 0x06000044 RID: 68 RVA: 0x00002D81 File Offset: 0x00000F81
        public TestMod()
        {
            TestMod.CustomDataTypeInit.Init();
        }

        // Token: 0x06000045 RID: 69 RVA: 0x00002D91 File Offset: 0x00000F91
        private void Awake()
        {
            base.Logger.LogInfo("TestMod has been loaded.");
            this.RegisterCustomNodes();
        }

        // Token: 0x06000046 RID: 70 RVA: 0x00002DAC File Offset: 0x00000FAC
        private void RegisterCustomNodes()
        {
            AgentGestalt agentGestalt1 = CustomNodeManager.CreateGestalt(typeof(TestAgent), "Test", "Test", AgentCategoryEnum.Misc);
            CustomNodeManager.CreateCommandPort(agentGestalt1, "execute", "", 1);
            CustomNodeManager.CreatePropertyPort(agentGestalt1, "value", "The entry", TestMod.CustomDataTypeInit.type, true, CustomTypeManager.NewData<string>("New String", "test"), null);
            CustomNodeManager.CreateOutputPort(agentGestalt1, "output", "The output", Data.Types.Text, false, null, null);
            CustomNodeManager.CreateNode(agentGestalt1, "aefc0112-85fd-4c59-b259-e54bad24ffce");
            AgentGestalt agentGestalt2 = CustomNodeManager.CreateGestalt(typeof(InjectAgent), "Inject", "Inject", AgentCategoryEnum.Misc);
            CustomNodeManager.CreateCommandPort(agentGestalt2, "execute", "", 1);
            CustomNodeManager.CreatePropertyPort(agentGestalt2, "value", "The entry", Data.Types.Text, true, new Data(""), null);
            CustomNodeManager.CreatePropertyPort(agentGestalt2, "next", "Next", Data.Types.Text, true, new Data(""), null, false, true);
            CustomNodeManager.CreateSelectionPort(agentGestalt2, "test", "", new List<string>() { "a", "b", "c" }, 0);
            CustomNodeManager.CreateSelectionPort(agentGestalt2, "test2", "", new List<string>() { "e", "f", "g" }, 0);
            CustomNodeManager.CreateOutputPort(agentGestalt2, "output", "The output", TestMod.CustomDataTypeInit.type, false, null, null);
            CustomNodeManager.CreateNode(agentGestalt2, "12942e63-3636-4d5f-87ae-0e0254d1d156");
        }

        // Token: 0x0400001B RID: 27
        public const string pluginGuid = "fr.example.testmod";

        // Token: 0x0400001C RID: 28
        public const string pluginName = "Test mod";

        // Token: 0x0400001D RID: 29
        public const string pluginVersion = "1.0.0";

        // Token: 0x0200002F RID: 47
        public static class CustomDataTypeInit
        {
            // Token: 0x06000099 RID: 153 RVA: 0x00005409 File Offset: 0x00003609
            static CustomDataTypeInit()
            {
                TestMod.CustomDataTypeInit.Logger.LogInfo("All custom types have been registered.");
            }

            // Token: 0x0600009A RID: 154 RVA: 0x0000543F File Offset: 0x0000363F
            public static void Init()
            {
            }

            // Token: 0x04000033 RID: 51
            private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CustomDataTypeInit");

            // Token: 0x04000034 RID: 52
            public static Data.Types type = CustomTypeManager.CreateType(typeof(NewStringType));
        }
    }

    public class NewStringType : CustomType<string>
    {
        // Token: 0x0600004D RID: 77 RVA: 0x00002FC4 File Offset: 0x000011C4
        public NewStringType()
        {
            base.typeName = "New String";
            base.description = "A simple custom string type.";
            base.defaultValue = "";
            base.icon = NewStringType.CreateTestSprite();
            base.sketchIcon = NewStringType.CreateTestSprite();
            base.editorType = typeof(NewStringEditor);
            base.editorObject = AssetBundlesManager.GetObjectFromAssetBundle<GameObject>("PlasmaModding.Resources.Prefabs.test_mod", "New String Editor");
        }

        // Token: 0x0600004E RID: 78 RVA: 0x0000303C File Offset: 0x0000123C
        public override byte[] ToBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        // Token: 0x0600004F RID: 79 RVA: 0x0000305C File Offset: 0x0000125C
        public override string FromBytes(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        // Token: 0x06000050 RID: 80 RVA: 0x00003080 File Offset: 0x00001280
        public override string ToNiceString(string value)
        {
            return value;
        }

        // Token: 0x06000051 RID: 81 RVA: 0x00003094 File Offset: 0x00001294
        public override string ToString(string value)
        {
            return value;
        }

        // Token: 0x06000052 RID: 82 RVA: 0x000030A8 File Offset: 0x000012A8
        public static Sprite CreateTestSprite()
        {
            Texture2D texture2D = new Texture2D(1, 1);
            texture2D.SetPixel(0, 0, new Color(1f, 1f, 1f));
            texture2D.Apply();
            return Sprite.Create(texture2D, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }
    }

    public class NewStringEditor : DataEditor
    {
        // Token: 0x04000021 RID: 33
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("NewStringEditor");
    }

    public class TestAgent : CustomAgent
    {
        // Token: 0x0600004A RID: 74 RVA: 0x00002EF4 File Offset: 0x000010F4
        [SketchNodePortOperation(1)]
        public void Test(SketchNode sketchNode)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("PlasmaModding.Resources.Next.png"))
            {
                bool flag = manifestResourceStream == null;
                if (flag)
                {
                    TestAgent.Logger.LogError("Resource not found: PlasmaModding.Resources.Next.png");
                }
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    manifestResourceStream.CopyTo(memoryStream);
                    byte[] array = memoryStream.ToArray();
                }
            }
            WriteOutput("output", new Data("test"));
        }

        // Token: 0x0400001F RID: 31
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("TestAgent");

        // Token: 0x04000020 RID: 32
        private int imageTextureIndex = 1;
    }

    public class InjectAgent : CustomAgent, IDataSelectionProvider
    {
        // Token: 0x0600004A RID: 74 RVA: 0x00002EF4 File Offset: 0x000010F4
        [SketchNodePortOperation(1)]
        public void Inject(SketchNode sketchNode)
        {
            string entry = GetProperty("value").GetValueText();
            WriteOutput("output", CustomTypeManager.NewData<string>("New String", entry));
        }
    }
}
