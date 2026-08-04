#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor 상단 메뉴바(Gotblin > Cheat Tools)를 통해 플레이모드 중 치트 기능을 즉시 실행할 수 있는 에디터 확장 클래스입니다.
/// </summary>
public static class CheatMenuTools
{
    [MenuItem("Gotblin/Cheat Tools/⚔️ Add All Weapons", false, 1)]
    public static void AddAllWeapons()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("치트 안내", "치트 기능은 게임 플레이 모드(Play Mode) 실행 중일 때 작동합니다.", "확인");
            return;
        }

        if (BlacksmithManager.Instance != null)
        {
            BlacksmithManager.Instance.AddAllWeaponsCheat();
        }
        else
        {
            Debug.LogWarning("[CheatTool] BlacksmithManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    [MenuItem("Gotblin/Cheat Tools/💎 Add All Gems", false, 2)]
    public static void AddAllGems()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("치트 안내", "치트 기능은 게임 플레이 모드(Play Mode) 실행 중일 때 작동합니다.", "확인");
            return;
        }

        if (BlacksmithManager.Instance != null)
        {
            BlacksmithManager.Instance.AddAllGemsCheat();
        }
        else
        {
            Debug.LogWarning("[CheatTool] BlacksmithManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    [MenuItem("Gotblin/Cheat Tools/💰 Add 100,000 Gold", false, 3)]
    public static void AddGold()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("치트 안내", "치트 기능은 게임 플레이 모드(Play Mode) 실행 중일 때 작동합니다.", "확인");
            return;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(100000);
            UI_ToastPopup.Show("+100,000 Gold 획득!");
        }
    }
}
#endif
