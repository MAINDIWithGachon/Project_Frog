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

    public bool TryDraw(out EquipmentGachaResult result)
    {
        result = null;

        if (!CanDraw())
        {
            return false;
        }

        if (!runtimeData.SpendGold(settings.drawCost))
        {
            Debug.LogWarning("[EquipmentGachaService] Not enough gold to draw.", this);
            return false;
        }

        EquipmentRarity drawnRarity = RollRarity();
        List<EquipmentDefinitionData> candidates = GetGachaCandidates(drawnRarity);

        if (candidates.Count == 0)
        {
            runtimeData.AddGold(settings.drawCost);
            Debug.LogWarning($"[EquipmentGachaService] No gacha candidates found for rarity '{drawnRarity}'. Refunded draw cost.", this);
            return false;
        }

        EquipmentDefinitionData selectedDefinition = candidates[Random.Range(0, candidates.Count)];
        if (selectedDefinition == null || !equipmentState.TryAddOwnedEquipment(selectedDefinition.equipmentId))
        {
            runtimeData.AddGold(settings.drawCost);
            Debug.LogWarning("[EquipmentGachaService] Failed to grant drawn equipment. Refunded draw cost.", this);
            return false;
        }

        result = new EquipmentGachaResult
        {
            equipmentId = selectedDefinition.equipmentId,
            displayName = selectedDefinition.displayName,
            rarity = selectedDefinition.rarity,
            drawCost = settings.drawCost,
            definition = selectedDefinition
        };

        return true;
    }

    public bool CanDraw()
    {
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

        return runtimeData.GetGold() >= settings.drawCost;
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
