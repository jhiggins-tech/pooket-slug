using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace PooketSlug
{
    /// <summary>
    /// The game's numbers by name, so the sandbox can change them without a build (<c>tune kit.superkills 6</c>). A
    /// knob is a path of field names, any case: the first is one of <see cref="Tuning"/>'s sections (<c>physics</c>,
    /// <c>kit</c>, <c>lives</c>, ...), a crewmate's kit numbers (<c>tones2.jet.ramptime</c>, from <c>Tones2Tuning</c>,
    /// <c>KieTuning</c>, <c>KcajTuning</c>), or anything in <see cref="Content"/> (<c>rifleman.hp</c>, <c>bosstank.*</c>,
    /// <c>tones2.runspeed</c>); the rest walk into it. Numbers and on/off settings can be changed; a <c>const</c> is
    /// built into the game and can't. Only the sandbox changes them, solo and before its world is built, and a visit
    /// that does plays nothing else.
    /// </summary>
    public static class Knobs
    {
        const BindingFlags Static = BindingFlags.Public | BindingFlags.Static;
        const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;
        const int MaxDepth = 4;

        /// <summary>Where a path starts: a static class, or an object.</summary>
        readonly struct Node
        {
            public readonly Type Type;
            public readonly object Target; // null for a static class

            public Node(Type type, object target)
            {
                Type = type;
                Target = target;
            }

            public FieldInfo[] Fields() => Target == null ? Type.GetFields(Static) : Type.GetFields(Instance);
        }

        static Dictionary<string, List<Node>> s_Roots;

        static Dictionary<string, List<Node>> Roots()
        {
            if (s_Roots != null) return s_Roots;
            var roots = new Dictionary<string, List<Node>>(StringComparer.OrdinalIgnoreCase);
            void Add(string name, Node node)
            {
                if (!roots.TryGetValue(name, out List<Node> list)) roots[name] = list = new List<Node>();
                list.Add(node);
            }
            foreach (Type section in typeof(Tuning).GetNestedTypes(BindingFlags.Public)) Add(section.Name, new Node(section, null));
            // a crewmate's kit numbers first, then their soldier (tones2.jet.ramptime, tones2.runspeed)
            foreach (Type t in typeof(Tuning).Assembly.GetTypes())
                if (t.Namespace == typeof(Tuning).Namespace && t != typeof(Tuning) && t.Name.EndsWith("Tuning", StringComparison.Ordinal) && t.IsAbstract && t.IsSealed)
                    Add(t.Name.Substring(0, t.Name.Length - "Tuning".Length), new Node(t, null));
            foreach (FieldInfo f in typeof(Content).GetFields(Static))
                if (Walks(f.FieldType) && f.GetValue(null) is object value) Add(f.Name, new Node(value.GetType(), value));
            return s_Roots = roots;
        }

        /// <summary>A field the path can walk into: one of the game's own classes (a def or a spec).</summary>
        static bool Walks(Type t) => t.IsClass && t.Namespace == typeof(Tuning).Namespace && !typeof(Delegate).IsAssignableFrom(t);

        static bool IsLeaf(Type t) => t == typeof(float) || t == typeof(int) || t == typeof(bool);

        static FieldInfo Field(Node node, string name)
        {
            foreach (FieldInfo f in node.Fields())
                if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) return f;
            return null;
        }

        /// <summary>Finds the knob <paramref name="path"/>: the field and what it's on, or why not.</summary>
        static bool Find(string path, out FieldInfo field, out object target, out string problem)
        {
            field = null;
            target = null;
            string[] parts = (path ?? "").Split('.');
            if (parts.Length < 2 || !Roots().TryGetValue(parts[0], out List<Node> starts))
            {
                problem = "no knob called " + path;
                return false;
            }
            foreach (Node start in starts)
            {
                Node node = start;
                for (int i = 1; i < parts.Length; i++)
                {
                    FieldInfo f = Field(node, parts[i]);
                    if (f == null) break;
                    object on = node.Target;
                    if (i == parts.Length - 1)
                    {
                        if (!IsLeaf(f.FieldType))
                        {
                            problem = path + " isn't a number or an on/off";
                            return false;
                        }
                        if (f.IsLiteral || f.IsStatic && f.IsInitOnly)
                        {
                            problem = path + " is built into the game";
                            return false;
                        }
                        field = f;
                        target = on;
                        problem = null;
                        return true;
                    }
                    if (!Walks(f.FieldType) || !(f.GetValue(on) is object next)) break;
                    node = new Node(next.GetType(), next);
                }
            }
            problem = "no knob called " + path;
            return false;
        }

        /// <summary>The knob's value now, as the sandbox would write it, or null if there's no such knob.</summary>
        public static string Get(string path) => Find(path, out FieldInfo f, out object on, out _) ? Format(f.GetValue(on)) : null;

        /// <summary>Sets the knob at <paramref name="path"/> to <paramref name="value"/>; false, with the problem, if it can't.</summary>
        public static bool TrySet(string path, string value, out string problem)
        {
            if (!Find(path, out FieldInfo f, out object on, out problem)) return false;
            object parsed = Parse(f.FieldType, value);
            if (parsed == null)
            {
                problem = path + " can't be " + (string.IsNullOrEmpty(value) ? "nothing" : value);
                return false;
            }
            f.SetValue(on, parsed);
            return true;
        }

        static object Parse(Type t, string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (t == typeof(float))
                return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) && !float.IsNaN(f) && !float.IsInfinity(f) ? (object)f : null;
            if (t == typeof(int))
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? (object)i : null;
            switch (value.ToLowerInvariant())
            {
                case "on": case "true": case "yes": case "1": return true;
                case "off": case "false": case "no": case "0": return false;
                default: return null;
            }
        }

        static string Format(object value) =>
            value is float f ? f.ToString("R", CultureInfo.InvariantCulture) :
            value is bool b ? (b ? "on" : "off") :
            Convert.ToString(value, CultureInfo.InvariantCulture);

        /// <summary>Every knob that can be changed, as "path value", in lower case (<c>?sandbox=knobs</c> logs them).</summary>
        public static List<string> All()
        {
            var all = new List<string>();
            var seen = new HashSet<string>();
            foreach (KeyValuePair<string, List<Node>> root in Roots())
                foreach (Node node in root.Value)
                    Walk(root.Key.ToLowerInvariant(), node, 1, all, seen);
            all.Sort(StringComparer.Ordinal);
            return all;
        }

        static void Walk(string path, Node node, int depth, List<string> all, HashSet<string> seen)
        {
            foreach (FieldInfo f in node.Fields())
            {
                string here = path + "." + f.Name.ToLowerInvariant();
                if (IsLeaf(f.FieldType))
                {
                    if (!f.IsLiteral && !(f.IsStatic && f.IsInitOnly) && seen.Add(here)) all.Add(here + " " + Format(f.GetValue(node.Target)));
                }
                else if (depth < MaxDepth && Walks(f.FieldType) && f.GetValue(node.Target) is object next)
                {
                    Walk(here, new Node(next.GetType(), next), depth + 1, all, seen);
                }
            }
        }
    }
}
