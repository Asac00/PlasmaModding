using Behavior;
using BepInEx.Logging;
using HarmonyLib;
using PlasmaModding.CustomTypes;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TheraBytes.BetterUi;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Visor;
using static UnityEngine.ImageConversion;

namespace PlasmaModding
{
    public static class CustomTypeManager
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CustomTypeManager");

        public static readonly Dictionary<string, ICustomType> customTypesProperties = new Dictionary<string, ICustomType>();
        private static readonly Dictionary<string, Data.Types> customTypesByName = new Dictionary<string, Data.Types>();

        static bool awoken = false;
        static int lastEnumNumber = 200;

        public static void Awake()
        {
            if (awoken) return;
            awoken = true;

            if (Holder.sketchViewNodePreviewWidths != null)
            {
                Logger.LogInfo($"{customTypesByName.Count} custom types have successfully been loaded.");
            }
        }

        public class LateTypeRegistrationException : Exception { }

        // This method allows to create a custom Data type
        public static Data.Types CreateType(Type type)
        {
            Data.Types customType = Data.Types.None;

            if (type == null || !type.IsClass || type.IsAbstract)
            {
                Logger.LogError($"Invalid type : {type}");
                return Data.Types.None;
            }

            Type baseType = type.BaseType;
            if (baseType == null || !baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(CustomType<>))
            {
                Logger.LogError($"{type.Name} does not correctly inherit from CustomType<>.");
                return Data.Types.None;
            }

            if (Activator.CreateInstance(type) is ICustomType instance)
            {
                string key = instance.typeName;

                if (!customTypesProperties.ContainsKey(key))
                {
                    customTypesProperties.Add(key, instance);

                    customType = (Data.Types)lastEnumNumber;
                    lastEnumNumber += 1;

                    customTypesByName.Add(key, customType);

                    Logger.LogInfo($"The type {key} has been registered.");
                }
                else
                {
                    Logger.LogWarning($"{key} has already been registered.");
                }
            }
            else
            {
                Logger.LogError($"Failed to instantiate {type.Name} as ICustomType.");
                return Data.Types.None;
            }

            Awake();

            return customType;
        }

        /// 
        ///  Data Patch
        /// 

