using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// What the local soldier is told to do this frame. Every input source (keyboard, gamepad, the touch pad)
    /// adds to the same frame, so they can be used together.
    /// </summary>
    public struct SoldierInput
    {
        /// <summary>How far a stick must lean to run, and to aim up or crouch. Gamepads and the touch pad share them.</summary>
        public const float RunThreshold = 0.4f, AimThreshold = 0.5f;

        public bool Left, Right, Up, Down;
        public bool Jump, JumpPressed;
        public bool Fire, FirePressed;
        public bool Special, SpecialPressed;
        public bool Super, SuperPressed;
        public bool Ability, AbilityPressed;
        /// <summary>TEAM: the team super, in co-op when both supers are charged (<see cref="TeamSupers"/>).</summary>
        public bool Team, TeamPressed;

        /// <summary>-1, 0 or 1: pushing both ways at once cancels out, as with the arrow keys.</summary>
        public float MoveX => (Right ? 1f : 0f) - (Left ? 1f : 0f);

        /// <summary>Adds a stick (x right, y up, length up to 1) as if its directions were arrow keys.</summary>
        public void AddStick(Vector2 stick)
        {
            Left |= stick.x < -RunThreshold;
            Right |= stick.x > RunThreshold;
            Up |= stick.y > AimThreshold;
            Down |= stick.y < -AimThreshold;
        }

        /// <summary>Adds another source's input: anything either one presses counts.</summary>
        public void Add(in SoldierInput other)
        {
            Left |= other.Left;
            Right |= other.Right;
            Up |= other.Up;
            Down |= other.Down;
            Jump |= other.Jump;
            JumpPressed |= other.JumpPressed;
            Fire |= other.Fire;
            FirePressed |= other.FirePressed;
            Special |= other.Special;
            SpecialPressed |= other.SpecialPressed;
            Super |= other.Super;
            SuperPressed |= other.SuperPressed;
            Ability |= other.Ability;
            AbilityPressed |= other.AbilityPressed;
            Team |= other.Team;
            TeamPressed |= other.TeamPressed;
        }
    }
}
