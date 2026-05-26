using System;

namespace NewMinGyeom.Backend
{
    [Serializable]
    public class BackendEquipmentDatabaseResponse
    {
        public BackendEquipmentDto[] equipments = Array.Empty<BackendEquipmentDto>();
    }
}
