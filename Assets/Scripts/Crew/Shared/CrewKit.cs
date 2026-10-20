using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A crewmate's kit in a player's hands: the slots every crewmate fills (the primary, SP, SU, a frame's tick, drawing,
    /// letting go when they go down or climb into a tabk, what their pose tells a partner) and how the kit changes the
    /// soldier's movement. <see cref="Player"/> holds one and calls the slots, with no crewmate's code of its own; a slot a
    /// kit leaves alone does what a plain soldier does. The rules and numbers stay in Core (<see cref="KitMeters"/>, each
    /// crewmate's tuning). A partner's screen copies the kit with a <see cref="KitCopy"/> and <see cref="OnPartnerMove"/>.
    /// </summary>
    public abstract class CrewKit
    {
        /// <summary>How loud a partner's moves are on our screen.</summary>
        public const float PartnerVolume = 0.35f;

        protected readonly Player Player;
        protected World World => Player.World;

        protected CrewKit(Player player) => Player = player;

        /// <summary>The kit <paramref name="player"/>'s crewmate fights with.</summary>
        public static CrewKit For(Player player)
        {
            if (player.Def == Content.Tones2) return new Tones2Kit(player);
            if (player.Def == Content.Kie) return new KieKit(player);
            if (player.Def == Content.Kcaj) return new KcajKit(player);
            return new PlainKit(player);
        }

        // ------------------------------------------------------------------ how it changes the soldier

        /// <summary>Rooted to the spot: can't move, turn, crouch, jump or fire (tones2 shaking before a rocket jump).</summary>
        public virtual bool Rooted => false;
        /// <summary>Locked on to something: shuffles at <see cref="SpeedScale"/>, can't turn, jump or fire (kcaj's beam).</summary>
        public virtual bool Fixated => false;
        /// <summary>How fast they walk, against their usual speed.</summary>
        public virtual float SpeedScale => 1f;
        /// <summary>Climbing from a launch: letting go of JUMP doesn't cut it short.</summary>
        public virtual bool Rising => false;
        /// <summary>Busy with the super: can't fire anything else, and nothing hurts them (tones2's spew).</summary>
        public virtual bool Busy => false;

        // ------------------------------------------------------------------ the slots

        /// <summary>Every frame, down, in a tabk or on foot, before anything else (kcaj's pills rain on whatever happens to him).</summary>
        public virtual void Always(float dt) { }

        /// <summary>Lets go of whatever's held: they went down, or climbed into a tabk.</summary>
        public virtual void LetGo() { }

        /// <summary>A frame on foot, after aiming and before firing (tones2's spew pours).</summary>
        public virtual void Tick(float dt) { }

        /// <summary>The primary is a stream held down rather than shots (tones2's jet): <see cref="HoldPrimary"/> every frame instead.</summary>
        public virtual bool Streams => false;

        /// <summary>The stream, <paramref name="firing"/> or not.</summary>
        public virtual void HoldPrimary(float dt, bool firing) { }

        /// <summary>The primary throws something of the kit's own rather than a shot (kie's weasels): <see cref="FirePrimary"/>.</summary>
        public virtual bool OwnPrimary => false;

        /// <summary>One press of an <see cref="OwnPrimary"/> from <paramref name="muzzle"/>: false when nothing went (the gun doesn't cool down).</summary>
        public virtual bool FirePrimary(Vector2 muzzle) => false;

        /// <summary>
        /// SP: <paramref name="pressed"/> this frame as a special (not a tabk call-in), <paramref name="held"/> while it's
        /// down. A plain special lobs a charge of <see cref="SoldierDef.Special"/>.
        /// </summary>
        public virtual void Special(float dt, bool pressed, bool held)
        {
            if (!pressed || !Player.Kit.UseSpecial()) return;
            WeaponDef sw = Player.Def.Special;
            World.Combat.FirePlayerShot(sw, Player.Pos + new Vector2(Player.Facing * 0.3f, Player.Height * 0.8f), sw.LobVelocity(Player.Facing, Player.Vel));
            Sfx.PlayShot(sw);
        }

        /// <summary>SU, with the super charged. A plain super fires <see cref="SoldierDef.Super"/> (<see cref="Player.FireSuper"/>).</summary>
        public virtual void Super()
        {
            if (Player.Kit.UseSuper()) Player.FireSuper();
        }

        /// <summary>Draws the kit's own pieces (a gauge, a beam), on foot or not.</summary>
        public virtual void Render() { }

        /// <summary>How far the kit shakes the soldier this frame.</summary>
        public virtual Vector2 ShakeOffset => Vector2.zero;

        /// <summary>What the soldier's pose says about the kit, for the partner's copy (<see cref="KitCopy"/>).</summary>
        public virtual void Pose(ref AvatarPose pose) { }

        // ------------------------------------------------------------------ co-op

        /// <summary>Tells the partner about a move that isn't a shot, so their screen copies it.</summary>
        public static void Send(World w, KitMove move, Vector2 at, Vector2 data) => w.Net?.SendKit(new KitEvent(move, at, data));

        /// <summary>The partner's crewmate made a move: a harmless copy of it here (theirs does the damage).</summary>
        public static void OnPartnerMove(World w, in KitEvent move)
        {
            switch (move.Move)
            {
                case KitMove.Weasel: KieKit.CopyWeasel(w, move); break;
                case KitMove.Trollogram: KieKit.OnTrollEvent(w, move); break;
                case KitMove.Mud: Tones2Kit.CopyMud(w, move.At); break;
                default:
                    // the rest are played by the partner's copy (kcaj's beam and pills, from him or his tabk)
                    foreach (RemotePlayer r in w.Remotes) r.Moves?.OnMove(move);
                    break;
            }
        }
    }

    /// <summary>A soldier with no kit of their own (the trooper, a stand-in): plain shots, a lobbed special, a plain super.</summary>
    public sealed class PlainKit : CrewKit
    {
        public PlainKit(Player player) : base(player) { }
    }

    /// <summary>
    /// A partner's crewmate on our screen: what their kit shows from their pose alone (tones2's liquids, his shaking),
    /// as harmless copies. Moves that aren't in the pose arrive as <see cref="KitEvent"/>s (<see cref="CrewKit.OnPartnerMove"/>),
    /// some of them for the copy to play (<see cref="OnMove"/>: kcaj's beam and pills).
    /// </summary>
    public abstract class KitCopy
    {
        protected readonly RemotePlayer Remote;

        protected KitCopy(RemotePlayer remote) => Remote = remote;

        /// <summary>The copy for <paramref name="remote"/>'s crewmate, or null when their pose shows nothing of their kit.</summary>
        public static KitCopy For(RemotePlayer remote)
        {
            if (remote.Link.Soldier == Content.Tones2) return new Tones2Copy(remote);
            if (remote.Link.Soldier == Content.Kcaj) return new KcajCopy(remote);
            return null;
        }

        public abstract void Tick(float dt, in AvatarPose pose, Vector2 vel);

        /// <summary>Draws the copy's own pieces (kcaj's beam and pills).</summary>
        public virtual void Render(in AvatarPose pose) { }

        /// <summary>A move of theirs that isn't in the pose, for the copy to play.</summary>
        public virtual void OnMove(in KitEvent move) { }

        /// <summary>How far the kit shakes them this frame.</summary>
        public virtual Vector2 ShakeOffset(in AvatarPose pose) => Vector2.zero;
    }
}
