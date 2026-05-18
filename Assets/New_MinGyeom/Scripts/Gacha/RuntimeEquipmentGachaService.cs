using System.Collections.Generic;
using NewMinGyeom.Equipment;
using UnityEngine;

namespace NewMinGyeom.Gacha
{
    public class RuntimeEquipmentGachaService : MonoBehaviour
    {
        [SerializeField] private EquipmentGachaSettings settings;
        [SerializeField] private EquipmentRuntimeState equipmentState;
        [SerializeField] private RuntimeData runtimeData;

        private static readonly EquipmentGrade[] GradeOrder =
        {
            EquipmentGrade.Common,
            EquipmentGrade.Magic,
            EquipmentGrade.Rare,
            EquipmentGrade.Epic,
            EquipmentGrade.Legendary
        };

        public EquipmentGachaSettings Settings => settings;
        public EquipmentRuntimeState EquipmentState => equipmentState;
        public RuntimeData RuntimeData => runtimeData;
        public EquipmentRuntimeDatabase RuntimeDatabase => equipmentState != null ? equipmentState.Database : null;

        public void Configure(
            EquipmentGachaSettings gachaSettings,
            EquipmentRuntimeState state,
            RuntimeData data)
        {
            if (gachaSettings != null)
            {
                settings = gachaSettings;
            }

            equipmentState = state != null ? state : equipmentState;
            runtimeData = data != null ? data : runtimeData;
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

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
            ResolveReferences();

            if (drawCount <= 0)
            {
                Debug.LogWarning("[RuntimeEquipmentGachaService] Draw count must be greater than zero.", this);
                return false;
            }

            if (settings == null)
            {
                Debug.LogWarning("[RuntimeEquipmentGachaService] Settings are not assigned.", this);
                return false;
            }

            if (equipmentState == null)
            {
                Debug.LogWarning("[RuntimeEquipmentGachaService] EquipmentRuntimeState is not assigned.", this);
                return false;
            }

            if (runtimeData == null)
            {
                Debug.LogWarning("[RuntimeEquipmentGachaService] RuntimeData is not assigned.", this);
                return false;
            }

            if (equipmentState.LoadState != EquipmentDatabaseLoadState.Ready ||
                equipmentState.Database == null ||
                equipmentState.Database.Definitions.Count == 0)
            {
                Debug.LogWarning("[RuntimeEquipmentGachaService] 장비 DB가 비어 있어서 뽑을 장비가 없습니다. 먼저 장비 DB 로드 버튼을 눌러 주세요.", this);
                return false;
            }

            if (!settings.IsValidRate())
            {
                Debug.LogWarning($"[RuntimeEquipmentGachaService] Invalid grade total rate: {settings.TotalRate}.", this);
                return false;
            }

            for (int i = 0; i < GradeOrder.Length; i++)
            {
                EquipmentGrade grade = GradeOrder[i];
                if (GetRate(grade) <= 0f)
                {
                    continue;
                }

                if (GetGachaCandidates(grade).Count == 0)
                {
                    Debug.LogWarning($"[RuntimeEquipmentGachaService] Grade '{grade}' has a configured rate but no enabled equipment.", this);
                    return false;
                }
            }

            return runtimeData.GetGem() >= GetTotalCost(drawCount);
        }

        public bool TryDraw(int drawCount, out List<RuntimeEquipmentGachaResult> results)
        {
            results = new List<RuntimeEquipmentGachaResult>();

            if (!CanDraw(drawCount))
            {
                return false;
            }

            int totalCost = GetTotalCost(drawCount);
            if (!runtimeData.SpendGem(totalCost))
            {
                Debug.LogWarning("[RuntimeEquipmentGachaService] Not enough gems to draw.", this);
                return false;
            }

            for (int i = 0; i < drawCount; i++)
            {
                if (!TryDrawSingle(i, out RuntimeEquipmentGachaResult result))
                {
                    runtimeData.AddGem(totalCost);
                    RevertGrantedResults(results);
                    results.Clear();
                    Debug.LogWarning("[RuntimeEquipmentGachaService] Failed during multi-draw. Refunded draw cost and reverted granted copies.", this);
                    return false;
                }

                results.Add(result);
            }

            return true;
        }