        // Add the custom types to the valid types
        [HarmonyPatch]
        private class Data_ValidTypesPatch
        {
            static MethodBase TargetMethod()
            {
                return typeof(Data).GetMethod("ValidTypes", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            public static void Postfix(ref IList<ValueDropdownItem<Data.Types>> __result)
            {
                foreach (string typeName in customTypesByName.Keys)
                {
                    __result.Add(new ValueDropdownItem<Data.Types>(typeName, customTypesByName[typeName]));
                }
            }
        }

        // Initialize the default values for the custom types
        [HarmonyPatch(typeof(Data), MethodType.Constructor)]
        private class Data_ConstructorPatch
        {
            public static void Postfix(Data __instance)
            {
                InititializeValuesByName(__instance);
            }
        }

        // It is the equivalent of Data(T value) for the custom types
        public static Data NewData<T>(string typeName, T value)
        {
            var data = new Data();
            data.type = customTypesByName[typeName];
            DataExtension.Get(data).valuesByName[typeName] = value;
            return data;
        }

        // Copy the values of a Data to anothe
        [HarmonyPatch(typeof(Data), "Copy")]
        private class Data_CopyPatch
        {
            public static void Postfix(Data __instance, Data data)
            {
                DataExtension.DataHolder holder = DataExtension.Get(__instance);

                DataExtension.DataHolder _holder = DataExtension.Get(data);
                Dictionary<string, object> _valuesByName = _holder.valuesByName;

                holder.valuesByName = new Dictionary<string, object>(_valuesByName);
            }
        }

        // Encode the values of a Data in binary
        [HarmonyPatch(typeof(Data), "Encode")]
        private class Data_EncodePatch
        {
            public static void Postfix(Data __instance, ref byte[] __result)
            {
                if (customTypesByName.ContainsValue(__instance.type))
                {
                    foreach (string typeName in customTypesProperties.Keys)
                    {
                        if (__instance.type == customTypesByName[typeName])
                        {
                            DataExtension.DataHolder holder = DataExtension.Get(__instance);
                            Dictionary<string, object> valuesByName = holder.valuesByName;

                            __result = __result.Concat(customTypesProperties[typeName].ToBytes(valuesByName[typeName])).ToArray();

                            break;
                        }
                    }
                }
            }
        }

        // The custom types values are used in the method Data(byte[] bytes)
        [HarmonyPatch(typeof(Data))]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(byte[]) })]
        public class Data_BytesConstructorPatch
        {
            public static void Postfix(Data __instance, byte[] bytes)
            {
                __instance.type = (Data.Types)Convert.ToInt32(bytes[0]);
                if (customTypesByName.ContainsValue(__instance.type))
                {
                    foreach (string typeName in customTypesProperties.Keys)
                    {
                        if (__instance.type == customTypesByName[typeName])
                        {
                            DataExtension.DataHolder holder = DataExtension.Get(__instance);

                            byte[] valuesBytes = new byte[bytes.Length - 1];
                            Array.Copy(bytes, 1, valuesBytes, 0, valuesBytes.Length);

                            holder.valuesByName[typeName] = customTypesProperties[typeName].FromBytes(valuesBytes);

                            break;
                        }
                    }
                }
            }
        }

        // Check if two instances of Data are equal
        [HarmonyPatch(typeof(Data), "IsEqualTo")]
        private class Data_IsEqualToPatch
        {
            public static void Postfix(Data __instance, Data data, ref bool __result)
            {
                if (customTypesByName.ContainsValue(__instance.type))
                {
                    if (__instance.type == data.type)
                    {
                        DataExtension.DataHolder holder = DataExtension.Get(__instance);
                        Dictionary<string, object> valuesByName = holder.valuesByName;

                        DataExtension.DataHolder _holder = DataExtension.Get(data);
                        Dictionary<string, object> _valuesByName = _holder.valuesByName;

                        foreach (string typeName in valuesByName.Keys)
                        {
                            if (__instance.type == customTypesByName[typeName]) { __result = valuesByName[typeName] == _valuesByName[typeName]; break; }
                        }
                    }
                }
            }
        }

        // Write the name of a Data type
        [HarmonyPatch(typeof(Data), "TypeToString")]
        private class Data_TypeToStringPatch
        {
            public static bool Prefix(Data.Types type, ref string __result)
            {
                if (customTypesByName.ContainsValue(type))
                {
                    foreach (string typeName in customTypesProperties.Keys)
                    {
                        if (type == customTypesByName[typeName]) { __result = $"<{typeName.ToUpper()}>"; break; }
                    }
                    return false;
                }
                return true;
            }
        }

        // Transform a Data into a string that is easily readable for a player
        [HarmonyPatch(typeof(Data), "ToNiceString")]
        private class Data_ToNiceStringPatch
        {
            public static bool Prefix(Data __instance, bool includeType, ref string __result)
            {
                if (customTypesByName.ContainsValue(__instance.type))
                {
                    __result = includeType ? (Data.TypeToString(__instance.type) + " ") : "";

                    foreach (string typeName in customTypesProperties.Keys)
                    {
                        if (__instance.type == customTypesByName[typeName])
                        {
                            InititializeValuesByName(__instance);
                            DataExtension.DataHolder holder = DataExtension.Get(__instance);
                            Dictionary<string, object> valuesByName = holder.valuesByName;

                            __result += customTypesProperties[typeName].ToNiceString(valuesByName[typeName]);

                            break;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        // Transform a Data into a string that contains its whole description
        [HarmonyPatch(typeof(Data), "ToString")]
        private class Data_ToStringPatch
        {
            public static bool Prefix(Data __instance, ref string __result)
            {
                if (customTypesByName.ContainsValue(__instance.type))
                {
                    foreach (string typeName in customTypesProperties.Keys)
                    {
                        if (__instance.type == customTypesByName[typeName])
                        {
                            InititializeValuesByName(__instance);
                            DataExtension.DataHolder holder = DataExtension.Get(__instance);
                            Dictionary<string, object> valuesByName = holder.valuesByName;

                            __result = $"({typeName})";
                            __result += customTypesProperties[typeName].ToString(valuesByName[typeName]);

                            break;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Enum), nameof(Enum.GetNames), new Type[] { typeof(Type) })]
        public class TypesGetNamesPatch
        {
            public static void Postfix(Type enumType, ref string[] __result)
            {
                if (enumType == typeof(Types))
                {
                    var customNames = customTypesByName.Keys.ToArray();
                    string[] combined = new string[__result.Length + customNames.Length];
                    __result.CopyTo(combined, 0);
                    customNames.CopyTo(combined, __result.Length);
                    __result = combined;
                }
            }
        }

        [HarmonyPatch(typeof(System.Enum), "TryParseEnum")]
        public class TypesTryParsePatch
        {
            public static void Postfix(Type enumType, string value, bool ignoreCase, ref object parseResult, ref bool __result)
            {
                if (!__result && enumType == typeof(Types))
                {
                    if (customTypesByName.TryGetValue(value, out Data.Types customValue))
                    {
                        __result = true;

                        Type enumTypeNested = typeof(Enum).GetNestedType("EnumResult", BindingFlags.NonPublic);
                        MethodInfo initMethod = enumTypeNested.GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
                        FieldInfo parsedEnumField = enumTypeNested.GetField("parsedEnum", BindingFlags.NonPublic | BindingFlags.Instance);

                        object enumResultInstance = Activator.CreateInstance(enumTypeNested);
                        initMethod.Invoke(enumResultInstance, new object[] { false });
                        parsedEnumField.SetValue(enumResultInstance, customValue);
                        parseResult = enumResultInstance;
                    }
                }
            }
        }

        ///
        /// AgentProperty Patch
        /// 

        // Change the data type of a Data
        [HarmonyPatch(typeof(AgentProperty), "SetDataType")]
        private class AgentProperty_SetDataTypePatch
        {
            public static bool Prefix(AgentProperty __instance, Data.Types type)
            {
                if (customTypesByName.ContainsValue(type))
                {
                    Data data = __instance.GetValue();
                    if (data.type != type)
                    {
                        foreach (string typeName in customTypesByName.Keys)
                        {
                            if (type == customTypesByName[typeName])
                            {
                                data = NewData(typeName, customTypesProperties[typeName].defaultValue);
                                break;
                            }
                        }
                        __instance.SetValue(data, false, true);
                    }
                    return false;
                }
                return true;
            }
        }

        // Get the value in agentProperty when its type is a custom one
        public static T GetValueCustomType<T>(AgentProperty agentProperty, string typeName)
        {
            Data data = agentProperty.GetValue();

            InititializeValuesByName(data);
            DataExtension.DataHolder holder = DataExtension.Get(data);

            return (T)holder.valuesByName[typeName];
        }

        [HarmonyPatch(typeof(AgentProperty), "SetValue")]
        private class SetValuePatch
        {
            public static bool Prefix(AgentProperty __instance, Data value, ref bool __result, bool forcePostUpdateSteps = false)
            {
                if (customTypesByName.ContainsValue(__instance.definition.defaultData.type))
                {
                    if (value == null)
                    {
                        value = new Data();
                    }

                    MethodInfo isLegalMethod = AccessTools.Method(typeof(AgentProperty), "IsLegal", new Type[] { typeof(Data) });
                    bool isLegal = (bool)isLegalMethod.Invoke(__instance, new object[] { value });
                    if (!isLegal)
                    {
                        __result = false;
                        return false;
                    }

                    bool isRuntime = (bool)AccessTools.Field(typeof(AgentProperty), "_isRuntime").GetValue(__instance);
                    if (isRuntime)
                    {
                        MethodInfo handleRuntimeImageOwnershipMethod = AccessTools.Method(typeof(AgentProperty), "HandleRuntimeImageOwnership", new Type[] { typeof(Data) });
                        handleRuntimeImageOwnershipMethod.Invoke(__instance, new object[] { value });
                    }
                    else
                    {
                        MethodInfo handleConfiguredImageOwnershipMethod = AccessTools.Method(typeof(AgentProperty), "HandleConfiguredImageOwnership", new Type[] { typeof(Data) });
                        handleConfiguredImageOwnershipMethod.Invoke(__instance, new object[] { value });
                    }

                    if (__instance.definition.allowsAnyData || __instance.definition.injectable)
                    {
                        MethodInfo runPostUpdateStepsMethod = AccessTools.Method(typeof(AgentProperty), "RunPostUpdateSteps");
                        if (!__instance.GetValue().IsEqualTo(value, true))
                        {
                            __instance.GetValue().Copy(value);
                            runPostUpdateStepsMethod.Invoke(__instance, null);
                            Data oldData = (Data)AccessTools.Field(typeof(AgentProperty), "_oldData").GetValue(__instance);
                            oldData.Copy(__instance.GetValue());
                        }
                        else if (__instance.definition.alwaysTrigger || forcePostUpdateSteps)
                        {
                            runPostUpdateStepsMethod.Invoke(__instance, null);
                        }
                        __result = true;
                        return false;
                    }

                    foreach (string typeName in customTypesByName.Keys)
                    {
                        if (__instance.definition.defaultData.type == customTypesByName[typeName])
                        {
                            SetValueCustomType(__instance, typeName, value, forcePostUpdateSteps);
                            __result = true;

                            break;
                        }
                    }

                    return false;
                }
                return true;
            }
        }

        public static bool SetValueCustomType<T>(AgentProperty agentProperty, string typeName, T value, bool forcePostUpdateSteps = false)
        {
            Data data = agentProperty.GetValue();

            InititializeValuesByName(data);
            DataExtension.DataHolder holder = DataExtension.Get(data);

            MethodInfo runPostUpdateStepsMethod = AccessTools.Method(typeof(AgentProperty), "RunPostUpdateSteps");

            if (data.type != customTypesByName[typeName])
            {
                Logger.LogError(string.Concat(new string[]
                {
                "Trying to set the value '",
                value.ToString(),
                "' on a Data of type '",
                data.type.ToString(),
                "'"
                }));
                return false;
            }
            if (!EqualityComparer<T>.Default.Equals((T)holder.valuesByName[typeName], value))
            {
                Data oldData = (Data)AccessTools.Field(typeof(AgentProperty), "_oldData").GetValue(agentProperty);

                holder.valuesByName[typeName] = value;
                runPostUpdateStepsMethod.Invoke(agentProperty, null);
                oldData.Copy(data);
                return true;
            }
            if (agentProperty.definition.alwaysTrigger || forcePostUpdateSteps)
            {
                runPostUpdateStepsMethod.Invoke(agentProperty, null);
            }
            return false;
        }

        public static bool SetValueCustomType(AgentProperty agentProperty, string typeName, Data value, bool forcePostUpdateSteps = false)
        {
            InititializeValuesByName(value);
            DataExtension.DataHolder holder = DataExtension.Get(value);

            return SetValueCustomType(agentProperty, typeName, holder.valuesByName[typeName], forcePostUpdateSteps);
        }

        ///
        /// Patch de AgentDebuggerCell
        /// 

        [HarmonyPatch(typeof(AgentDebuggerCell), "UpdateContent")]
        private class UpdateContentPatch
        {
            public static void Postfix(AgentDebuggerCell __instance)
            {
                AgentProperty agentProperty = (AgentProperty)AccessTools.Field(typeof(AgentDebuggerCell), "_property").GetValue(__instance);

                Data value = agentProperty.GetValue();
                Data.Types type = value.type;

                if (customTypesByName.ContainsValue(type))
                {
                    foreach (string typeName in customTypesProperties.Keys)
                    {
                        if (type == customTypesByName[typeName])
                        {
                            string text = (string)AccessTools.Field(typeof(AgentDebuggerCell), "_text").GetValue(__instance);
                            text = value.ToNiceString(false, 2);

                            break;
                        }
                    }
                }
            }
        }

        ///
        /// Patch de AgentGestalt
        /// 

        [HarmonyPatch]
        private class AgentGestaltValidTypesPatch
        {
            static MethodBase TargetMethod()
            {
                return typeof(Data).GetMethod("ValidTypes", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            public static void Postfix(ref IList<ValueDropdownItem<Data.Types>> __result)
            {
                foreach (string typeName in customTypesByName.Keys)
                {
                    __result.Add(new ValueDropdownItem<Data.Types>(typeName, customTypesByName[typeName]));
                }
            }
        }

        ///
        /// Patch de SketchViewNodeRow
        /// 

        public static Holder.DataTypeDescriptor DataTypeDescriptorByName(string typeName)
        {
            Holder.DataTypeDescriptor dataTypeDescriptor = new Holder.DataTypeDescriptor();
            dataTypeDescriptor.name = typeName;
            dataTypeDescriptor.description = customTypesProperties[typeName].description;
            dataTypeDescriptor.icon = customTypesProperties[typeName].icon;
            dataTypeDescriptor.sketchIcon = customTypesProperties[typeName].sketchIcon;

            return dataTypeDescriptor;
        }

        [HarmonyPatch(typeof(SketchViewNodeRow), "BuildPreview")]
        private class SketchViewNodeRowBuildPreviewPatch
        {
            public static void Prefix()
            {
                foreach (var pair in customTypesByName)
                {
                    string typeName = pair.Key;
                    Data.Types customType = pair.Value;

                    List<int> previewSizes = new List<int> { 5, 4 };

                    if (!Holder.sketchViewNodePreviewWidths.ContainsKey(customType))
                    {
                        Holder.sketchViewNodePreviewWidths.Add(customType, previewSizes);
                    }
                    if (!Holder.instance.dataTypeDescriptors.ContainsKey(customType))
                    {
                        Holder.DataTypeDescriptor dataTypeDescriptor = DataTypeDescriptorByName(typeName);
                        Holder.instance.dataTypeDescriptors.Add(customType, dataTypeDescriptor);
                    }
                }
            }
            public static void Postfix(SketchViewNodeRow __instance, bool cleanUpTriggers = true)
            {
                Sketch sketch = (Sketch)AccessTools.Field(typeof(SketchViewNodeRow), "_sketch").GetValue(__instance);
                SketchNode sketchNode = (SketchNode)AccessTools.Field(typeof(SketchViewNodeRow), "_sketchNode").GetValue(__instance);
                AgentProperty agentProperty = (AgentProperty)AccessTools.Field(typeof(SketchViewNodeRow), "_property").GetValue(__instance);

                Data.Types type = agentProperty.GetDataType();

                bool flag = sketch.DoesPropertyUseVariable(sketchNode, agentProperty.id);
                if (!flag && customTypesByName.ContainsValue(type))
                {
                    GameObject textPreview = (GameObject)AccessTools.Field(typeof(SketchViewNodeRow), "textPreview").GetValue(__instance);
                    textPreview.SetActive(true);
                    textPreview.GetComponent<TextMeshPro>().text = agentProperty.GetValue().ToNiceString(false, 2);
                }
            }
        }

        ///
        /// ProcessorUIVariableManagerItem Patch
        /// 

        // Show the value of a variable with a custom type
        [HarmonyPatch(typeof(ProcessorUIVariableManagerItem), "BuildPreview")]
        private class ProcessorUIVariableManagerItemBuildPreviewPatch
        {
            public static void Postfix(ProcessorUIVariableManagerItem __instance)
            {
                AgentProperty agentProperty = (AgentProperty)AccessTools.Field(typeof(ProcessorUIVariableManagerItem), "_variable").GetValue(__instance);
                Data.Types type = agentProperty.GetDataType();

                if (customTypesByName.ContainsValue(type))
                {
                    __instance.textPreview.SetActive(true);
                    __instance.textPreview.GetComponentInChildren<TextMeshProUGUI>().text = agentProperty.GetValue().ToNiceString(false, 2);
                }
            }
        }

        ///
        /// ProcessorUI Patch
        /// 

        // Initialize a cutom Editor
        [HarmonyPatch(typeof(ProcessorUI), "ShowEditor")]
        private class ProcessorUI_ShowEditorPatch
        {
            public static void Prefix(ProcessorUI __instance)
            {
                Agent agentToEdit = (Agent)AccessTools.Field(typeof(ProcessorUI), "_agentToEdit").GetValue(__instance);
                int agentPropertyIdToEdit = (int)AccessTools.Field(typeof(ProcessorUI), "_agentPropertyIdToEdit").GetValue(__instance);

                AgentProperty agentProperty = agentToEdit.runtimeProperties[agentPropertyIdToEdit];
                Data.Types type = agentProperty.GetDataType();

                if (customTypesByName.ContainsValue(type) && !__instance.editors.ContainsKey(type))
                {
                    foreach (string typeName in customTypesByName.Keys)
                    {
                        if (type == customTypesByName[typeName])
                        {
                            Transform referenceTransform = __instance.editors[Data.Types.Text].transform;

                            GameObject customEditorPrefab = AssetBundlesManager.GetObjectFromAssetBundle<GameObject>("PlasmaModding.Resources.Prefabs.plasma_modding", "Custom Editor");

                            GameObject customEditorObject = GameObject.Instantiate(customEditorPrefab, referenceTransform.parent);

                            customEditorObject.name = typeName + " Editor";

                            Type editorType = customTypesProperties[typeName].editorType;
                            DataEditor customEditor = (DataEditor)customEditorObject.AddComponent(editorType);

                            customEditor.closeButton = customEditor.transform.parent.parent.Find("Header/Close Button").GetComponent<BetterButton>();
                            customEditor.confirmMapper = customEditor.transform.parent.parent.Find("Apply Button/Confirm Message").GetComponent<UIColorMapperController>();
                            customEditor.applyButton = customEditor.transform.parent.parent.Find("Apply Button").GetComponent<BetterButton>();

                            __instance.editors.Add(type, customEditor);

                            break;
                        }
                    }
                }
            }
        }

        // Add the custom types to the type selection
        [HarmonyPatch(typeof(ProcessorUI), "ConfigureTypeSelector")]
        private class ConfigureTypeSelectorPatch
        {
            public static void Prefix(ProcessorUI __instance, Data.Types typeToShow)
            {
                List<Data.Types> hasATypeSelector = new List<Data.Types>();

                foreach (GameObject gameObject in __instance.typeSelectorTypeObjects)
                {
                    TypeSelectionItem component = gameObject.GetComponent<TypeSelectionItem>();
                    if (customTypesByName.ContainsValue(component.type))
                    {
                        hasATypeSelector.Add(component.type);
                    }
                }

                if (hasATypeSelector.Count < customTypesByName.Count)
                {
                    foreach (string typeName in customTypesByName.Keys)
                    {
                        if (!hasATypeSelector.Contains(customTypesByName[typeName]))
                        {
                            GameObject referenceTypeSelector = __instance.typeSelectorTypeObjects[0];
                            GameObject copy = UnityEngine.Object.Instantiate(referenceTypeSelector, referenceTypeSelector.transform.parent);

                            copy.name = typeName + " Button";

                            TypeSelectionItem typeSelectionItem = copy.GetComponent<TypeSelectionItem>();

                            typeSelectionItem.type = customTypesByName[typeName];
                            typeSelectionItem.normalMapper = copy.GetComponents<UIBetterButtonColorMapper>()[0];
                            typeSelectionItem.selectedMapper = copy.GetComponents<UIBetterButtonColorMapper>()[1];

                            Holder.DataTypeDescriptor dataTypeDescriptor = DataTypeDescriptorByName(typeName);
                            typeSelectionItem.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = dataTypeDescriptor.name;
                            typeSelectionItem.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = dataTypeDescriptor.description;
                            typeSelectionItem.transform.Find("Icon").GetComponent<Image>().sprite = dataTypeDescriptor.icon;

                            GameObject[] newtypeSelectorTypeObjects = new GameObject[__instance.typeSelectorTypeObjects.Length + 1];
                            Array.Copy(__instance.typeSelectorTypeObjects, newtypeSelectorTypeObjects, __instance.typeSelectorTypeObjects.Length);
                            newtypeSelectorTypeObjects[__instance.typeSelectorTypeObjects.Length] = copy;

                            __instance.typeSelectorTypeObjects = newtypeSelectorTypeObjects;
                        }
                    }
                }
            }
        }

        /// 
        /// PropertyList Patch
        /// 

        [HarmonyPatch(typeof(PropertyList), "OnPropertyClicked")]
        private class PropertyList_OnPropertyClickedPatch
        {
            public static void Prefix(PropertyList __instance, PropertyRow property)
            {
                Agent agent = (Agent)AccessTools.Field(typeof(PropertyList), "_agent").GetValue(__instance);
                
                AgentProperty agentProperty = agent.runtimeProperties[property.propertyIndex];
                Data.Types type = agentProperty.GetDataType();

                if (customTypesByName.ContainsValue(type) && !__instance.editors.ContainsKey(type))
                {
                    foreach (string typeName in customTypesByName.Keys)
                    {
                        if (type == customTypesByName[typeName])
                        {
                            Transform referenceTransform = __instance.editors[Data.Types.Text].transform;

                            GameObject customEditorPrefab = AssetBundlesManager.GetObjectFromAssetBundle<GameObject>("PlasmaModding.Resources.Prefabs.plasma_modding", "Custom Editor");

                            GameObject customEditorObject = GameObject.Instantiate(customEditorPrefab, referenceTransform.parent);

                            customEditorObject.name = typeName + " Editor";

                            Type editorType = customTypesProperties[typeName].editorType;
                            DataEditor customEditor = (DataEditor)customEditorObject.AddComponent(editorType);

                            __instance.editors.Add(type, customEditorObject);

                            break;
                        }
                    }
                }

                foreach (var pair in customTypesByName)
                {
                    string typeName = pair.Key;
                    Data.Types customType = pair.Value;

                    List<int> previewSizes = new List<int> { 5, 4 };

                    if (!Holder.sketchViewNodePreviewWidths.ContainsKey(customType))
                    {
                        Holder.sketchViewNodePreviewWidths.Add(customType, previewSizes);
                    }
                    if (!Holder.instance.dataTypeDescriptors.ContainsKey(customType))
                    {
                        Holder.DataTypeDescriptor dataTypeDescriptor = DataTypeDescriptorByName(typeName);
                        Holder.instance.dataTypeDescriptors.Add(customType, dataTypeDescriptor);
                    }
                }
            }
        }

        private static readonly Vector2[] buttonPositions = new Vector2[] { new Vector2(-84, 389), new Vector2(-44, 299), new Vector2(-18, 209), new Vector2(-6, 119), new Vector2(-18, 30), new Vector2(-44, -61), new Vector2(-80, -150) };
        private static string nextPageButtonName = "Next Page Button";
        [HarmonyPatch(typeof(PropertyList), "Setup")]
        private class PropertyListSetupPatch
        {
            public static void Postfix(PropertyList __instance)
            {
                if (__instance.typeButtons.childCount != 8 + customTypesByName.Count)
                {
                    List<Data.Types> hasATypeButton = new List<Data.Types>();
                    GameObject nextPageButton = null;

                    foreach (object obj in __instance.typeButtons)
                    {
                        TypeButton component = ((RectTransform)obj).GetComponent<TypeButton>();
                        if (customTypesByName.ContainsValue(component.type))
                        {
                            hasATypeButton.Add(component.type);
                        }
                        if (component.gameObject.name == nextPageButtonName)
                        {
                            nextPageButton = component.gameObject;
                        }
                    }

                    if (nextPageButton == null)
                    {
                        GameObject referenceTypeButton = __instance.typeButtons.GetChild(0).gameObject;
                        nextPageButton = UnityEngine.Object.Instantiate(referenceTypeButton, referenceTypeButton.transform.parent);

                        nextPageButton.name = nextPageButtonName;

                        nextPageButton.GetComponent<RectTransform>().anchoredPosition = buttonPositions[6];

                        nextPageButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "NEXT";

                        Sprite nextPageButtonIcon = LoadSpriteFromAssembly("PlasmaModding.Resources.Next.png");
                        nextPageButton.transform.Find("Icon").GetComponent<Image>().sprite = nextPageButtonIcon;
                    }

                    if (hasATypeButton.Count < customTypesByName.Count)
                    {
                        foreach (string typeName in customTypesByName.Keys)
                        {
                            if (!hasATypeButton.Contains(customTypesByName[typeName]))
                            {
                                GameObject referenceTypeButton = __instance.typeButtons.GetChild(0).gameObject;
                                GameObject copy = UnityEngine.Object.Instantiate(referenceTypeButton, referenceTypeButton.transform.parent);

                                copy.name = typeName + " Button";

                                copy.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = typeName.ToUpper();

                                copy.transform.Find("Icon").GetComponent<Image>().sprite = customTypesProperties[typeName].icon;

                                copy.GetComponent<TypeButton>().type = customTypesByName[typeName];
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PropertyList), "RefreshTypeSelector")]
        private class RefreshTypeSelectorPatch
        {
            public static bool Prefix(PropertyList __instance)
            {
                PropertyListExtension.PropertyListHolder holder = PropertyListExtension.Get(__instance);
                int currentPage = holder.currentPage;

                Agent agent = (Agent)AccessTools.Field(typeof(PropertyList), "_agent").GetValue(__instance);
                PropertyRow selectedProperty = (PropertyRow)AccessTools.Field(typeof(PropertyList), "_selectedProperty").GetValue(__instance);
                Data.Types dataType = agent.GetRuntimeProperty(selectedProperty.propertyIndex, true).GetDataType();

                int j = 0;
                for (int i = 0; i < __instance.typeButtons.childCount; i++)
                {
                    Transform child = __instance.typeButtons.GetChild(i);
                    if (child.name == nextPageButtonName)
                    {
                        continue;
                    }
                    if (j / 6 != currentPage)
                    {
                        child.gameObject.SetActive(false);
                        Data.Types type = child.GetComponent<TypeButton>().type;

                        if (type == dataType)
                        {
                            if(!holder.hasPerformedNextPageAction)
                            {
                                holder.currentPage = j / 6;

                                MethodInfo refreshTypeSelectorMethod = AccessTools.Method(typeof(PropertyList), "RefreshTypeSelector");
                                refreshTypeSelectorMethod.Invoke(__instance, null);

                                return false;
                            }
                            holder.hasPerformedNextPageAction = false;
                        }
                    }
                    else
                    {
                        child.gameObject.SetActive(true);
                        child.gameObject.GetComponent<RectTransform>().anchoredPosition = buttonPositions[j % 6];
                    }
                    j++;
                }

                return true;
            }
        }

        // Manage the click on a type of Data in the properties screen
        [HarmonyPatch(typeof(PropertyList), "OnTypeClicked")]
        private class OnTypeClickedPatch
        {
            public static bool Prefix(PropertyList __instance, TypeButton typeButton)
            {
                if (typeButton.gameObject.name == nextPageButtonName)
                {
                    PropertyListExtension.PropertyListHolder holder = PropertyListExtension.Get(__instance);

                    Agent agent = (Agent)AccessTools.Field(typeof(PropertyList), "_agent").GetValue(__instance);
                    PropertyRow selectedProperty = (PropertyRow)AccessTools.Field(typeof(PropertyList), "_selectedProperty").GetValue(__instance);

                    Data.Types currentDataType = agent.GetRuntimeProperty(selectedProperty.propertyIndex, true).GetDataType();
                    GameObject currentEditorObject = new GameObject();

                    holder.currentPage = (__instance.typeButtons.childCount - 2) / 6 > holder.currentPage ? holder.currentPage + 1 : 0;
                    holder.hasPerformedNextPageAction = true;

                    MethodInfo refreshTypeSelectorMethod = AccessTools.Method(typeof(PropertyList), "RefreshTypeSelector");
                    refreshTypeSelectorMethod.Invoke(__instance, null);

                    return false;
                }

                Data.Types type = typeButton.type;

                if (customTypesByName.ContainsValue(type) && !__instance.editors.ContainsKey(type))
                {
                    foreach (string typeName in customTypesByName.Keys)
                    {
                        if (type == customTypesByName[typeName])
                        {
                            Transform referenceTransform = __instance.editors[Data.Types.Text].transform;

                            GameObject customEditorPrefab = AssetBundlesManager.GetObjectFromAssetBundle<GameObject>("PlasmaModding.Resources.Prefabs.plasma_modding", "Custom Editor");

                            GameObject customEditorObject = GameObject.Instantiate(customEditorPrefab, referenceTransform.parent);

                            customEditorObject.name = typeName + " Editor";

                            Type editorType = customTypesProperties[typeName].editorType;
                            DataEditor customEditor = (DataEditor)customEditorObject.AddComponent(editorType);

                            __instance.editors.Add(type, customEditorObject);

                            break;
                        }
                    }
                }

                foreach (var pair in customTypesByName)
                {
                    string typeName = pair.Key;
                    Data.Types customType = pair.Value;

                    List<int> previewSizes = new List<int> { 5, 4 };

                    if (!Holder.sketchViewNodePreviewWidths.ContainsKey(customType))
                    {
                        Holder.sketchViewNodePreviewWidths.Add(customType, previewSizes);
                    }
                    if (!Holder.instance.dataTypeDescriptors.ContainsKey(customType))
                    {
                        Holder.DataTypeDescriptor dataTypeDescriptor = DataTypeDescriptorByName(typeName);
                        Holder.instance.dataTypeDescriptors.Add(customType, dataTypeDescriptor);
                    }
                }

                return true;
            }
        }

        /// 
        /// TypeButton Patch
        /// 

        [HarmonyPatch(typeof(TypeButton), "Select")]
        private class SelectPatch
        {
            public static bool Prefix(TypeButton __instance)
            {
                return __instance.gameObject.name != nextPageButtonName;
            }
        }

        ///
        /// Other methods
        ///

        public static void InititializeValuesByName(Data data)
        {
            DataExtension.DataHolder holder = DataExtension.Get(data);

            foreach (string typeName in customTypesByName.Keys)
            {
                if (!holder.valuesByName.ContainsKey(typeName))
                {
                    holder.valuesByName[typeName] = customTypesProperties[typeName].defaultValue;
                }
            }
        }

        public static void PrintHierarchy(GameObject root)
        {
            Logger.LogWarning($"--- Hiérarchie de {root.name} ---");
            PrintHierarchyRecursive(root.transform, "");
        }

        private static void PrintHierarchyRecursive(Transform current, string indent)
        {
            // Affiche le nom de l'objet
            Logger.LogWarning($"{indent}- {current.name}");

            // Affiche les composants de cet objet
            foreach (var component in current.GetComponents<Component>())
            {
                if (component != null)
                    Logger.LogWarning($"{indent}  ↳ {component.GetType().Name}");
                else
                    Logger.LogWarning($"{indent}  ↳ <Missing Component>");
            }

            // Appel récursif pour les enfants
            foreach (Transform child in current)
            {
                PrintHierarchyRecursive(child, indent + "  ");
            }
        }

        public static Sprite LoadSpriteFromAssembly(string resourcePath)
        {
            // Expected format: "Namespace.Folder.FileName.png"
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                {
                    Logger.LogError($"Resource not found: {resourcePath}");
                    return null;
                }

                byte[] buffer;
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    buffer = ms.ToArray();
                }

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(buffer); // Fills the texture from byte array

                texture.filterMode = FilterMode.Point; // Optional: pixelated look

                Rect rect = new Rect(0, 0, texture.width, texture.height);
                Vector2 pivot = new Vector2(0.5f, 0.5f); // Center pivot

                return Sprite.Create(texture, rect, pivot, 100f); // 100 = pixels per unit
            }
        }
    }

    public static class DataExtension
    {
        private static readonly ConditionalWeakTable<Data, DataHolder> table = new ConditionalWeakTable<Data, DataHolder>();

        public class DataHolder
        {
            public Dictionary<string, object> valuesByName = new Dictionary<string, object>();
        }

        public static DataHolder Get(Data instance) => table.GetOrCreateValue(instance);
    }

    public static class PropertyListExtension
    {
        private static readonly ConditionalWeakTable<PropertyList, PropertyListHolder> table = new ConditionalWeakTable<PropertyList, PropertyListHolder>();

        public class PropertyListHolder
        {
            public int currentPage;
            public bool hasPerformedNextPageAction = false;
        }

        public static PropertyListHolder Get(PropertyList instance) => table.GetOrCreateValue(instance);
    }
}
