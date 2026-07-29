# Gotblin Project UI Font Guidelines

Gotblin 프로젝트의 모든 인게임 UI 텍스트 및 앞으로 생성될 모든 UI 텍스트 컴포넌트에는 일관된 비주얼 아이덴티티 유지를 위해 **`NeoDunggeunmoPro-Regular SDF`** 폰트 에셋을 표준 폰트로 적용합니다.

---

## 📌 기본 폰트 사양 (Font Specification)

* **표준 폰트 이름**: `NeoDunggeunmoPro-Regular SDF`
* **폰트 에셋 경로**: `Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular SDF.asset`
* **폰트 에셋 GUID**: `e00828f873d75d246aa91b57c3aa1fca`
* **기본 마테리얼 FileID**: `-6445253934987609687`

---

## ⚙️ 프로젝트 전역 설정 (Global Project Settings)

1. **TMP Settings (기본 폰트 설정)**:
   - 경로: `Assets/TextMesh Pro/Resources/TMP Settings.asset`
   - `m_defaultFontAsset`이 `NeoDunggeunmoPro-Regular SDF` (guid: `e00828f873d75d246aa91b57c3aa1fca`)로 지정되어 있으므로, 씬 에디터에서 새로 생성하는 모든 TextMeshPro / TextMeshProUGUI 요소는 자동으로 기본 적용됩니다.

2. **기존 프리팹 및 씬 일괄 변경**:
   - `SampleScene.unity` 및 모든 UI 프리팹(`UI_BlacksmithPanel.prefab`, `UI_BlacksmithSlot.prefab`, `BuildingSlot_Template.prefab` 등)의 fontAsset 및 sharedMaterial 참조가 `NeoDunggeunmoPro-Regular SDF`로 교체되었습니다.

---

## 🛠️ 신규 UI 개발 및 C# 코드 작성 규칙 (Development Rules)

### 1. 씬 에디터 UI 작업 시
- 새로운 텍스트 개체를 생성할 때는 반드시 TextMeshPro (`TextMeshProUGUI`) 컴포넌트를 사용합니다.
- `Font Asset` 항목이 `NeoDunggeunmoPro-Regular SDF`로 지정되어 있는지 확인합니다.

### 2. C# 스크립트로 폰트를 동적 할당할 때
- 스크립트 상에서 폰트를 동적으로 지정하거나 폴백해야 할 경우, 기본 폰트나 `NeoDunggeunmoPro-Regular SDF`를 로드하여 할당합니다.
```csharp
// 1. TMP_Settings의 기본 폰트 참조
TMP_FontAsset defaultFont = TMPro.TMP_Settings.defaultFontAsset;

// 2. Resources/Fonts 경로에서 직접 로드 시
TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("NeoDunggeunmoPro-Regular SDF");
```

---

## 📝 관리 및 유지보수

본 문서 규칙은 UI 텍스트 일관성을 유지하기 위한 것이므로, 팀원 또는 AI 에이전트가 새로운 UI 스크립트나 프리팹을 제작할 때 반드시 이 가이드를 준수합니다.
