using NewMinGyeom.Equipment;
using System;
using System.Collections;
using System.Collections.Generic;
using BackEnd;
using LitJson;
using UnityEngine;
using BackendSdk = BackEnd.Backend;

namespace NewMinGyeom.Backend
{
    public class BackendEquipmentUpgradeRuleLoader : MonoBehaviour
    {
        [SerializeField] private EquipmentRuntimeState equipmentState;
        [SerializeField] private bool autoLoadOnStart;
        [SerializeField] private string backendChartFileId;
        [SerializeField] private float waitForBackendReadySeconds = 10f;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (autoLoadOnStart)
            {
                StartCoroutine(LoadFromBackendWhenReady());
            }
        }

        [ContextMenu("Load Equipment Upgrade Rules From Backend")]
        public void LoadFromBackend()
        {
            TryLoadFromBackend();
        }

        public bool TryLoadFromBackend()
        {
            ResolveReferences();
            if (equipmentState == null)
            {
                Debug.LogWarning("[BackendEquipmentUpgradeRuleLoader] EquipmentRuntimeState is missing.", this);
                return false;
            }

            if (!BackendSdk.IsInitialized)
            {
                Debug.LogWarning("[BackendEquipmentUpgradeRuleLoader] Backend SDK is not initialized.", this);
                return false;
            }

            if (!BackendSdk.IsLogin)
            {
                Debug.LogWarning("[BackendEquipmentUpgradeRuleLoader] Backend login is not completed. Load upgrade rule chart after guest/custom login succeeds.", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(backendChartFileId))
            {
                Debug.LogWarning("[BackendEquipmentUpgradeRuleLoader] Backend chart file id is empty.", this);
                return false;
            }

            try
            {
                BackendReturnObject response = BackendSdk.Chart.GetChartContents(backendChartFileId);
                if (!response.IsSuccess())
                {
                    Debug.LogWarning($"[BackendEquipmentUpgradeRuleLoader] Failed to load upgrade rule chart from backend: {response}", this);
                    return false;
                }

                List<EquipmentUpgradeRule> rules = ToUpgradeRules(response.FlattenRows());
                equipmentState.SetUpgradeRules(rules);

                if (rules.Count > 0)
                {
                    Debug.Log($"[BackendEquipmentUpgradeRuleLoader] Loaded {rules.Count} upgrade rule rows from backend chart '{backendChartFileId}'.", this);
                }
                else
                {
                    Debug.LogWarning($"[BackendEquipmentUpgradeRuleLoader] Backend chart '{backendChartFileId}' returned no upgrade rule rows.", this);
                }

                return rules.Count > 0;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BackendEquipmentUpgradeRuleLoader] Failed to load upgrade rule chart from backend: {exception.Message}", this);
                return false;
            }
        }

        private IEnumerator LoadFromBackendWhenReady()
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0f, waitForBackendReadySeconds);
            while ((!BackendSdk.IsInitialized || !BackendSdk.IsLogin) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            LoadFromBackend();
        }

        private static List<EquipmentUpgradeRule> ToUpgradeRules(JsonData rows)
        {
            List<EquipmentUpgradeRule> rules = new();
            if (rows == null || !rows.IsArray)
            {
                return rules;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                JsonData row = rows[i];
                if (row == null || !row.IsObject)
                {
                    continue;
                }

                if (!Enum.TryParse(GetStringAny(row, "grade", "rarity"), true, out EquipmentGrade grade))
                {
                    continue;
                }

                rules.Add(new EquipmentUpgradeRule
                {
                    grade = grade,
                    currentLevel = GetInt(row, "currentLevel", 1),
                    requiredDuplicateCount = GetInt(row, "requiredDuplicateCount"),
                    requiredGold = GetInt(row, "requiredGold"),
                    requiredUpgradeStone = GetInt(row, "requiredUpgradeStone")
                });
            }

            return rules;
        }

        private static string GetStringAny(JsonData row, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                string value = GetString(row, keys[i]);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string GetString(JsonData row, string key, string fallback = "")
        {
            if (row == null || !row.IsObject || !row.ContainsKey(key))
            {
                return fallback;
            }

            return UnwrapBackendValue(row[key])?.ToString() ?? fallback;
        }

        private static int GetInt(JsonData row, string key, int fallback = 0)
        {
            string value = GetString(row, key);
            return int.TryParse(value, out int parsed) ? parsed : fallback;
        }

        private static JsonData UnwrapBackendValue(JsonData value)
        {
            if (value == null || !value.IsObject)
            {
                return value;
            }

            if (value.ContainsKey("S"))
            {
                return value["S"];
            }

            if (value.ContainsKey("N"))
            {
                return value["N"];
            }

            if (value.ContainsKey("BOOL"))
            {
                return value["BOOL"];
            }

            return value;
        }

        private void ResolveReferences()
        {
            if (equipmentState == null)
            {
                equipmentState = GetComponent<EquipmentRuntimeState>();
            }

            if (equipmentState == null)
            {
                equipmentState = GetComponentInParent<EquipmentRuntimeState>();
            }
        }
    }
}
