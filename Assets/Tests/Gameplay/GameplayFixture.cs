using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>
    /// What gameplay tests share: a real <see cref="World"/> on a small map, with a camera and the art made, run by
    /// calling <see cref="World.Tick"/> as the game does each frame. They're PlayMode tests (entities destroy
    /// themselves as they go, which needs play mode); each test gets its own world, torn down after.
    /// </summary>
    public abstract class GameplayFixture
    {
        protected const float Frame = 1f / 60f;
        protected World World;
        Camera m_Camera;

        /// <summary>A flat 40-tile strip (the kit demos' map): 'P' is our soldier, 's' a rifleman, 'r' a runner.</summary>
        protected static string[] Strip(string row) => DemoScript.Ground(row.PadRight(40, Level.Empty));

        [SetUp]
        public void MakeCamera()
        {
            Art.Init();
            m_Camera = new GameObject("Test camera").AddComponent<Camera>();
            m_Camera.orthographic = true;
            m_Camera.orthographicSize = Tuning.Camera.ViewHeight * 0.5f;
        }

        [TearDown]
        public void TearDownWorld()
        {
            if (World != null) Object.DestroyImmediate(World.Root.gameObject);
            World = null;
            Object.DestroyImmediate(m_Camera.gameObject);
        }

        protected World Make(string row, INetLink net = null, SoldierDef soldier = null)
        {
            World = new World(m_Camera, new MissionDef("test", byte.MaxValue, "TEST", "", Strip(row)), net, soldier);
            return World;
        }

        /// <summary>Runs the world for <paramref name="seconds"/> with the given input held.</summary>
        protected void Run(float seconds, SoldierInput input = default)
        {
            for (float t = 0f; t < seconds; t += Frame) World.Tick(Frame, input);
        }

        protected Enemy First(EnemyBrain brain)
        {
            foreach (Enemy e in World.Enemies)
                if (e.Def.Brain == brain && !e.Removed) return e;
            return null;
        }
    }

    /// <summary>A co-op session that's only a record of what the world sends, and a snapshot the test sets.</summary>
    public sealed class FakeNet : INetLink
    {
        public bool Authority;
        public readonly List<NetEnemyState> Incoming = new List<NetEnemyState>();
        public readonly List<NetEnemyState> Written = new List<NetEnemyState>();
        public readonly List<HitReport> Hits = new List<HitReport>();

        public bool IsAuthority => Authority;
        public int LocalSkin => 0;
        public int LocalPlayerId => 1;
        public int Round => 0;

        public IReadOnlyList<IAvatarLink> RemoteAvatars { get; } = new List<IAvatarLink>();
        public IReadOnlyList<IAvatarLink> Partners { get; } = new List<IAvatarLink>();
        public void SendAvatar(SoldierDef soldier, Vector2 pos, Vector2 vel, int poseFlags, int hp, int tankFlags, Vector2 tankPos) { }
        public void SendDen(SoldierDef soldier, int flags) { }
        public void SendScene(int taps) { }
        public void SendShot(in ShotEvent shot) { }
        public void SendScore(int points) { }

        public int Mission { get; set; }
        public float CamX { get; set; }
        public int Score { get; set; }
        public bool Victory { get; set; }
        public int NextSpawn { get; set; }
        public int NextNetId { get; set; }
        public void WriteEnemies(List<NetEnemyState> enemies)
        {
            Written.Clear();
            Written.AddRange(enemies);
        }
        public void ReadEnemies(List<NetEnemyState> into)
        {
            into.Clear();
            into.AddRange(Incoming);
        }
        public void WritePickups(List<NetPickupState> pickups) { }
        public bool ReadPickups(List<NetPickupState> into) => false;
        public void WriteBoss(in NetBossState boss) { }
        public bool ReadBoss(out NetBossState boss)
        {
            boss = default;
            return false;
        }
        public void SendEnemyShot(in ShotEvent shot) { }
        public void ReportHit(in HitReport hit) => Hits.Add(hit);
        public void ClaimPickup(int id) { }
        public void GrantPickup(int id, PickupDef def, int playerId) { }
        public void RequestRestart() { }
    }
}
