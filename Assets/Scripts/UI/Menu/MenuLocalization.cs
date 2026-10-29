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
    // Original text and fonts are cached so the menu can safely return to English.
    private sealed class OriginalTextState
    {
        public string text;
        public TMP_FontAsset font;
    }

    private static readonly Dictionary<TextMeshProUGUI, OriginalTextState> originals =
        new Dictionary<TextMeshProUGUI, OriginalTextState>();

    // Each dictionary maps an English UI label to its translated replacement.
    private static readonly Dictionary<string, string> korean = new Dictionary<string, string>
    {
        { "NEWGAME", "새 게임" },
        { "CONTROLS", "조작법" },
        { "BACK", "뒤로" },
        { "SAVE", "저장" },
        { "GRAPHICS", "그래픽" },
        { "GRAPHICSQUALITY", "그래픽 품질" },
        { "LANGUAGE", "언어" },
        { "LOW", "낮음" },
        { "MEDIUM", "중간" },
        { "HIGH", "높음" },
        { "ENG", "영어" },
        { "KOR", "한국어" },
        { "KR", "한국어" },
        { "PH", "타갈로그어" },
        { "WASD-MOVESPACE-JUMPMOUSE-LOOKAROUNDCTRLS-CROUCHRIGHTCLICK-TOUSEBATTERYSROLLUP/DOWN-SWITCHITEMS", "W A S D - 이동\n\nSPACE - 점프\n\nMOUSE - 시점 이동\n\nCTRL - 앉기\n\nRIGHT CLICK - 배터리 사용\n\nSCROLL UP/DOWN - 아이템 전환" },
        { "SHIFT-SPRINTF-FLASHLIGHTE-INTERACTG-DROPITEMSM-MAPESC-PAUSE", "SHIFT - 달리기\n\nF - 손전등\n\nE - 상호작용\n\nG - 아이템 버리기\n\nM - 지도\n\nESC - 일시정지" }
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

        foreach (MenuManager menuManager in Object.FindObjectsOfType<MenuManager>(true))
            menuManager.RefreshAboutLocalization();
    }
}
