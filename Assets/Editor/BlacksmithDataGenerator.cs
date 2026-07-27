#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 대장간 무기(단검, 양손검, 전투도끼 1~3단계) 및 보석(루비, 에메랄드, 다이아몬드 1~3단계)
/// 등급별 2배 이상 스케일링 밸런스 데이터 에셋을 자동으로 생성 및 상호 연동하는 에디터 헬퍼입니다.
/// </summary>
public static class BlacksmithDataGenerator
{
    [MenuItem("Gotblin/Generate Blacksmith Sample Data Assets")]
    public static void GenerateAllDataAssets()
    {
        string weaponDir = "Assets/Resources/Data/Weapons";
        string gemDir = "Assets/Resources/Data/Gems";

        EnsureDirectory(weaponDir);
        EnsureDirectory(gemDir);

        // 1. 단검 계열 생성 (공속/이속 중심, 스탯 2.3~2.5배 스케일링, 성공:실패:파괴 = 3:1:1)
        var d1 = CreateWeaponAsset(weaponDir, "Dagger_Lv1", "dagger_1", "수습 단검", "단검", 1, 15, 5,
            new List<ItemOption> {
                new ItemOption { optionType = WeaponOptionType.AttackSpeed, value = 0.2f },
                new ItemOption { optionType = WeaponOptionType.MoveSpeed, value = 10f }
            });

        var d2 = CreateWeaponAsset(weaponDir, "Dagger_Lv2", "dagger_2", "은빛 단검", "단검", 2, 35, 15,
            new List<ItemOption> {
                new ItemOption { optionType = WeaponOptionType.AttackSpeed, value = 0.5f },
                new ItemOption { optionType = WeaponOptionType.MoveSpeed, value = 25f }
            });

        var d3 = CreateWeaponAsset(weaponDir, "Dagger_Lv3", "dagger_3", "그림자 단검", "단검", 3, 90, 45,
            new List<ItemOption> {
                new ItemOption { optionType = WeaponOptionType.AttackSpeed, value = 1.2f },
                new ItemOption { optionType = WeaponOptionType.MoveSpeed, value = 60f }
            });

        d1.nextGradeWeapon = d2;
        d2.nextGradeWeapon = d3;
        EditorUtility.SetDirty(d1);
        EditorUtility.SetDirty(d2);

        // 2. 양손검 계열 생성 (공격력%/타겟수 중심, 스탯 2.3~2.5배 스케일링, 성공:실패:파괴 = 3:1:1)
        var s1 = CreateWeaponAsset(weaponDir, "Sword_Lv1", "sword_1", "강철검 1단계", "양손검", 1, 30, 8,
            new List<ItemOption> {
                new ItemOption { optionType = WeaponOptionType.ATKPercent, value = 0.10f }
            });

        var s2 = CreateWeaponAsset(weaponDir, "Sword_Lv2", "sword_2", "명검 2단계", "양손검", 2, 70, 20,
            new List<ItemOption> {
                new ItemOption { optionType = WeaponOptionType.ATKPercent, value = 0.25f },
                new ItemOption { optionType = WeaponOptionType.TargetCount, value = 1f }
            });

        var s3 = CreateWeaponAsset(weaponDir, "Sword_Lv3", "sword_3", "용살검 3단계", "양손검", 3, 160, 60,
            new List<ItemOption> {
                new ItemOption { optionType = WeaponOptionType.ATKPercent, value = 0.60f },
                new ItemOption { optionType = WeaponOptionType.TargetCount, value = 2f }
            });

        s1.nextGradeWeapon = s2;
        s2.nextGradeWeapon = s3;
        EditorUtility.SetDirty(s1);
        EditorUtility.SetDirty(s2);

        // 3. 전투도끼 계열 생성 (고공격력/생명력% 중심, 스탯 2.3~2.5배 스케일링, 성공:실패:파괴 = 3:1:1)
        var a1 = CreateWeaponAsset(weaponDir, "Axe_Lv1", "axe_1", "벌목도끼", "전투도끼", 1, 50, 10,
            new List<ItemOption> {
                new ItemOption { optionType = WeaponOptionType.HPPercent, value = 0.10f }
            });

        var a2 = CreateWeaponAsset(weaponDir, "Axe_Lv2", "axe_2", "전투도끼", "전투도끼", 2, 120, 25,
            new List<ItemOption> {
                new ItemOption { optionType = WeaponOptionType.HPPercent, value = 0.25f },
                new ItemOption { optionType = WeaponOptionType.TargetCount, value = 1f }
            });

        var a3 = CreateWeaponAsset(weaponDir, "Axe_Lv3", "axe_3", "파괴의 광도끼", "전투도끼", 3, 280, 75,
            new List<ItemOption> {
                new ItemOption { optionType = WeaponOptionType.HPPercent, value = 0.60f },
                new ItemOption { optionType = WeaponOptionType.TargetCount, value = 2f }
            });

        a1.nextGradeWeapon = a2;
        a2.nextGradeWeapon = a3;
        EditorUtility.SetDirty(a1);
        EditorUtility.SetDirty(a2);

        // 4. 보석 에셋 생성 (루비, 에메랄드, 다이아몬드 - 성공:실패:파괴 = 3:1:1)
        var r1 = CreateGemAsset(gemDir, "Ruby_Lv1", "ruby_1", "루비 1단계", 1, 500);
        var r2 = CreateGemAsset(gemDir, "Ruby_Lv2", "ruby_2", "루비 2단계", 2, 1200);
        var r3 = CreateGemAsset(gemDir, "Ruby_Lv3", "ruby_3", "빛나는 루비 3단계", 3, 3000);
        r1.nextLevelGem = r2;
        r2.nextLevelGem = r3;
        EditorUtility.SetDirty(r1);
        EditorUtility.SetDirty(r2);

        var e1 = CreateGemAsset(gemDir, "Emerald_Lv1", "emerald_1", "에메랄드 1단계", 1, 800);
        var e2 = CreateGemAsset(gemDir, "Emerald_Lv2", "emerald_2", "에메랄드 2단계", 2, 2000);
        var e3 = CreateGemAsset(gemDir, "Emerald_Lv3", "emerald_3", "영롱한 에메랄드 3단계", 3, 5000);
        e1.nextLevelGem = e2;
        e2.nextLevelGem = e3;
        EditorUtility.SetDirty(e1);
        EditorUtility.SetDirty(e2);

        var dm1 = CreateGemAsset(gemDir, "Diamond_Lv1", "diamond_1", "다이아몬드 1단계", 1, 2000);
        var dm2 = CreateGemAsset(gemDir, "Diamond_Lv2", "diamond_2", "다이아몬드 2단계", 2, 5000);
        var dm3 = CreateGemAsset(gemDir, "Diamond_Lv3", "diamond_3", "찬란한 다이아몬드 3단계", 3, 12000);
        dm1.nextLevelGem = dm2;
        dm2.nextLevelGem = dm3;
        EditorUtility.SetDirty(dm1);
        EditorUtility.SetDirty(dm2);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[BlacksmithDataGenerator] 총 9종의 무기 에셋과 9종의 보석 에셋 생성을 성공적으로 완료하였습니다!</color>");
    }

