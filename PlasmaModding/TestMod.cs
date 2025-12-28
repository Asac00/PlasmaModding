using Behavior;
using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Visor;

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
                TestMod.CustomDataTypeInit.Logger.LogInfo("All custom types have been registered.");
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
            base.typeName = "New String";
            base.description = "A simple custom string type.";
            base.defaultValue = "";
            base.icon = NewStringType.CreateTestSprite();
            base.sketchIcon = NewStringType.CreateTestSprite();
            base.editorType = typeof(NewStringEditor);
            base.editorObject = AssetBundlesManager.GetObjectFromAssetBundle<GameObject>("PlasmaModding.Resources.Prefabs.test_mod", "New String Editor");
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

    public class NewStringEditor : DataEditor
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("NewStringEditor");

        public override TMP_InputField mainTextField
        {
            get
            {
                return this._inputField;
            }
        }

        private void Awake()
        {
            this._originalSize = this.processorUISize;
        }

        public override void Setup(Agent agent, int propertyId, ProcessorUI processorUI = null, bool canClose = true)
        {
            //base.Setup(agent, propertyId, processorUI, canClose);
            this._runtimeProperty = agent.GetRuntimeProperty(propertyId, true);
            this._processorUI = processorUI;
            this._inputField = Require.ComponentInChildren<TMP_InputField>(this, false, false);
            this._inputField.onValueChanged.AddListener(new UnityAction<string>(this.HandleChange));
            this._inputField.SetTextWithoutNotify(this._runtimeProperty.GetValueText());
            if (!string.IsNullOrEmpty(this._inputField.text))
            {
                this._inputField.caretPosition = 0;
            }
            this._previousText = this._runtimeProperty.GetValueText();
            if (this._processorUI != null)
            {
                // this.processorUISize = (this._runtimeProperty.definition.isScript ? new Vector2(this._originalSize.x * 3f, this._originalSize.y * 2f) : this._originalSize);
                this.processorUISize = new Vector2(500, 1000);
                this.highlightedText.SetActive(this._runtimeProperty.definition.isScript);
                /*if (this._runtimeProperty.definition.isScript)
                {
                    this.scriptMapper.enabled = true;
                    this.scriptMapper.ApplyColors(Holder.instance, false);
                }
                else
                {
                    this.normalMapper.enabled = true;
                    this.normalMapper.ApplyColors(Holder.instance, false);
                }*/
                this._inputField.restoreOriginalTextOnEscape = !this._runtimeProperty.definition.isScript;
                this._inputField.ActivateInputField();

                //StartCoroutine(DelayedActivate());
            }
            this.showApplyMessage = (this._processorUI == null || !this._runtimeProperty.definition.isScript);
            Logger.LogWarning(processorUISize.x);
            Logger.LogWarning(processorUISize.y);
        }

        public override void CleanUp()
        {
            base.CleanUp();
            this._inputField.onValueChanged.RemoveAllListeners();
            this._inputField.onValidateInput = null;
        }

        private void HandleChange(string text)
        {
            if (!this._runtimeProperty.definition.isScript && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) && (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)))
            {
                this._inputField.SetTextWithoutNotify(this._previousText);
                base.SetData(new Data(this._inputField.text), null);
                this._inputField.DeactivateInputField(false);
                EventSystem.current.SetSelectedGameObject(null);
                if (this._processorUI != null)
                {
                    base.Apply();
                }
            }
            else
            {
                this._previousText = text;
                if (this._processorUI != null)
                {
                    base.SetData(new Data(this._inputField.text), null);
                }
            }
            base.SetDirty(this._runtimeProperty.definition.isScript || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || (!Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter)));
        }

        public GameObject highlightedText;

        private TMP_InputField _inputField;

        private Vector2 _originalSize;

        private string _previousText;
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
