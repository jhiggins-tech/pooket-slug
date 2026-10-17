using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>How much a log channel reports. Each level includes the ones after it.</summary>
    public enum LogLevel { Debug, Info, Warn, Error, Off }

    /// <summary>
    /// A named log channel. Its lines read "[PooketSlug/Net] ...", so they're easy to filter in the browser
    /// console and in CI logs. Each channel has its own level, and the page address can change them at
    /// startup: <c>?log=net:debug,game:warn</c> (see <see cref="Configure"/>).
    /// </summary>
    public sealed class Log
    {
        public static readonly Log Game = new Log("Game");
        public static readonly Log Net = new Log("Net");
        public static readonly Log Audio = new Log("Audio");
        public static readonly IReadOnlyList<Log> All = new[] { Game, Net, Audio };

        public readonly string Name;
        public LogLevel Level = LogLevel.Info;
        readonly string m_Prefix;

        Log(string name)
        {
            Name = name;
            m_Prefix = "[PooketSlug/" + name + "] ";
        }

        public bool IsEnabled(LogLevel level) => level != LogLevel.Off && level >= Level;

        public void Debug(string message) { if (IsEnabled(LogLevel.Debug)) UnityEngine.Debug.Log(m_Prefix + message); }
        public void Info(string message) { if (IsEnabled(LogLevel.Info)) UnityEngine.Debug.Log(m_Prefix + message); }
        public void Warn(string message) { if (IsEnabled(LogLevel.Warn)) UnityEngine.Debug.LogWarning(m_Prefix + message); }
        public void Exception(Exception e) { if (IsEnabled(LogLevel.Error)) UnityEngine.Debug.LogException(e); }

        /// <summary>
        /// Applies a spec such as "net:debug,game:warn", or "all:debug" for every channel. Unknown channels
        /// and levels are ignored. Returns how many channel levels it set.
        /// </summary>
        public static int Configure(string spec)
        {
            int set = 0;
            foreach ((string channel, LogLevel level) in ParseSpec(spec))
                foreach (Log log in All)
                {
                    if (channel != "all" && !string.Equals(channel, log.Name, StringComparison.OrdinalIgnoreCase)) continue;
                    log.Level = level;
                    set++;
                }
            return set;
        }

        /// <summary>Parses "net:debug,game:warn" into (channel, level) pairs, skipping malformed entries.</summary>
        public static List<(string channel, LogLevel level)> ParseSpec(string spec)
        {
            var pairs = new List<(string, LogLevel)>();
            if (string.IsNullOrEmpty(spec)) return pairs;
            foreach (string part in spec.Split(','))
            {
                int colon = part.IndexOf(':');
                if (colon <= 0) continue;
                string channel = part.Substring(0, colon).Trim().ToLowerInvariant();
                string level = part.Substring(colon + 1).Trim();
                if (channel.Length > 0 && TryParseLevel(level, out LogLevel parsed)) pairs.Add((channel, parsed));
            }
            return pairs;
        }

        static bool TryParseLevel(string text, out LogLevel level)
        {
            switch (text.ToLowerInvariant())
            {
                case "debug": level = LogLevel.Debug; return true;
                case "info": level = LogLevel.Info; return true;
                case "warn": case "warning": level = LogLevel.Warn; return true;
                case "error": level = LogLevel.Error; return true;
                case "off": case "none": level = LogLevel.Off; return true;
                default: level = LogLevel.Info; return false;
            }
        }
    }
}
