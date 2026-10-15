using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>Text that rises and fades where something happened: points, pickups, "OUT OF AMMO".</summary>
    public struct PopupText
    {
        public string Text;
        public Vector2 Pos;
        public Color Color;
        public float Age;
        /// <summary>Its text size, or 0 for the usual.</summary>
        public int Size;
    }

    /// <summary>The run's points, and the popups that announce them. The HUD draws both.</summary>
    public sealed class Scoring
    {
        /// <summary>Kept by the authority; co-op followers copy it from the shared world.</summary>
        public int Points;
        public readonly List<PopupText> Popups = new List<PopupText>();

        public void Add(int points, Vector2 at, bool popup)
        {
            Points += points;
            if (popup) Popup(points.ToString(), at, Color.white);
        }

        public void Popup(string text, Vector2 at, Color color, int size = 0) =>
            Popups.Add(new PopupText { Text = text, Pos = at, Color = color, Size = size });

        public void Tick(float dt)
        {
            for (int i = 0; i < Popups.Count; i++)
            {
                PopupText p = Popups[i];
                p.Age += dt;
                p.Pos.y += dt * Tuning.Hud.PopupRise;
                Popups[i] = p;
            }
            Popups.RemoveAll(p => p.Age > Tuning.Hud.PopupLife);
        }
    }
}
