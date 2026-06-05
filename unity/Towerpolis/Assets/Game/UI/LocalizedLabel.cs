using TMPro;
using UnityEngine;

namespace Towerpolis.Game.UI
{
    /// <summary>
    /// Binds a code-built <see cref="TMP_Text"/> to a localization key (ADR-0008): sets the text now and
    /// re-resolves on every <see cref="Loc.LanguageChanged"/>. Use for STATIC captions (button labels,
    /// titles). Dynamic labels (counts, costs) stay plain and resolve via <c>Loc.T</c> in their repaint
    /// method instead. Self-unsubscribes on destroy.
    /// </summary>
    public sealed class LocalizedLabel : MonoBehaviour
    {
        TMP_Text _text;
        string _key;
        object[] _args; // optional format args for the rare static-but-parameterised caption

        public void Bind(TMP_Text text, string key)
        {
            _text = text;
            _key = key;
            Apply();
        }

        /// <summary>Update the format args and re-apply (for a caption that includes a number).</summary>
        public void SetArgs(params object[] args)
        {
            _args = args;
            Apply();
        }

        void OnEnable()
        {
            Loc.LanguageChanged += Apply;
            if (_key != null) Apply();
        }

        void OnDisable() => Loc.LanguageChanged -= Apply;

        void Apply()
        {
            if (_text == null || _key == null) return;
            _text.text = _args != null && _args.Length > 0 ? Loc.T(_key, _args) : Loc.T(_key);
        }
    }
}
