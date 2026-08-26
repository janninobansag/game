using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the menu's selected language to TextMeshPro UI labels.
/// Original English labels and fonts are kept so switching back is safe.
/// </summary>
public static class MenuLocalization
{
    private sealed class OriginalTextState
    {
        public string text;
        public TMP_FontAsset font;
    }

    private static readonly Dictionary<TextMeshProUGUI, OriginalTextState> originals =
        new Dictionary<TextMeshProUGUI, OriginalTextState>();

    private static readonly Dictionary<string, string> korean = new Dictionary<string, string>
    {
        { "NEWGAME", "\uC0C8 \uAC8C\uC784" },
        { "CONTROLS", "\uC870\uC791\uBC95" },
        { "BACK", "\uB4A4\uB85C" },
        { "SAVE", "\uC800\uC7A5" },
        { "GRAPHICS", "\uADF8\uB798\uD53D" },
        { "GRAPHICSQUALITY", "\uADF8\uB798\uD53D \uD488\uC9C8" },
        { "LANGUAGE", "\uC5B8\uC5B4" },
        { "LOW", "\uB0AE\uC74C" },
        { "MEDIUM", "\uC911\uAC04" },
        { "HIGH", "\uB192\uC74C" },
        { "ENG", "\uC601\uC5B4" },
        { "KOR", "\uD55C\uAD6D\uC5B4" },
        { "KR", "\uD55C\uAD6D\uC5B4" },
        { "PH", "\uD0C0\uAC08\uB85C\uADF8\uC5B4" },
        { "WASD-MOVESPACE-JUMPMOUSE-LOOKAROUNDCTRLS-CROUCHRIGHTCLICK-TOUSEBATTERYSROLLUP/DOWN-SWITCHITEMS", "W A S D - \uC774\uB3D9\n\nSPACE - \uC810\uD504\n\nMOUSE - \uC2DC\uC810 \uC774\uB3D9\n\nCTRL - \uC549\uAE30\n\nRIGHT CLICK - \uBC30\uD130\uB9AC \uC0AC\uC6A9\n\nSCROLL UP/DOWN - \uC544\uC774\uD15C \uC804\uD658" },
        { "SHIFT-SPRINTF-FLASHLIGHTE-INTERACTG-DROPITEMSM-MAPESC-PAUSE", "SHIFT - \uB2EC\uB9AC\uAE30\n\nF - \uC190\uC804\uB4F1\n\nE - \uC0C1\uD638\uC791\uC6A9\n\nG - \uC544\uC774\uD15C \uBC84\uB9AC\uAE30\n\nM - \uC9C0\uB3C4\n\nESC - \uC77C\uC2DC\uC815\uC9C0" }
    };
    private static readonly Dictionary<string, string> tagalog = new Dictionary<string, string>
    {
        { "NEWGAME", "BAGONG LARO" },
        { "CONTROLS", "MGA KONTROL" },
        { "BACK", "BUMALIK" },
        { "SAVE", "I-SAVE" },
        { "GRAPHICS", "GRAPHICS" },
        { "GRAPHICSQUALITY", "KALIDAD NG GRAPHICS" },
        { "LANGUAGE", "WIKA" },
        { "LOW", "MABABA" },
        { "MEDIUM", "KATAMTAMAN" },
        { "HIGH", "MATAAS" },
        { "ENG", "INGLES" },
        { "KOR", "KOREANO" },
        { "KR", "KOREANO" },
        { "PH", "TAGALOG" },
        { "WASD-MOVESPACE-JUMPMOUSE-LOOKAROUNDCTRLS-CROUCHRIGHTCLICK-TOUSEBATTERYSROLLUP/DOWN-SWITCHITEMS", "W A S D - GUMALAW\n\nSPACE - TUMALON\n\nMOUSE - TUMINGIN SA PALIGID\n\nCTRL - YUMUKO\n\nRIGHT CLICK - GUMAMIT NG BATERYA\n\nSCROLL UP/DOWN - MAGPALIT NG ITEM" },
        { "SHIFT-SPRINTF-FLASHLIGHTE-INTERACTG-DROPITEMSM-MAPESC-PAUSE", "SHIFT - TUMAKBO\n\nF - FLASHLIGHT\n\nE - MAKIPAG-UGNAYAN\n\nG - ITAPON ANG ITEM\n\nM - MAPA\n\nESC - I-PAUSE" }
    };
    private static readonly HashSet<string> keepEnglish = new HashSet<string>
    {
        "VAREN", "LOADGAME", "PLAY", "SETTINGS", "ABOUT", "QUIT"
    };

    private static string NormalizeKey(string text)
    {
        return text.Trim().ToUpperInvariant()
            .Replace(" ", string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\t", string.Empty);
    }

    private static bool ShouldKeepEnglish(string text)
    {
        // The stylized main menu stores its labels as "V A R E N" and
        // "P L A Y", so normalize decorative spaces before checking.
        return keepEnglish.Contains(NormalizeKey(text));
    }
    public static void Apply(GameLanguage language, TMP_FontAsset koreanFont)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        TextMeshProUGUI[] allText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();

        foreach (TextMeshProUGUI label in allText)
        {
            if (label == null || label.gameObject.scene != activeScene)
                continue;

            if (!originals.TryGetValue(label, out OriginalTextState original))
            {
                original = new OriginalTextState { text = label.text, font = label.font };
                originals[label] = original;
            }

            if (language == GameLanguage.Korean && koreanFont != null && !ShouldKeepEnglish(original.text))
            {
                if (korean.TryGetValue(NormalizeKey(original.text), out string translated))
                    label.text = translated;
                else
                    label.text = original.text;

                if (koreanFont != null)
                    label.font = koreanFont;
            }
            else if (language == GameLanguage.Tagalog && !ShouldKeepEnglish(original.text))
            {
                label.text = tagalog.TryGetValue(NormalizeKey(original.text), out string translated)
                    ? translated
                    : original.text;

                if (original.font != null)
                    label.font = original.font;
            }
            else
            {
                label.text = original.text;
                if (original.font != null)
                    label.font = original.font;
            }
        }
    }
}