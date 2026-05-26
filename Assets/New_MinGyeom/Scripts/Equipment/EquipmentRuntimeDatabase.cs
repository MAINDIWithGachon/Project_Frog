using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewMinGyeom.Equipment
{
    [Serializable]
    public class EquipmentRuntimeDatabase
    {
        [SerializeField] private List<EquipmentDefinition> definitions = new();

        private readonly Dictionary<string, EquipmentDefinition> definitionById = new();

        public IReadOnlyList<EquipmentDefinition> Definitions => definitions;
        public EquipmentDatabaseLoadState LoadState { get; private set; } = EquipmentDatabaseLoadState.Empty;
        public bool IsReady => LoadState == EquipmentDatabaseLoadState.Ready;

        public void SetDefinitions(IEnumerable<EquipmentDefinition> newDefinitions)
        {
            definitions.Clear();
            definitionById.Clear();

            if (newDefinitions != null)
            {
                foreach (EquipmentDefinition definition in newDefinitions)
                {
                    if (definition == null || string.IsNullOrWhiteSpace(definition.equipmentId))
                    {
                        continue;
                    }

                    definitions.Add(definition);
                    definitionById[definition.equipmentId] = definition;
                }
            }

            LoadState = definitions.Count > 0 ? EquipmentDatabaseLoadState.Ready : EquipmentDatabaseLoadState.Empty;
        }

        public void SetLoading()
        {
            LoadState = EquipmentDatabaseLoadState.Loading;
        }

        public void SetEmpty()
        {
            definitions.Clear();
            definitionById.Clear();
            LoadState = EquipmentDatabaseLoadState.Empty;
        }

        public void SetFailed()
        {
            LoadState = EquipmentDatabaseLoadState.Failed;
        }

        public bool TryGetDefinition(string equipmentId, out EquipmentDefinition definition)
        {
            EnsureLookup();
            return definitionById.TryGetValue(equipmentId, out definition);
        }

        public EquipmentDefinition GetDefinition(string equipmentId)
        {
            return TryGetDefinition(equipmentId, out EquipmentDefinition definition) ? definition : null;
        }

        public List<EquipmentDefinition> GetBySlotType(EquipmentSlotType slotType)
        {
            List<EquipmentDefinition> results = new();

            for (int i = 0; i < definitions.Count; i++)
            {
                EquipmentDefinition definition = definitions[i];
                if (definition == null || definition.slotType != slotType)
                {
                    continue;
                }

                results.Add(definition);
            }

            return results;
        }

        private void EnsureLookup()
        {
            if (definitionById.Count == definitions.Count)
            {
                return;
            }

            definitionById.Clear();
            foreach (EquipmentDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.equipmentId))
                {
                    continue;
                }

                definitionById[definition.equipmentId] = definition;
            }
        }
    }
}
