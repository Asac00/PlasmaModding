using Behavior;
using BepInEx;
using BepInEx.Logging;
using PlasmaModding.CustomTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using TubeRendererExamples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Visor;
using static System.Net.Mime.MediaTypeNames;

namespace PlasmaModding
{
    [BepInPlugin("fr.example.testmod", "Test mod", "1.0.0")]
    public class TestMod : BaseUnityPlugin
    {
        public TestMod()
        {
            TestMod.CustomDataTypeInit.Init();
        }

        private void Awake()
        {
            base.Logger.LogInfo("TestMod has been loaded.");
            this.RegisterCustomNodes();
        }

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

        public const string pluginGuid = "fr.example.testmod";

        public const string pluginName = "Test mod";

        public const string pluginVersion = "1.0.0";

        public static class CustomDataTypeInit
        {
            static CustomDataTypeInit()
            {
            }

            public static void Init()
            {
            }

            private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CustomDataTypeInit");

            public static Data.Types type = CustomTypeManager.CreateType(typeof(NewStringType));
        }
    }

    public class NewStringType : CustomType<string>
    {
        public NewStringType()
        {
            typeName = "New String";
            description = "A simple custom string type.";
            defaultValue = "";
            icon = NewStringType.CreateTestSprite();
            sketchIcon = NewStringType.CreateTestSprite();
            editorType = typeof(NewStringEditor);
            editorObject = AssetBundlesManager.GetObjectFromAssetBundle<GameObject>("PlasmaModding.Resources.Prefabs.test_mod", "New String Editor");
        }

        public override byte[] ToBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public override string FromBytes(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public override string ToNiceString(string value)
        {
            return value;
        }

        public override string ToString(string value)
        {
            return value;
        }

        public static Sprite CreateTestSprite()
        {
            Texture2D texture2D = new Texture2D(1, 1);
            texture2D.SetPixel(0, 0, new Color(1f, 1f, 1f));
            texture2D.Apply();
            return Sprite.Create(texture2D, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }
    }

    public class NewStringEditor : CustomEditor<string>
    {
        public NewStringEditor()
        {
            editorSize = new Vector2(700, 1200);
            typeName = "New String";
            needConfirmation = false;
        }

        public override void BuildUI()
        {
            TMP_Text result = GetOrCreateLabel("result", "");
            PlaceRelativeToEditorCenter("result", Vector2.zero);

            Button addButton = GetOrCreateButton("add", "ADD", 200, 100);
            PlaceRelativeTo("add", "result", RelativePlacement.Below, 100);
            AddListener(Add, addButton.onClick);

            Button resetButton = GetOrCreateButton("reset", "RESET", 200, 100);
            PlaceRelativeTo("reset", "add", RelativePlacement.Below, 100);
            AddListener(Reset, resetButton.onClick);
        }

        public override void ApplyValueToUI(string value)
        {
            ChangeLabelText("result", value);
        }

        private void Add()
        {
            outputValue += "A";
            ChangeLabelText("result", outputValue);
        }

        private void Reset()
        {
            outputValue = "";
            ChangeLabelText("result", outputValue);
        }
    }

    public class TestAgent : CustomAgent
    {
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

        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("TestAgent");

        private int imageTextureIndex = 1;
    }

    public class InjectAgent : CustomAgent, IDataSelectionProvider
    {
        [SketchNodePortOperation(1)]
        public void Inject(SketchNode sketchNode)
        {
            string entry = GetProperty("value").GetValueText();
            WriteOutput("output", CustomTypeManager.NewData<string>("New String", entry));
        }
    }
}
