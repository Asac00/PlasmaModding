using Behavior;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheraBytes.BetterUi;
using UnityEngine;
using Visor;

namespace PlasmaModding
{
    public static class CustomNodeManager
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CustomNodeManager");

        static IEnumerable<AgentGestalt> agentGestalts = Enumerable.Empty<AgentGestalt>();
        static bool loadedNodeResources = false;
        static bool awoken = false;
        static Dictionary<Type, Dictionary<int, Dictionary<int, string>>> customProviders = new Dictionary<Type, Dictionary<int, Dictionary<int, string>>>();
        static bool allProvidersRegistered = false;
        static Dictionary<string, AgentCategoryEnum> customCategories = new Dictionary<string, AgentCategoryEnum>();
        private static int highestCategoryId = 10;
        private static int recent_port_dict_id;

        public static void Awake()
        {
            if (awoken) return;
            awoken = true;

            if (Holder.agentGestalts != null)
            {
                loadedNodeResources = true;
            }
        }
        public class LateGestaltRegistrationException : Exception { }
        private static void RegisterGestalt(AgentGestalt gestalt)
        {
            Awake();
            if (loadedNodeResources)
                throw new LateGestaltRegistrationException();
            agentGestalts = agentGestalts.Concat(new[] { gestalt });
        }

        public class InsufficientGestaltDataException : Exception
        {
            public InsufficientGestaltDataException(string message) : base(message)
            {
            }
        }

        public static void CreateNode(AgentGestalt gestalt, string unique_node_name)
        {
            gestalt.id = (AgentGestaltEnum)unique_node_name.GetHashCode() + 1000;
            if (gestalt.agent == null)
                throw new InsufficientGestaltDataException("No agent attached to gestalt");
            if (gestalt.displayName == null)
                throw new InsufficientGestaltDataException("Node should have a display name");
            if (gestalt.properties == null)
                gestalt.properties = new Dictionary<int, AgentGestalt.Property>();
            if (gestalt.ports == null)
                gestalt.ports = new Dictionary<int, AgentGestalt.Port>();
            RegisterGestalt(gestalt);
        }
        public static AgentGestalt CreateGestalt(Type agent, string displayName, string description = null, AgentCategoryEnum category = AgentCategoryEnum.Misc)
        {
            AgentGestalt gestalt = (AgentGestalt)ScriptableObject.CreateInstance(typeof(AgentGestalt));
            gestalt.componentCategory = AgentGestalt.ComponentCategories.Behavior;
            gestalt.properties = new Dictionary<int, AgentGestalt.Property>();
            gestalt.ports = new Dictionary<int, AgentGestalt.Port>();
            gestalt.type = AgentGestalt.Types.Logic;

            gestalt.agent = agent;
            gestalt.displayName = displayName;
            gestalt.description = description;
            gestalt.nodeCategory = category;

            return gestalt;
        }
        private static AgentGestalt.Port CreateGenericPort(AgentGestalt gestalt, string name, string description)
        {
            AgentGestalt.Port port = new AgentGestalt.Port();
            int port_dict_id = 1;
            try
            {
                port_dict_id = GetHighestKey(gestalt.ports) + 1;
            }
            catch (Exception) { }

            int position = 1;
            try
            {
                position = gestalt.ports[port_dict_id - 1].position + 1;
            }
            catch (Exception) { }


            port.position = position;
            gestalt.ports.Add(port_dict_id, port);
            port.name = name;
            port.description = description;
            recent_port_dict_id = port_dict_id;
            return port;
        }

        public static AgentGestalt.Port CreateCommandPort(AgentGestalt gestalt, string name, string description, int operation)
        {
            AgentGestalt.Port port = CreateGenericPort(gestalt, name, description);
            port.operation = operation;
            port.type = AgentGestalt.Port.Types.Command;
            return port;
        }