    private static WeaponItemData CreateWeaponAsset(string dir, string assetName, string id, string name, string type, int grade, int baseAtk, int iron, List<ItemOption> options, int succW = 3, int keepW = 1, int destW = 1)
    {
        string path = $"{dir}/{assetName}.asset";
        WeaponItemData asset = AssetDatabase.LoadAssetAtPath<WeaponItemData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<WeaponItemData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.weaponID = id;
        asset.weaponName = name;
        asset.weaponType = type;
        asset.grade = grade;
        asset.baseATK = baseAtk;
        asset.successWeight = succW;
        asset.keepWeight = keepW;
        asset.destroyWeight = destW;
        asset.requiredIronIngot = iron;
        asset.additionalOptions = options;

        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static GemItemData CreateGemAsset(string dir, string assetName, string id, string name, int level, int sellPrice, int succW = 3, int keepW = 1, int destW = 1)
    {
        string path = $"{dir}/{assetName}.asset";
        GemItemData asset = AssetDatabase.LoadAssetAtPath<GemItemData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<GemItemData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.gemID = id;
        asset.gemName = name;
        asset.level = level;
        asset.sellPrice = sellPrice;
        asset.successWeight = succW;
        asset.keepWeight = keepW;
        asset.destroyWeight = destW;

        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
