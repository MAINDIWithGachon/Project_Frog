using System;
using NewMinGyeom.Equipment;
using UnityEngine;

namespace NewMinGyeom.Backend
{
    public class EquipmentSaveDirtyTracker : MonoBehaviour
    {
        [SerializeField] private EquipmentRuntimeState equipmentState;
        [SerializeField] private bool dirty;

        public event Action DirtyChanged;

        public bool IsDirty => dirty;

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void MarkDirty()
        {
            if (dirty)
            {
                return;
            }

            dirty = true;
            DirtyChanged?.Invoke();
        }

        public void ClearDirty()
        {
            if (!dirty)
            {
                return;
            }

            dirty = false;
            DirtyChanged?.Invoke();
        }

        public bool ConsumeDirty()
        {
            bool wasDirty = dirty;
            ClearDirty();
            return wasDirty;
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

        private void Subscribe()
        {
            if (equipmentState != null)
            {
                equipmentState.StateChanged -= MarkDirty;
                equipmentState.StateChanged += MarkDirty;
            }
        }

        private void Unsubscribe()
        {
            if (equipmentState != null)
            {
                equipmentState.StateChanged -= MarkDirty;
            }
        }
    }
}