        public static AgentGestalt.Port CreatePropertyPort(AgentGestalt gestalt, string name, string description, Data defaultData = null, string reference_name = null, bool hidePort = false, bool isTypeEditable = false)
        {
            if (defaultData == null)
                defaultData = new Data();

            AgentGestalt.Port port = CreateGenericPort(gestalt, name, description);
            AgentGestalt.Property property = new AgentGestalt.Property();

            int property_dict_id = 1;
            try
            {
                property_dict_id = GetHighestKey(gestalt.ports) + 1;
            }
            catch (Exception) { }

            property.position = port.position;
            gestalt.properties.Add(property_dict_id, property);

            if (gestalt.agent.IsSubclassOf(typeof(CustomAgent)))
            {
                if (!CustomAgent.properties.ContainsKey(gestalt.agent))
                {
                    CustomAgent.properties.Add(gestalt.agent, new Dictionary<string, int>());

                }
                CustomAgent.properties[gestalt.agent].Add(reference_name ?? name, property_dict_id);
            }

            property.accessible = true;
            property.configurable = true;
            property.defaultData = defaultData;
            property.name = name;
            property.description = description;
            property.allowsAnyData = isTypeEditable;
            property.handler = 64;

            port.dataType = defaultData.type;
            port.mappedProperty = property_dict_id;
            port.type = AgentGestalt.Port.Types.Property;
            port.hidePort = hidePort;
            port.expectsData = true;
            port.allowsAnyData = isTypeEditable;

            return port;
        }

        public static AgentGestalt.Port CreateSelectionPort(AgentGestalt gestalt, string name, string description, List<string> choices, int defaultID = 0)
        {
            allProvidersRegistered = false;

            Dictionary<int, string> value = new Dictionary<int, string>();
            for (int j = 0; j < choices.Count; j++)
            {
                value.Add(j, choices[j]);
            }

            if (!customProviders.ContainsKey(gestalt.agent))
            {
                customProviders.Add(gestalt.agent, new Dictionary<int, Dictionary<int, string>>());
            }
            int category = customProviders[gestalt.agent].Count;
            customProviders[gestalt.agent].Add(category, value);


            Data.Selection selection = new Data.Selection
            {
                provider = gestalt.agent,
                category = category,
                id = defaultID
            };
            Data selectionData = new Data(selection);

            AgentGestalt.Port port = CreatePropertyPort(gestalt, name, description, selectionData, null, true, false);

            return port;
        }

        private static int GetHighestKey(Dictionary<int, AgentGestalt.Port> l)
        {
            return l.Keys.OrderBy(b => b).Last();
        }

        public static AgentGestalt.Port CreateOutputPort(AgentGestalt gestalt, string name, string description, Data defaultData = null, string reference_name = null, bool isTypeEditable = false)
        {
            if (defaultData == null)
                defaultData = new Data();

            AgentGestalt.Port port = CreateGenericPort(gestalt, name, description);
            AgentGestalt.Property property = new AgentGestalt.Property();

            int property_dict_id = 1;
            try
            {
                property_dict_id = GetHighestKey(gestalt.ports) + 1;
            }
            catch (Exception) { }

            property.position = port.position;
            gestalt.properties.Add(property_dict_id, property); 

            if (gestalt.agent.IsSubclassOf(typeof(CustomAgent)))
            {
                if (!CustomAgent.outputs.ContainsKey(gestalt.agent))
                {
                    CustomAgent.outputs.Add(gestalt.agent, new Dictionary<string, int>());

                }
                CustomAgent.outputs[gestalt.agent].Add(reference_name ?? name, recent_port_dict_id);
            }

            property.defaultData = defaultData;
            property.name = name;
            property.description = description;
            property.injectable = defaultData.type == Data.Types.None;
            port.dataType = defaultData.type;
            port.injectedProperty = isTypeEditable ? property_dict_id : 0;
            port.type = AgentGestalt.Port.Types.Output;
            return port;
        }

