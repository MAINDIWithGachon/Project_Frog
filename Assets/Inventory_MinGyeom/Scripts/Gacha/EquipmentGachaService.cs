using System.Collections.Generic;
using UnityEngine;

public class EquipmentGachaService : MonoBehaviour
{
    [SerializeField] private EquipmentGachaSettings settings;
    [SerializeField] private EquipmentPrototypeState equipmentState;
    [SerializeField] private RuntimeData runtimeData;

    private static readonly EquipmentRarity[] RarityOrder =
    {
        EquipmentRarity.Common,
        EquipmentRarity.Magic,
        EquipmentRarity.Rare,
        EquipmentRarity.Epic,
        EquipmentRarity.Legendary
    };

    public EquipmentGachaSettings Settings => settings;
    public EquipmentPrototypeState EquipmentState => equipmentState;
    public RuntimeData RuntimeData => runtimeData;
    public EquipmentDatabase EquipmentDatabase => equipmentState != null ? equipmentState.EquipmentDatabase : null;

    public int GetTotalCost(int drawCount)
    {
        if (settings == null || drawCount <= 0)
        {
            return 0;
        }

        return settings.drawCost * drawCount;
    }

    public bool CanDraw(int drawCount)
    {
        if (drawCount <= 0)
        {
            Debug.LogWarning("[EquipmentGachaService] Draw count must be greater than zero.", this);
            return false;
        }

        if (settings == null)
        {
            Debug.LogWarning("[EquipmentGachaService] Settings are not assigned.", this);
            return false;
        }

        if (equipmentState == null)
        {
            Debug.LogWarning("[EquipmentGachaService] EquipmentPrototypeState is not assigned.", this);
            return false;
        }

        if (runtimeData == null)
        {
            Debug.LogWarning("[EquipmentGachaService] RuntimeData is not assigned.", this);
            return false;
        }

        EquipmentDatabase database = equipmentState.EquipmentDatabase;
        if (database == null)
        {
            Debug.LogWarning("[EquipmentGachaService] EquipmentDatabase is not assigned.", this);
            return false;
        }

        if (!settings.IsValidRate())
        {
            Debug.LogWarning($"[EquipmentGachaService] Invalid rarity total rate: {settings.TotalRate}.", this);
            return false;
        }

        for (int i = 0; i < RarityOrder.Length; i++)
        {
            EquipmentRarity rarity = RarityOrder[i];
            if (settings.GetRate(rarity) <= 0f)
            {
                continue;
            }

            if (GetGachaCandidates(rarity).Count == 0)
            {
                Debug.LogWarning($"[EquipmentGachaService] Rarity '{rarity}' has a configured rate but no enabled equipment.", this);
                return false;
            }
        }

        return runtimeData.GetGold() >= GetTotalCost(drawCount);
    }

    public bool TryDraw(int drawCount, out List<EquipmentGachaResult> results)
    {
        results = new List<EquipmentGachaResult>();

        if (!CanDraw(drawCount))
        {
            return false;
        }

        int totalCost = GetTotalCost(drawCount);
        if (!runtimeData.SpendGold(totalCost))
        {
            Debug.LogWarning("[EquipmentGachaService] Not enough gold to draw.", this);
            return false;
        }

        for (int i = 0; i < drawCount; i++)
        {
            if (!TryDrawSingle(i, out EquipmentGachaResult result))
            {
                runtimeData.AddGold(totalCost);
                for (int refundIndex = 0; refundIndex < results.Count; refundIndex++)
                {
                    EquipmentGachaResult grantedResult = results[refundIndex];
                    if (grantedResult?.definition == null)
                    {
                        continue;
                    }

                    equipmentState.TryConsumeOwnedEquipmentCopies(grantedResult.definition.equipmentId, 1);
                }

                results.Clear();
                Debug.LogWarning("[EquipmentGachaService] Failed during multi-draw. Refunded draw cost and reverted granted copies.", this);
                return false;
            }

            results.Add(result);
        }

        return true;
    }

    private bool TryDrawSingle(int drawIndex, out EquipmentGachaResult result)
    {
        result = null;

        EquipmentRarity drawnRarity = RollRarity();
        List<EquipmentDefinitionData> candidates = GetGachaCandidates(drawnRarity);
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[EquipmentGachaService] No gacha candidates found for rarity '{drawnRarity}'.", this);
            return false;
        }

        EquipmentDefinitionData selectedDefinition = candidates[Random.Range(0, candidates.Count)];
        if (selectedDefinition == null || !equipmentState.TryAddOwnedEquipment(selectedDefinition.equipmentId))
        {
            Debug.LogWarning("[EquipmentGachaService] Failed to grant drawn equipment.", this);
            return false;
        }

        result = new EquipmentGachaResult
        {
            drawIndex = drawIndex,
            equipmentId = selectedDefinition.equipmentId,
            displayName = selectedDefinition.displayName,
            rarity = selectedDefinition.rarity,
            drawCost = settings.drawCost,
            currentLevel = equipmentState.GetOwnedLevel(selectedDefinition.equipmentId),
            currentOwnedCount = equipmentState.GetOwnedCount(selectedDefinition.equipmentId),
            definition = selectedDefinition
        };

        return true;
    }

    private EquipmentRarity RollRarity()
    {
        float roll = Random.Range(0f, settings.TotalRate);
        float cumulative = 0f;

        for (int i = 0; i < RarityOrder.Length; i++)
        {
            EquipmentRarity rarity = RarityOrder[i];
            cumulative += settings.GetRate(rarity);

            if (roll <= cumulative)
            {
                return rarity;
            }
        }

        return EquipmentRarity.Legendary;
    }

    private List<EquipmentDefinitionData> GetGachaCandidates(EquipmentRarity rarity)
    {
        List<EquipmentDefinitionData> candidates = new();
        EquipmentDatabase database = equipmentState != null ? equipmentState.EquipmentDatabase : null;
        if (database == null)
        {
            return candidates;
        }

        IReadOnlyList<EquipmentDefinitionData> definitions = database.EquipmentDefinitions;
        for (int i = 0; i < definitions.Count; i++)
        {
            EquipmentDefinitionData definition = definitions[i];
            if (definition == null || !definition.isGachaEnabled || definition.rarity != rarity)
            {
                continue;
            }

            candidates.Add(definition);
        }

        return candidates;
    }
}
