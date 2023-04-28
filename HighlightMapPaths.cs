using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
namespace AtO_HighlightMapPaths
{
    [BepInPlugin(PLUGIN_GUID, "Highlight Map Paths", "1.0.0")]
    public class HighlightMapPaths : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.rjf.ato.highlightmappaths";
        public void Awake()
        {
            var harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();
        }

        static FieldInfo mapNode = typeof(MapManager).GetField(nameof(mapNode), BindingFlags.NonPublic | BindingFlags.Instance);
        static Dictionary<string, Node> _mapNode;

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
                foreach (var n in start.nodeData.NodesConnected)
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

            [HarmonyPatch(nameof(DrawArrowsTemp))]
            [HarmonyPrefix]
            public static void DrawArrowsTemp(ref MapManager __instance, Node _nodeSource)
            {
                _mapNode = mapNode.GetValue(__instance) as Dictionary<string, Node>;
                var currentNode = _mapNode[AtOManager.Instance.currentMapNode];
                var paths = GetPaths(currentNode, _nodeSource);
                foreach ( var path in paths )
                {
                    for ( var i = 0; i < path.Count - 1; i++)
                    {
                        __instance.DrawArrow(_mapNode[path[i]], _mapNode[path[i+1]], false, true, false);
                    }
                }
            }
        }
    }
}