        public static AgentCategoryEnum CustomCategory(string name)
        {
            name = name.ToUpperInvariant();
            if (customCategories.ContainsKey(name))
                return customCategories[name];
            customCategories.Add(name, (AgentCategoryEnum)(++highestCategoryId));
            return (AgentCategoryEnum)highestCategoryId;
        }

        // TODO : Not functionnal yet
        public static void SendNotification(SketchNotifications.Levels level, SketchNotifications.Types type, AgentGestalt.Port port, string log)
        {
            SketchNotifications.Notification notification = new SketchNotifications.Notification();
            notification.level = level;
            notification.type = type;
            notification.portId = port.position;
            notification.propertyId = port.position;
            notification.log = log;

            
            // Sketch.SendNotification(notification);
        }

        [HarmonyPatch(typeof(Resources), "LoadAll", new Type[] { typeof(string), typeof(Type) })]
        private class LoadResourcesPatch
        {
            public static void Postfix(string path, Type systemTypeInstance, ref UnityEngine.Object[] __result)
            {
                if (path == "Gestalts/Logic Agents" && systemTypeInstance == typeof(AgentGestalt) && !loadedNodeResources)
                {
                    int size = __result.Length;
                    int newSize = size + agentGestalts.Count();
                    UnityEngine.Object[] temp = new UnityEngine.Object[newSize];
                    __result.CopyTo(temp, 0);
                    agentGestalts.ToArray().CopyTo(temp, size);
                    __result = temp;
                    loadedNodeResources = true;
                }
            }
        }

        [HarmonyPatch(typeof(Visor.ProcessorUICategoryItem), nameof(Visor.ProcessorUICategoryItem.Setup))]
        private class AddCategoryToDictPatch
        {
            static int applied = 0;
            public static void Prefix()
            {
                if (Holder.instance.agentCategories != null && applied < customCategories.Count())
                    foreach (string categoryName in customCategories.Keys)
                    {
                        if (!Holder.instance.agentCategories.ContainsKey(customCategories[categoryName]))
                        {
                            applied++;
                            Holder.instance.agentCategories.Add(customCategories[categoryName], categoryName);
                        }
                    }
            }
        }

        [HarmonyPatch(typeof(DataSelectionProvider), "GetOption")]
        private class GetOptionPatch
        {
            public static void Prefix()
            {
                if (!allProvidersRegistered)
                {
                    Type targetType = typeof(DataSelectionProvider);

                    FieldInfo providersField = targetType.GetField("_providers", BindingFlags.Static | BindingFlags.NonPublic);
                    Dictionary<Type, Dictionary<int, Dictionary<int, string>>> providers
                        = (Dictionary<Type, Dictionary<int, Dictionary<int, string>>>)providersField.GetValue(null);

                    RegisterProviders(providers);
                }
            }
        }

        [HarmonyPatch(typeof(DataSelectionProvider), "GetOptions")]
        private class GetOptionsPatch
        {
            public static void Prefix()
            {
                if (!allProvidersRegistered)
                {
                    Type targetType = typeof(DataSelectionProvider);

                    FieldInfo providersField = targetType.GetField("_providers", BindingFlags.Static | BindingFlags.NonPublic);
                    Dictionary<Type, Dictionary<int, Dictionary<int, string>>> providers
                        = (Dictionary<Type, Dictionary<int, Dictionary<int, string>>>)providersField.GetValue(null);

                    RegisterProviders(providers);
                }
            }
        }

        private static void RegisterProviders(Dictionary<Type, Dictionary<int, Dictionary<int, string>>> providers)
        {
            foreach (var provider in customProviders)
            {
                if (providers.ContainsKey(provider.Key)) { continue; }
                providers.Add(provider.Key, provider.Value);
            }
            allProvidersRegistered = true;
        }

