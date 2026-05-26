using System;

namespace NewMinGyeom.Backend
{
    [Serializable]
    public class BackendOwnedEquipmentDto
    {
        public string equipmentId;
        public int level = 1;
        public int ownedCount = 1;
    }

    [Serializable]
    public class BackendEquippedSlotDto
    {
        public string slotType;
        public string equippedItemId;
    }

    [Serializable]
    public class BackendEquipmentUserDataResponse
    {
        public BackendOwnedEquipmentDto[] ownedEquipments = Array.Empty<BackendOwnedEquipmentDto>();
        public BackendEquippedSlotDto[] equippedSlots = Array.Empty<BackendEquippedSlotDto>();
    }
}