        private bool TryDrawSingle(int drawIndex, out RuntimeEquipmentGachaResult result)
        {
            result = null;

            EquipmentGrade drawnGrade = RollGrade();
            List<EquipmentDefinition> candidates = GetGachaCandidates(drawnGrade);
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[RuntimeEquipmentGachaService] No gacha candidates found for grade '{drawnGrade}'.", this);
                return false;
            }

            EquipmentDefinition selectedDefinition = candidates[Random.Range(0, candidates.Count)];
            if (selectedDefinition == null || !equipmentState.TryAddOwnedEquipment(selectedDefinition.equipmentId))
            {
                Debug.LogWarning("[RuntimeEquipmentGachaService] Failed to grant drawn equipment.", this);
                return false;
            }

            result = new RuntimeEquipmentGachaResult
            {
                drawIndex = drawIndex,
                equipmentId = selectedDefinition.equipmentId,
                displayName = selectedDefinition.displayName,
                grade = selectedDefinition.grade,
                drawCost = settings.drawCost,
                currentLevel = equipmentState.GetOwnedLevel(selectedDefinition.equipmentId),
                currentOwnedCount = equipmentState.GetOwnedCount(selectedDefinition.equipmentId),
                definition = selectedDefinition
            };

            return true;
        }

        private EquipmentGrade RollGrade()
        {
            float roll = Random.Range(0f, settings.TotalRate);
            float cumulative = 0f;

            for (int i = 0; i < GradeOrder.Length; i++)
            {
                EquipmentGrade grade = GradeOrder[i];
                cumulative += GetRate(grade);

                if (roll <= cumulative)
                {
                    return grade;
                }
            }

            return EquipmentGrade.Legendary;
        }

        private List<EquipmentDefinition> GetGachaCandidates(EquipmentGrade grade)
        {
            List<EquipmentDefinition> candidates = new();
            EquipmentRuntimeDatabase database = equipmentState != null ? equipmentState.Database : null;
            if (database == null || !database.IsReady)
            {
                return candidates;
            }

            IReadOnlyList<EquipmentDefinition> definitions = database.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                EquipmentDefinition definition = definitions[i];
                if (definition == null || !definition.isGachaEnabled || definition.grade != grade)
                {
                    continue;
                }

                candidates.Add(definition);
            }

            return candidates;
        }

        private void RevertGrantedResults(IReadOnlyList<RuntimeEquipmentGachaResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                RuntimeEquipmentGachaResult grantedResult = results[i];
                if (grantedResult?.definition == null)
                {
                    continue;
                }

                equipmentState.TryConsumeOwnedEquipmentCopies(grantedResult.definition.equipmentId, 1);
            }
        }

        private float GetRate(EquipmentGrade grade)
        {
            if (settings == null)
            {
                return 0f;
            }

            return grade switch
            {
                EquipmentGrade.Common => settings.commonRate,
                EquipmentGrade.Magic => settings.magicRate,
                EquipmentGrade.Rare => settings.rareRate,
                EquipmentGrade.Epic => settings.epicRate,
                EquipmentGrade.Legendary => settings.legendaryRate,
                _ => 0f
            };
        }

        private void ResolveReferences()
        {
            GachaModuleRoot gachaModuleRoot = GetComponentInParent<GachaModuleRoot>();
            EquipmentModuleRoot equipmentModuleRoot = GetComponentInParent<EquipmentModuleRoot>();

            if (settings == null)
            {
                settings = gachaModuleRoot != null ? gachaModuleRoot.Settings : null;
            }

            if (equipmentState == null)
            {
                equipmentState = GetComponent<EquipmentRuntimeState>();
            }

            if (equipmentState == null)
            {
                equipmentState = GetComponentInParent<EquipmentRuntimeState>();
            }

            if (equipmentState == null && gachaModuleRoot != null)
            {
                equipmentState = gachaModuleRoot.EquipmentState;
            }

            if (equipmentState == null && equipmentModuleRoot != null)
            {
                equipmentState = equipmentModuleRoot.EquipmentState;
            }

            if (runtimeData == null)
            {
                runtimeData = gachaModuleRoot != null ? gachaModuleRoot.RuntimeData : null;
            }

            if (runtimeData == null && equipmentModuleRoot != null)
            {
                runtimeData = equipmentModuleRoot.RuntimeData;
            }

            if (runtimeData == null)
            {
                runtimeData = GetComponentInParent<RuntimeData>();
            }
        }
    }
}
