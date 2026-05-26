using NewMinGyeom.Equipment;
using UnityEngine;

namespace NewMinGyeom.Backend
{
    public class BackendEquipmentUserDataLoader : MonoBehaviour
    {
        [SerializeField] private EquipmentRuntimeState equipmentState;
        [SerializeField] private bool autoLoadOnStart;

        [TextArea(8, 40)]
        [SerializeField] private string mockUserDataJson = @"{
  ""ownedEquipments"": [
    { ""equipmentId"": ""sword_001"", ""level"": 1, ""ownedCount"": 4 }
  ],
  ""equippedSlots"": [
    { ""slotType"": ""Weapon"", ""equippedItemId"": ""sword_001"" }
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
                LoadFromBackend();
            }
        }

        [ContextMenu("Load Equipment User Data From Backend")]
        public void LoadFromBackend()
        {
            LoadFromJson(mockUserDataJson);
        }

        public void LoadFromJson(string json)
        {
            ResolveReferences();
            if (equipmentState == null)
            {
                Debug.LogWarning("[BackendEquipmentUserDataLoader] EquipmentRuntimeState is missing.", this);
                return;
            }

            try
            {
                BackendEquipmentUserDataResponse response =
                    JsonUtility.FromJson<BackendEquipmentUserDataResponse>(json);

                equipmentState.SetOwnedEquipments(BackendEquipmentMapper.ToInstances(response?.ownedEquipments));
                ApplyEquippedSlots(response?.equippedSlots);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[BackendEquipmentUserDataLoader] Failed to parse equipment user data: {exception.Message}", this);
            }
        }

        private void ApplyEquippedSlots(BackendEquippedSlotDto[] equippedSlots)
        {
            if (equippedSlots == null)
            {
                return;
            }

            for (int i = 0; i < equippedSlots.Length; i++)
            {
                BackendEquippedSlotDto slot = equippedSlots[i];
                if (slot == null)
                {
                    continue;
                }

                EquipmentSlotType slotType = BackendEquipmentMapper.ToSlotType(slot.slotType);
                equipmentState.SetEquippedSlot(slotType, slot.equippedItemId);
            }
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
