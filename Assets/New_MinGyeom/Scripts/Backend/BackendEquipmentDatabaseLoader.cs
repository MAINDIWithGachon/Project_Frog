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
    public class BackendEquipmentDatabaseLoader : MonoBehaviour
    {
        [SerializeField] private EquipmentRuntimeState equipmentState;
        [SerializeField] private bool autoLoadOnStart;
        [SerializeField] private string backendChartFileId;
        [SerializeField] private float waitForBackendReadySeconds = 10f;
        [SerializeField] private bool fallbackToMockJsonOnBackendFailure;

        [TextArea(8, 40)]
        [SerializeField] private string mockResponseJson = @"{
  ""equipments"": [
    {
      ""equipmentId"": ""sword_001"",
      ""displayName"": ""Training Sword"",
      ""slotType"": ""Weapon"",
      ""grade"": ""Common"",
      ""iconKey"": ""sword_001"",
      ""description"": ""Loaded from backend-shaped JSON."",
      ""maxLevel"": 10,
      ""attack"": 12,
      ""hp"": 0,
      ""hpRegen"": 0,
      ""critChance"": 0,
      ""critDamage"": 0,
      ""isGachaEnabled"": true
    }
  ]
}";

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

        [ContextMenu("Load Equipment Database From Backend")]
        public void LoadFromBackend()
        {
            TryLoadFromBackend();
        }

        public bool TryLoadFromBackend()
        {
            ResolveReferences();
            if (equipmentState == null)
            {
                Debug.LogWarning("[BackendEquipmentDatabaseLoader] EquipmentRuntimeState is missing.", this);
                return false;
            }

            if (!BackendSdk.IsInitialized)
            {
                Debug.LogWarning("[BackendEquipmentDatabaseLoader] Backend SDK is not initialized.", this);
                return TryLoadFallback();
            }

            if (!BackendSdk.IsLogin)
            {
                Debug.LogWarning("[BackendEquipmentDatabaseLoader] Backend login is not completed. Load equipment chart after guest/custom login succeeds.", this);
                return TryLoadFallback();
            }

            if (string.IsNullOrWhiteSpace(backendChartFileId))
            {
                Debug.LogWarning("[BackendEquipmentDatabaseLoader] Backend chart file id is empty.", this);
                return TryLoadFallback();
            }

            EquipmentRuntimeDatabase database = new();
            database.SetLoading();
            equipmentState.SetDatabase(database);

            try
            {
                BackendReturnObject response =
                    BackendSdk.Chart.GetChartContents(backendChartFileId);

                if (!response.IsSuccess())
                {
                    database.SetFailed();
                    equipmentState.SetDatabase(database);
                    Debug.LogWarning($"[BackendEquipmentDatabaseLoader] Failed to load equipment chart from backend: {response}", this);
                    return TryLoadFallback();
                }

                List<BackendEquipmentDto> equipments = ToEquipmentDtos(response.FlattenRows());
                database.SetDefinitions(BackendEquipmentMapper.ToDefinitions(equipments));
                equipmentState.SetDatabase(database);

                if (database.IsReady)
                {
                    Debug.Log($"[BackendEquipmentDatabaseLoader] Loaded {equipments.Count} equipment rows from backend chart '{backendChartFileId}'.", this);
                }
                else
                {
                    Debug.LogWarning($"[BackendEquipmentDatabaseLoader] Backend chart '{backendChartFileId}' returned no equipment rows.", this);
                }

                return database.IsReady;
            }
            catch (Exception exception)
            {
                database.SetFailed();
                equipmentState.SetDatabase(database);
                Debug.LogError($"[BackendEquipmentDatabaseLoader] Failed to load equipment chart from backend: {exception.Message}", this);
                return TryLoadFallback();
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

        public void LoadFromJson(string json)
        {
            TryLoadFromJson(json);
        }

        public bool TryLoadFromJson(string json)
        {
            ResolveReferences();
            if (equipmentState == null)
            {
                Debug.LogWarning("[BackendEquipmentDatabaseLoader] EquipmentRuntimeState is missing.", this);
                return false;
            }

            EquipmentRuntimeDatabase database = new();
            database.SetLoading();
            equipmentState.SetDatabase(database);

            try
            {
                BackendEquipmentDatabaseResponse response =
                    JsonUtility.FromJson<BackendEquipmentDatabaseResponse>(json);

                database.SetDefinitions(BackendEquipmentMapper.ToDefinitions(response?.equipments));
                equipmentState.SetDatabase(database);
                return database.IsReady;
            }
            catch (System.Exception exception)
            {
                database.SetFailed();
                equipmentState.SetDatabase(database);
                Debug.LogError($"[BackendEquipmentDatabaseLoader] Failed to parse equipment DB: {exception.Message}", this);
                return false;
            }
        }

        private bool TryLoadFallback()
        {
            return fallbackToMockJsonOnBackendFailure && TryLoadFromJson(mockResponseJson);
        }

        private static List<BackendEquipmentDto> ToEquipmentDtos(JsonData rows)
        {
            List<BackendEquipmentDto> equipments = new();
            if (rows == null || !rows.IsArray)
            {
                return equipments;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                JsonData row = rows[i];
                if (row == null || !row.IsObject)
                {
                    continue;
                }

                BackendEquipmentDto dto = new()
                {
                    equipmentId = GetString(row, nameof(BackendEquipmentDto.equipmentId)),
                    displayName = GetString(row, nameof(BackendEquipmentDto.displayName)),
                    slotType = GetStringAny(row, nameof(BackendEquipmentDto.slotType), "category"),
                    grade = GetStringAny(row, nameof(BackendEquipmentDto.grade), "rarity"),
                    iconKey = GetStringAny(
                        row,
                        nameof(BackendEquipmentDto.iconKey),
                        nameof(BackendEquipmentDto.equipmentId),
                        nameof(BackendEquipmentDto.iconAssetPath)),
                    description = GetString(row, nameof(BackendEquipmentDto.description)),
                    maxLevel = GetInt(row, nameof(BackendEquipmentDto.maxLevel), 1),
                    attack = GetInt(row, nameof(BackendEquipmentDto.attack)),
                    hp = GetInt(row, nameof(BackendEquipmentDto.hp)),
                    hpRegen = GetFloatAny(row, nameof(BackendEquipmentDto.hpRegen), "healPerSec"),
                    critChance = GetFloat(row, nameof(BackendEquipmentDto.critChance)),
                    critDamage = GetFloat(row, nameof(BackendEquipmentDto.critDamage)),
                    isGachaEnabled = GetBool(row, nameof(BackendEquipmentDto.isGachaEnabled), true)
                };

                if (!string.IsNullOrWhiteSpace(dto.equipmentId))
                {
                    equipments.Add(dto);
                }
            }

            return equipments;
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
            if (!TryGetValue(row, key, out JsonData value))
            {
                return fallback;
            }

            return UnwrapBackendValue(value)?.ToString() ?? fallback;
        }

        private static int GetInt(JsonData row, string key, int fallback = 0)
        {
            string value = GetString(row, key);
            return int.TryParse(value, out int parsed) ? parsed : fallback;
        }

        private static float GetFloatAny(JsonData row, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                string value = GetString(row, keys[i]);
                if (float.TryParse(value, out float parsed))
                {
                    return parsed;
                }
            }

            return 0f;
        }

        private static float GetFloat(JsonData row, string key, float fallback = 0f)
        {
            string value = GetString(row, key);
            return float.TryParse(value, out float parsed) ? parsed : fallback;
        }

        private static bool GetBool(JsonData row, string key, bool fallback = false)
        {
            string value = GetString(row, key);
            return bool.TryParse(value, out bool parsed) ? parsed : fallback;
        }

        private static bool TryGetValue(JsonData row, string key, out JsonData value)
        {
            value = null;
            if (row == null || !row.IsObject || !row.ContainsKey(key))
            {
                return false;
            }

            value = row[key];
            return true;
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
