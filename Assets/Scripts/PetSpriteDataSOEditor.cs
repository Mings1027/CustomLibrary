using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(PetSpriteDataSO))]
public class PetSpriteDataSOEditor : Editor
{
    private DefaultAsset spriteFolder; // Inspector에서 폴더 드래그
    private string prefix = "Pet"; // 기본 prefix

    [Tooltip("속성별 펫 수")] private int petsPerElement = 6; // 속성별 펫 수
    [Tooltip("펫별 진화 단계")] private int evolutionCount = 5;

    // Foldout 상태 저장
    private bool[] elementFoldouts = new bool[6];
    private bool[][] petFoldouts = new bool[6][];

    private void OnEnable()
    {
        for (int i = 0; i < 6; i++)
            petFoldouts[i] = new bool[6]; // 각 속성별 펫 Foldout 초기화
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("자동 Sprite 세팅", EditorStyles.boldLabel);

        spriteFolder =
            (DefaultAsset)EditorGUILayout.ObjectField("Sprite Folder", spriteFolder, typeof(DefaultAsset), false);
        prefix = EditorGUILayout.TextField("Sprite Prefix", prefix);
        petsPerElement = EditorGUILayout.IntField("속성별 펫 수", petsPerElement);
        evolutionCount = EditorGUILayout.IntField("펫별 진화 단계", evolutionCount);
        if (GUILayout.Button("자동 배열 채우기"))
        {
            if (spriteFolder == null)
            {
                Debug.LogWarning("폴더를 선택해주세요.");
                return;
            }

            AutoFillSprites(spriteFolder);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("속성별 펫 정보", EditorStyles.boldLabel);

        DrawElementFoldouts(); // ← Foldout 그리는 함수 호출
        // DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
    }

    private void AutoFillSprites(DefaultAsset spriteFolder)
    {
        var so = target as PetSpriteDataSO;
        if (so == null) return;

        if (spriteFolder == null)
        {
            Debug.LogWarning("폴더를 선택해주세요.");
            return;
        }

        // 1. 폴더 경로 가져오기
        string folderPath = AssetDatabase.GetAssetPath(spriteFolder);

        // 2. 폴더 안 모든 Sprite GUID 가져오기 (prefix 포함)
        string[] guids = AssetDatabase.FindAssets(prefix, new[] { folderPath });

        // 3. GUID → Sprite 로딩
        var sprites = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(s => s != null)
            .OrderBy(s => ExtractNumberFromName(s.name))
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning("폴더 안에 Sprite가 없습니다. prefix와 시작 이름을 확인하세요.");
            return;
        }

        // 4. elementPetSprites 배열 초기화
        var elementArrayField = typeof(PetSpriteDataSO)
            .GetField("elementPetSprites",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var elementTypes = (ElementType[])System.Enum.GetValues(typeof(ElementType));
        ElementPetSprites[] elementArray = new ElementPetSprites[elementTypes.Length];

        elementArrayField.SetValue(so, elementArray);

        // 5. 속성별, 펫별, 진화 단계별 Sprite 채우기
        for (int e = 0; e < elementTypes.Length; e++)
        {
            ElementPetSprites elementData = new ElementPetSprites();
            PetEvolutionSprites[] pets = new PetEvolutionSprites[elementTypes.Length];

            for (int p = 0; p < petsPerElement; p++)
            {
                PetEvolutionSprites pet = new PetEvolutionSprites();
                Sprite[] evolutions = new Sprite[evolutionCount];

                for (int u = 0; u < evolutions.Length; u++)
                {
                    int spriteIndex = e * petsPerElement * 5 + p * 5 + u;
                    evolutions[u] = spriteIndex < sprites.Length ? sprites[spriteIndex] : null;
                }

                // Sprite 배열 세팅
                var spriteField = typeof(PetEvolutionSprites)
                    .GetField("sprites",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                spriteField.SetValue(pet, evolutions);

                // petName 자동 지정
                var nameField = typeof(PetEvolutionSprites)
                    .GetField("petName",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                nameField.SetValue(pet, $"{prefix}_{e * 6 + p}");

                pets[p] = pet;
            }

            // pets 배열 세팅
            var petsField = typeof(ElementPetSprites)
                .GetField("pets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            petsField.SetValue(elementData, pets);

            // elementType enum 순서대로 세팅
            var elementTypeField = typeof(ElementPetSprites)
                .GetField("elementType",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            elementTypeField.SetValue(elementData, (ElementType)e);

            elementArray[e] = elementData;
        }

        // 6. 저장
        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        Debug.Log($"자동 배열 채우기 완료! 총 {sprites.Length}개 Sprite가 순서대로 배치되었습니다.");
    }

// Sprite 이름에서 숫자 추출 (Pet001 -> 1)
    private int ExtractNumberFromName(string name)
    {
        string digits = new string(name.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int n) ? n : 0;
    }

    private void DrawElementFoldouts()
    {
        var so = target as PetSpriteDataSO;
        if (so == null) return;

        var elementArrayField = typeof(PetSpriteDataSO)
            .GetField("elementPetSprites",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ElementPetSprites[] elementArray = (ElementPetSprites[])elementArrayField.GetValue(so);

        if (elementArray == null) return;

        for (int e = 0; e < elementArray.Length; e++)
        {
            var elementData = elementArray[e];
            if (elementData == null) continue;

            elementFoldouts[e] =
                EditorGUILayout.Foldout(elementFoldouts[e], $"Element: {elementData.ElementType}", true);
            if (!elementFoldouts[e]) continue;

            var petsField = typeof(ElementPetSprites)
                .GetField("pets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            PetEvolutionSprites[] pets = (PetEvolutionSprites[])petsField.GetValue(elementData);

            if (pets == null) continue;

            EditorGUI.indentLevel++;
            for (int p = 0; p < pets.Length; p++)
            {
                var pet = pets[p];
                if (pet == null) continue;

                petFoldouts[e][p] = EditorGUILayout.Foldout(petFoldouts[e][p], $"Pet: {prefix}_{e * 6 + p}", true);
                if (!petFoldouts[e][p]) continue;

                var spriteField = typeof(PetEvolutionSprites)
                    .GetField("sprites",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Sprite[] evolutions = (Sprite[])spriteField.GetValue(pet);

                EditorGUI.indentLevel++;
                if (evolutions != null)
                {
                    for (int u = 0; u < evolutions.Length; u++)
                    {
                        // 배열 원소처럼 한 줄에 라벨 + ObjectField
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Evolution {u}", GUILayout.Width(70));
                        evolutions[u] = (Sprite)EditorGUILayout.ObjectField(evolutions[u], typeof(Sprite), false);
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }
    }
}