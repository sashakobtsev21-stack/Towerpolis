using System;
using System.Collections.Generic;
using UnityEngine;

namespace Towerpolis.Game.UI
{
    /// <summary>
    /// Lightweight localization service (ADR-0008): RU + EN string tables in code, a persisted language, and
    /// a <see cref="LanguageChanged"/> event so code-built UI can re-resolve. Core stays string-free — this
    /// lives entirely in the Unity layer. Use <see cref="T(string)"/> for literals and
    /// <see cref="T(string,object[])"/> (a <c>string.Format</c>) for anything with numbers — never concatenate
    /// localized fragments. A missing key returns <c>"#key"</c> (+ an error in the editor) so holes are loud.
    /// </summary>
    public static class Loc
    {
        const string PrefKey = "towerpolis.lang";

        static Dictionary<string, string> _active;
        static bool _ready;

        /// <summary>The active language (always Russian or English).</summary>
        public static SystemLanguage Language { get; private set; } = SystemLanguage.Russian;

        /// <summary>Fired after the language changes so already-built labels can re-resolve.</summary>
        public static event Action LanguageChanged;

        /// <summary>Idempotent boot: restore the saved language, or pick a default from the device
        /// (Russian → RU; every other system language → EN). Safe to call from each HUD's Start.</summary>
        public static void Init()
        {
            if (_ready) return;
            _ready = true;
            SystemLanguage lang;
            if (PlayerPrefs.HasKey(PrefKey))
                lang = (SystemLanguage)PlayerPrefs.GetInt(PrefKey);
            else
                lang = Application.systemLanguage == SystemLanguage.Russian
                    ? SystemLanguage.Russian : SystemLanguage.English;
            Apply(lang);
        }

        /// <summary>Switch language (persists + notifies). No-op if unchanged.</summary>
        public static void SetLanguage(SystemLanguage lang)
        {
            lang = lang == SystemLanguage.Russian ? SystemLanguage.Russian : SystemLanguage.English;
            if (_ready && lang == Language) return;
            Apply(lang);
            PlayerPrefs.SetInt(PrefKey, (int)lang);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        static void Apply(SystemLanguage lang)
        {
            Language = lang;
            _active = lang == SystemLanguage.Russian ? LocTables.Ru : LocTables.En;
        }

        /// <summary>Resolve a key to the active-language string.</summary>
        public static string T(string key)
        {
            if (!_ready) Init();
            if (_active != null && _active.TryGetValue(key, out string value)) return value;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("[Loc] missing key: " + key);
#endif
            return "#" + key;
        }

        /// <summary>Resolve a format key and fill it (e.g. <c>T("hud.best", score)</c>).</summary>
        public static string T(string key, params object[] args)
        {
            string template = T(key);
            try { return string.Format(template, args); }
            catch (FormatException)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[Loc] bad format for key: " + key);
#endif
                return template;
            }
        }
    }
}
