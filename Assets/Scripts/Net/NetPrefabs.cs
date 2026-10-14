#if FUSION2
using System;
using Fusion;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The project has no prefab assets, so the two network prefabs are built in code too: a template is
    /// assembled under an inactive root (so it never runs itself), baked, and registered in Fusion's prefab
    /// table under a fixed GUID. Every client registers them in the same order, so the ids line up.
    /// </summary>
    static class NetPrefabs
    {
        public static NetworkPrefabId Player, World;
        static GameObject s_Root;

        public static void Register()
        {
            if (s_Root != null) return;
            s_Root = new GameObject("[Network Templates]");
            s_Root.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(s_Root);
            Player = Add<PlayerNet>("PlayerNet", new Guid("7c0f3d1e-52a8-4f0b-9a61-3b1e6d2c0a01"), NetworkObjectFlags.DestroyWhenStateAuthorityLeaves);
            World = Add<WorldNet>("WorldNet", new Guid("7c0f3d1e-52a8-4f0b-9a61-3b1e6d2c0a02"), NetworkObjectFlags.MasterClientObject);
        }

        static NetworkPrefabId Add<T>(string name, Guid guid, NetworkObjectFlags flags) where T : NetworkBehaviour
        {
            var go = new GameObject(name);
            go.transform.SetParent(s_Root.transform, false);
            NetworkObject obj = go.AddComponent<NetworkObject>();
            go.AddComponent<T>();
            obj.Flags |= flags;
            new TemplateBaker().Bake(go);

            NetworkObjectGuid id = guid;
            NetworkPrefabTable table = NetworkProjectConfig.Global.PrefabTable;
            var source = new NetworkPrefabSourceStatic { Object = obj, AssetGuid = id, AssetPath = name };
            return table.TryAddSource(source, out NetworkPrefabId prefabId) ? prefabId : table.GetId(id);
        }

        sealed class TemplateBaker : NetworkObjectBaker
        {
            // The baker wants an execution order for objects that start inactive; ours never run as templates.
            protected override bool TryGetExecutionOrder(MonoBehaviour obj, out int order)
            {
                order = obj is NetworkObject ? 1 : 0;
                return true;
            }
        }
    }
}
#endif
