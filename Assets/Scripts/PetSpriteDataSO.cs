using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum ElementType
{
    Fire,
    Water,
    Wind,
    Earth,
    Light,
    Dark
}

[System.Serializable]
public class PetEvolutionSprites
{
    [SerializeField] private string petName;

    [Tooltip("펫의 각 진화단계별 스프라이트 (0~4 업그레이드 단계)")] [SerializeField]
    private Sprite[] sprites = new Sprite[5];

    public int SpriteLength => sprites.Length;

    public void SetSprite(int index, Sprite sprite)
    {
        if (index < 0 || index >= sprites.Length) return;
        sprites[index] = sprite;
    }

    public Sprite GetSprite(int index)
    {
        if (index < 0 || index >= sprites.Length) return null;
        return sprites[index];
    }
}

[System.Serializable]
public class ElementPetSprites
{
    [Tooltip("속성 종류")] [SerializeField] private ElementType elementType;

    [Tooltip("이 속성에 포함된 펫들의 스프라이트 (펫 6마리)")] [SerializeField]
    private PetEvolutionSprites[] pets = new PetEvolutionSprites[6];

    public ElementType ElementType => elementType;
    public int PetLength => pets.Length;

    public PetEvolutionSprites GetPet(int index)
    {
        if (index < 0 || index >= pets.Length) return null;
        return pets[index];
    }
}

[CreateAssetMenu(fileName = "PetSpriteDataSO", menuName = "Game Data/Pet Sprite Data")]
public class PetSpriteDataSO : ScriptableObject
{
    [Tooltip("모든 속성의 펫 스프라이트 데이터")] [SerializeField]
    private ElementPetSprites[] elementPetSprites = new ElementPetSprites[6];

    private Dictionary<ElementType, ElementPetSprites> _dict;

    private void Initialize()
    {
        if (_dict != null) return;

        _dict = new Dictionary<ElementType, ElementPetSprites>();
        foreach (var elementData in elementPetSprites)
        {
            if (!_dict.ContainsKey(elementData.ElementType))
                _dict.Add(elementData.ElementType, elementData);
        }
    }

    /// <summary>
    /// 펫 고유번호(0~179)로 현재 단계 스프라이트 가져오기
    /// </summary>
    public Sprite GetSpriteByPetNum(int petNum)
    {
        Initialize();

        if (petNum < 0 || petNum >= 180)
            return null;

        int elementIndex = petNum / 30; // 속성 구분 (0~5)
        int petIndex = (petNum % 30) / 5; // 해당 속성 내 펫 번호 (0~5)
        int upgradeIndex = petNum % 5; // 진화단계 (0~4)

        ElementType elementType = (ElementType)elementIndex;

        if (_dict.TryGetValue(elementType, out var elementData))
        {
            if (petIndex < 0 || petIndex >= elementData.PetLength) return null;
            var pet = elementData.GetPet(petIndex);
            if (upgradeIndex < 0 || upgradeIndex >= pet.SpriteLength) return null;
            return pet.GetSprite(upgradeIndex);
        }

        return null;
    }

    /// <summary>
    /// 펫 고유번호로 마지막 진화단계 스프라이트 가져오기
    /// </summary>
    public Sprite GetFinalSpriteByPetNum(int petNum)
    {
        Initialize();

        if (petNum < 0 || petNum >= 180)
            return null;

        int elementIndex = petNum / 30; // 속성
        int petIndex = (petNum % 30) / 5; // 해당 속성 내 펫 번호
        ElementType elementType = (ElementType)elementIndex;

        if (_dict.TryGetValue(elementType, out var elementData))
        {
            if (petIndex < 0 || petIndex >= elementData.PetLength) return null;
            var pet = elementData.GetPet(petIndex);
            return pet.SpriteLength > 0 ? pet.GetSprite(pet.SpriteLength - 1) : null;
        }

        return null;
    }
}