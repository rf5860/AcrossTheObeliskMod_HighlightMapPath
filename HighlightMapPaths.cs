using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AtO_HighlightMapPaths
{
    [BepInPlugin(PLUGIN_GUID, "Highlight Map Paths", "3.0.1")]
    public class HighlightMapPaths : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.rjf.ato.highlightmappaths";
        public static readonly string[] COLOR_CODES = { "#75fbff", "#ff2fc7", "#8cff86", "#ffae62", "#d634ff" };
        public void Awake()
        {
            var harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();
        }

        static FieldInfo mapNode = typeof(MapManager).GetField(nameof(mapNode), BindingFlags.NonPublic | BindingFlags.Instance);
        static Dictionary<string, Node> _mapNode;
        static FieldInfo roads = typeof(MapManager).GetField(nameof(roads), BindingFlags.NonPublic | BindingFlags.Instance);
        static Dictionary<string, Transform> _roads;
        static FieldInfo roadTemp = typeof(MapManager).GetField(nameof(roadTemp), BindingFlags.NonPublic | BindingFlags.Instance);
        static List<string> _roadTemp;
        static FieldInfo availableNodes = typeof(MapManager).GetField(nameof(availableNodes), BindingFlags.NonPublic | BindingFlags.Instance);
        static List<string> _availableNodes;

        [HarmonyPatch(typeof(MapManager))]
        public class MapManager_Patch
        {
            public static void GetPaths(Node start, Node end, HashSet<string> visitedNodes, List<string> pathList, List<List<string>> paths)
            {
                if (start.Equals(end))
                {
                    paths.Add(new List<string>(pathList));
                    return;
                }
                visitedNodes.Add(start.name);
                foreach (var n in start.nodeData.NodesConnected.Where(n => !_availableNodes.Contains(n.NodeId)))
                {
                    if (!visitedNodes.Contains(n.NodeId))
                    {
                        pathList.Add(n.NodeId);
                        GetPaths(_mapNode[n.NodeId], end, visitedNodes, pathList, paths);
                        pathList.Remove(n.NodeId);
                    }
                }
                visitedNodes.Remove(start.name);
            }

            public static List<List<string>> GetPaths(Node start, Node end)
            {
                var isVisited = new HashSet<string>();
                var paths = new List<List<string>> { };
                var pathList = new List<string> { start.name };
                GetPaths(start, end, isVisited, pathList, paths);
                return paths;
            }

            public static void DrawArrow(Node _nodeSource, Node _nodeDestination, Color from, Color to)
            {
                string text = _nodeSource.nodeData.NodeId + "-" + _nodeDestination.nodeData.NodeId;
                if (_roads.ContainsKey(text))
                {
                    Transform transform = _roads[text];
                    transform.gameObject.SetActive(value: true);
                    LineRenderer component = transform.GetComponent<LineRenderer>();
                    component.startColor = from;
                    component.endColor = to;
                    _roadTemp.Add(text);
                }
            }

            [HarmonyPatch(nameof(DrawArrowsTemp))]
            [HarmonyPostfix]
            public static void DrawArrowsTemp(ref MapManager __instance, Node _nodeSource)
            {
                var log = BepInEx.Logging.Logger.CreateLogSource("DrawArrowsTemp");
                _mapNode = mapNode.GetValue(__instance) as Dictionary<string, Node>;
                _roads = roads.GetValue(__instance) as Dictionary<string, Transform>;
                _roadTemp = roadTemp.GetValue(__instance) as List<string>;
                _availableNodes = availableNodes.GetValue(__instance) as List<string>;
                var currentNode = _mapNode[AtOManager.Instance.currentMapNode];
                var paths = GetPaths(currentNode, _nodeSource);
                log.LogInfo("Paths: " + paths.Count);
                for (int i1 = 0; i1 < paths.Count; i1++)
                {
                    List<string> path = paths[i1];
                    log.LogInfo(string.Format("Paths {0}: {1}", i1, String.Join(" → ", path.Select(p => _mapNode[p].nodeData.NodeName))));
                    Color color = Globals.Instance.MapArrowTemp;
                    ColorUtility.TryParseHtmlString(COLOR_CODES[i1 % COLOR_CODES.Length], out color);
                    for ( var i2 = 0; i2 < path.Count - 1; i2++)
                    {
                        DrawArrow(_mapNode[path[i2]], _mapNode[path[i2+1]], color, color);
                    }
                }
            }
        }
    }
}