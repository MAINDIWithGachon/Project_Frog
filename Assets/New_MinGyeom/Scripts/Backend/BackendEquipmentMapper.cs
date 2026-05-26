using System;
using System.Collections.Generic;
using NewMinGyeom.Equipment;
using UnityEngine;

namespace NewMinGyeom.Backend
{
    public static class BackendEquipmentMapper
    {
        public static EquipmentDefinition ToDefinition(BackendEquipmentDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new EquipmentDefinition
            {
                equipmentId = dto.equipmentId,
                displayName = dto.displayName,
                slotType = ParseEnum(dto.slotType, EquipmentSlotType.Weapon),
                grade = ParseEnum(dto.grade, EquipmentGrade.Common),
                iconKey = dto.iconKey,
                description = dto.description,
                maxLevel = Mathf.Max(1, dto.maxLevel),
                isGachaEnabled = dto.isGachaEnabled,
                stats = new EquipmentStatBlock
                {
                    attack = dto.attack,
                    hp = dto.hp,
                    hpRegen = dto.hpRegen,
                    critChance = dto.critChance,
                    critDamage = dto.critDamage
                }
            };
        }

        public static List<EquipmentDefinition> ToDefinitions(IEnumerable<BackendEquipmentDto> dtos)
        {
            List<EquipmentDefinition> results = new();
            if (dtos == null)
            {
                return results;
            }

            foreach (BackendEquipmentDto dto in dtos)
            {
                EquipmentDefinition definition = ToDefinition(dto);
                if (definition == null || string.IsNullOrWhiteSpace(definition.equipmentId))
                {
                    continue;
                }

                results.Add(definition);
            }

            return results;
        }

        public static EquipmentInstance ToInstance(BackendOwnedEquipmentDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new EquipmentInstance(dto.equipmentId, dto.level, dto.ownedCount);
        }

        public static List<EquipmentInstance> ToInstances(IEnumerable<BackendOwnedEquipmentDto> dtos)
        {
            List<EquipmentInstance> results = new();
            if (dtos == null)
            {
                return results;
            }

            foreach (BackendOwnedEquipmentDto dto in dtos)
            {
                EquipmentInstance instance = ToInstance(dto);
                if (instance == null || string.IsNullOrWhiteSpace(instance.equipmentId))
                {
                    continue;
                }

                results.Add(instance);
            }

            return results;
        }

        public static EquipmentSlotType ToSlotType(string value)
        {
            return ParseEnum(value, EquipmentSlotType.Weapon);
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
        }
    }
}