        [HarmonyPatch(typeof(ProcessorUINodeLibrary), "Awake")]
        public class ProcessorUINodeLibrary_Awake_Patch
        {
            public static bool Prefix(ProcessorUINodeLibrary __instance)
            {
                // === Access private fields via reflection ===
                var escapeManagerField = AccessTools.Field(typeof(ProcessorUINodeLibrary), "_escapeManager");
                var nodeItemsField = AccessTools.Field(typeof(ProcessorUINodeLibrary), "_nodeItems");
                var categoryItemsField = AccessTools.Field(typeof(ProcessorUINodeLibrary), "_categoryItems");
                var categorizedItemsField = AccessTools.Field(typeof(ProcessorUINodeLibrary), "_categorizedItems");
                var favoriteItemsField = AccessTools.Field(typeof(ProcessorUINodeLibrary), "_favoriteItems");
                var favoriteCategoryField = AccessTools.Field(typeof(ProcessorUINodeLibrary), "_favoriteCategory");

                // === Reproduce original Awake field initialization ===
                escapeManagerField.SetValue(
                    __instance,
                    Require.ComponentInParent<EscapeManager>(__instance)
                );

                nodeItemsField.SetValue(
                    __instance,
                    new Dictionary<AgentId, GameObject>()
                );

                categoryItemsField.SetValue(
                    __instance,
                    new Dictionary<AgentCategoryEnum, GameObject>()
                );

                categorizedItemsField.SetValue(
                    __instance,
                    new Dictionary<AgentCategoryEnum, List<GameObject>>()
                );

                favoriteItemsField.SetValue(
                    __instance,
                    new List<GameObject>()
                );

                // === Create Favorite category (mandatory, created in original Awake) ===
                GameObject favoriteGO = GameObject.Instantiate(
                    __instance.categoryItemPrefab,
                    __instance.content,
                    false
                );

                var favoriteCategory =
                    Require.Component<ProcessorUICategoryItem>(favoriteGO);

                favoriteCategory.Setup(AgentCategoryEnum.Misc, __instance, true);

                // IMPORTANT: assign to private field (equivalent to this._favoriteCategory = ...)
                favoriteCategoryField.SetValue(__instance, favoriteCategory);

                // === Collect actual categories (native + custom) ===
                var categories = new HashSet<AgentCategoryEnum>();

                // Native categories actually used by the game
                foreach (var kvp in Holder.logicNodesByCategory)
                    categories.Add(kvp.Key);

                // Custom categories injected by the mod
                foreach (var custom in customCategories.Values)
                    categories.Add(custom);

                // Sort categories by displayed name (same behavior as original UI)
                var orderedCategories = categories
                    .OrderBy(c => Holder.instance.agentCategories[c])
                    .ToList();

                // === Build UI ===
                var categoryItems = (Dictionary<AgentCategoryEnum, GameObject>)categoryItemsField.GetValue(__instance);
                var categorizedItems = (Dictionary<AgentCategoryEnum, List<GameObject>>)categorizedItemsField.GetValue(__instance);
                var nodeItems = (Dictionary<AgentId, GameObject>)nodeItemsField.GetValue(__instance);

                foreach (var category in orderedCategories)
                {
                    GameObject categoryGO = GameObject.Instantiate(
                        __instance.categoryItemPrefab,
                        __instance.content,
                        false
                    );

                    Require.Component<ProcessorUICategoryItem>(categoryGO)
                        .Setup(category, __instance, false);

                    categoryItems.Add(category, categoryGO);

                    var nodeList = new List<GameObject>();
                    categorizedItems.Add(category, nodeList);

                    // Skip categories without nodes
                    if (!Holder.logicNodesByCategory.TryGetValue(category, out var nodes))
                        continue;

                    foreach (AgentId agentId in nodes)
                    {
                        GameObject nodeGO = GameObject.Instantiate(
                            __instance.nodeItemPrefab,
                            __instance.content,
                            false
                        );

                        nodeGO.GetComponent<ProcessorUINodeItem>()
                              .Setup(agentId, category, __instance);

                        nodeItems.Add(agentId, nodeGO);
                        nodeList.Add(nodeGO);
                    }
                }

                // === Skip original Awake ===
                return false;
            }
        }
    }
}