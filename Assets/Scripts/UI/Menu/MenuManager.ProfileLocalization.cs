using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class MenuManager
{
    // Remember the fonts assigned to scene Text components so switching languages
    // never replaces an Inspector choice permanently.
    private readonly Dictionary<Text, Font> profileInspectorFonts = new Dictionary<Text, Font>();

    private Font ProfileFont(bool heading, string text = null)
    {
        if (GetAboutLanguage() == GameLanguage.Korean && profileKoreanFont != null &&
            (text == null || ContainsKorean(text)))
            return profileKoreanFont;

        Font preferred = heading ? profileDisplayFont : profileBodyFont;
        return preferred != null ? preferred : profileFont;
    }

    private static bool ContainsKorean(string value)
    {
        foreach (char character in value)
        {
            if (character >= '\uac00' && character <= '\ud7a3')
                return true;
        }
        return false;
    }

    private string ProfileText(string english, string tagalog, string korean)
    {
        switch (GetAboutLanguage())
        {
            case GameLanguage.Tagalog: return tagalog;
            case GameLanguage.Korean: return korean;
            default: return english;
        }
    }

    private string ProfileLabel(string name)
    {
        return ProfileText("PROFILE: ", "PROFILE: ", "프로필: ") + name;
    }

    private string ProfileDisplayName(GameProfiles.Profile profile)
    {
        return profile.id == GameProfiles.OriginalProfileId
            ? ProfileText("Original Save", "Dating Save", "기존 저장")
            : profile.name;
    }

    private string ProfileDeletedMessage()
    {
        return ProfileText("Profile deleted.", "Nabura ang profile.", "프로필이 삭제되었습니다.");
    }

    private string LocalizeProfileError(string error)
    {
        if (GetAboutLanguage() == GameLanguage.English)
            return error;
        if (error == "Enter a name from 1 to 24 characters.")
            return ProfileText(error, "Maglagay ng pangalan na 1 hanggang 24 na titik.", "이름은 1~24자로 입력하세요.");
        if (error == "That profile name is already in use.")
            return ProfileText(error, "Ginagamit na ang pangalang iyan.", "이미 사용 중인 프로필 이름입니다.");
        if (error == "Could not open that profile.")
            return ProfileText(error, "Hindi mabuksan ang profile.", "프로필을 열 수 없습니다.");
        return ProfileText(error, "Hindi makumpleto ang aksyon sa profile.", "프로필 작업을 완료할 수 없습니다.");
    }

    private void UpdateDeleteConfirmationText()
    {
        if (deleteConfirmationText == null)
            return;
        deleteConfirmationText.text = ProfileText(
            "Permanently delete \"" + pendingDeleteName + "\" and all its saves?",
            "Permanenteng burahin ang \"" + pendingDeleteName + "\" at lahat ng save nito?",
            "\"" + pendingDeleteName + "\" 프로필과 저장 파일을 영구 삭제할까요?");
        ApplyProfileLocalizedFont(deleteConfirmationText, deleteConfirmationText.text);
    }

    public void RefreshProfileLocalization()
    {
        if (profilePanel == null || profileList == null || activeProfileText == null)
            return;

        SetProfileLabel(profilePanel.transform.Find("Profile Card/Title"),
            ProfileText("CHOOSE YOUR STORY", "PILIIN ANG IYONG KUWENTO", "프로필 선택"));
        SetProfileLabel(profilePanel.transform.Find("Profile Card/Hint"),
            ProfileText("Each player keeps a separate story.",
                "Magkahiwalay ang progreso ng bawat manlalaro.",
                "플레이어마다 진행 상황이 따로 저장됩니다."));
        SetProfileLabel(profilePanel.transform.Find("Profile Card/New Profile Name/Placeholder"),
            ProfileText("Enter a new profile name", "Pangalan ng bagong profile", "새 프로필 이름"));
        SetProfileLabel(profilePanel.transform.Find("Profile Card/New Profile Name/Name"),
            profileNameInput != null ? profileNameInput.text : "", true);
        SetProfileLabel(profilePanel.transform.Find("Profile Card/Create Profile/Label"),
            ProfileText("CREATE PROFILE", "GUMAWA NG PROFILE", "프로필 만들기"));
        SetProfileLabel(profilePanel.transform.Find("Profile Card/Close Profile/Label"),
            ProfileText("CLOSE", "ISARA", "닫기"));
        SetProfileLabel(profilePanel.transform.Find("Delete Confirmation/Confirm Delete/Label"),
            ProfileText("DELETE FOREVER", "BURAHIN NANG TULUYAN", "영구 삭제"));
        SetProfileLabel(profilePanel.transform.Find("Delete Confirmation/Cancel Delete/Label"),
            ProfileText("CANCEL", "KANSELAHIN", "취소"));
        if (profileErrorText != null)
            ApplyProfileLocalizedFont(profileErrorText, profileErrorText.text);
        if (pendingDeleteId != null)
            UpdateDeleteConfirmationText();
        RefreshProfileMenu();
    }

    private void SetProfileLabel(Transform target, string value, bool koreanInput = false)
    {
        if (target == null)
            return;
        Text label = target.GetComponent<Text>();
        if (label == null)
            return;
        label.text = value;
        ApplyProfileLocalizedFont(label, value, koreanInput);
    }

    private void ApplyProfileLocalizedFont(Text label, string value, bool koreanInput = false)
    {
        if (!profileInspectorFonts.TryGetValue(label, out Font inspectorFont))
        {
            inspectorFont = label.font;
            profileInspectorFonts.Add(label, inspectorFont);
        }

        if (GetAboutLanguage() == GameLanguage.Korean && profileKoreanFont != null &&
            (koreanInput || ContainsKorean(value)))
            label.font = profileKoreanFont;
        else
            label.font = inspectorFont;
    }
}
