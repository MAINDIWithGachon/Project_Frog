using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 장비의 고정 데이터를 보관하는 ScriptableObject 데이터베이스입니다.
/// </summary>
[CreateAssetMenu(
    fileName = "EquipmentDatabase",
    menuName = "Project Frog/Equipment/Equipment Database")]
public class EquipmentDatabase : ScriptableObject
{
    // 인스펙터에서 관리하는 장비 목록이며, 런타임에서는 ID 또는 카테고리 기준으로 조회합니다.
    [SerializeField] private List<EquipmentDefinitionData> equipmentDefinitions = new();

    public IReadOnlyList<EquipmentDefinitionData> EquipmentDefinitions => equipmentDefinitions;

    public bool TryGetById(string equipmentId, out EquipmentDefinitionData definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            return false;
        }

        for (int i = 0; i < equipmentDefinitions.Count; i++)
        {
            EquipmentDefinitionData candidate = equipmentDefinitions[i];
            if (candidate == null || candidate.equipmentId != equipmentId)
            {
                continue;
            }

            definition = candidate;
            return true;
        }

        return false;
    }

    public List<EquipmentDefinitionData> GetByCategory(EquipmentCategory category)
    {
        // 원본 에셋 데이터를 직접 건드리지 않도록 조회 결과는 새 리스트로 만들어 반환합니다.
        List<EquipmentDefinitionData> results = new();

        for (int i = 0; i < equipmentDefinitions.Count; i++)
        {
            EquipmentDefinitionData definition = equipmentDefinitions[i];
            if (definition == null || definition.category != category)
            {
                continue;
            }

            results.Add(definition);
        }

        return results;
    }
}
