using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// The ⓘ kit screen's flow, over the den (the couch pick) or the pause menu: whose kit, and the move whose demo
    /// plays (<see cref="KitGuide"/>).
    /// </summary>
    public sealed class KitFlow
    {
        public bool Open { get; private set; }
        public SoldierDef Soldier { get; private set; }
        public int Move { get; private set; }

        /// <summary>Opens the kit screen on <paramref name="soldier"/>'s moves, the first one's demo playing.</summary>
        public void Show(SoldierDef soldier)
        {
            Open = true;
            Soldier = soldier;
            Move = 0;
            Sfx.Play(Sfx.Pickup, 0.4f);
        }

        /// <summary>The kit screen's buttons: a move (or its own button, so FIRE shows FIRE's), and back.</summary>
        public void Tick(MenuAction click, MenuInput keys, SoldierInput soldier)
        {
            IReadOnlyList<MoveGuide> moves = KitGuide.For(Soldier);
            int pick = -1;
            if (click >= MenuAction.Move1 && click <= MenuAction.Move4) pick = click - MenuAction.Move1;
            else if (soldier.FirePressed) pick = SlotIndex(moves, KitSlot.Fire);
            else if (soldier.SpecialPressed) pick = SlotIndex(moves, KitSlot.Special);
            else if (soldier.SuperPressed) pick = SlotIndex(moves, KitSlot.Super);
            else if (soldier.AbilityPressed) pick = SlotIndex(moves, KitSlot.Ability);
            if (pick >= 0 && pick < moves.Count && pick != Move)
            {
                Move = pick;
                Sfx.Play(Sfx.Pickup, 0.3f);
            }
            if (click == MenuAction.Back || keys.Back || keys.Settings || keys.Start) Open = false;
        }

        static int SlotIndex(IReadOnlyList<MoveGuide> moves, KitSlot slot)
        {
            for (int i = 0; i < moves.Count; i++)
                if (moves[i].Slot == slot) return i;
            return -1;
        }
    }
}
