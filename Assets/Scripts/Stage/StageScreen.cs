// The base of a screen played on the stage (the den, the tabk bay): its own canvas that takes taps, StageKit's stage and
// world in it, and a tap turned into the stage's art pixels (screens-tidy, Audit 3).
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// A screen on <see cref="StageKit"/>'s stage: a canvas at <c>order</c> with a raycaster, the letterbox, the stage and
    /// its world (320 x 180 art pixels at 2x, pivot in the middle). The screens build their parts in it and hide it when built.
    /// </summary>
    public abstract class StageScreen
    {
        protected readonly RectTransform m_Root, m_Stage, m_World;
        /// <summary>The letterbox round the stage.</summary>
        protected readonly Image m_Letterbox;

        protected StageScreen(Transform parent, string name, int order)
        {
            m_Root = Ui.Canvas(parent, name, order);
            m_Root.gameObject.AddComponent<GraphicRaycaster>();
            m_Letterbox = Build(m_Root, out m_Stage, out m_World);
        }

        public bool Showing => m_Root.gameObject.activeSelf;

        /// <summary>Calls <paramref name="act"/> when <paramref name="rt"/> gets a pointer event of <paramref name="type"/>.</summary>
        protected static void OnPointer(RectTransform rt, EventTriggerType type, System.Action<PointerEventData> act)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(e => act((PointerEventData)e));
            rt.gameObject.AddComponent<EventTrigger>().triggers.Add(entry);
        }

        /// <summary>Where <paramref name="e"/> landed in the world, in art pixels from its bottom left; false if it missed it.</summary>
        protected bool ToArt(PointerEventData e, out Vector2 p)
        {
            p = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_World, e.position, e.pressEventCamera, out Vector2 local)) return false;
            p = local / Px + new Vector2(StageW, StageH) / 2f; // the world's pivot is its middle
            return true;
        }
    }
}
