using System.Collections;
using BackEnd;
using UnityEngine;
using BackendSdk = BackEnd.Backend;

namespace NewMinGyeom.Backend
{
    public class BackendGuestLoginBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool autoLoginOnStart = true;
        [SerializeField] private float waitForBackendInitializeSeconds = 10f;
        [SerializeField] private bool loadEquipmentDatabaseAfterLogin = true;
        [SerializeField] private bool loadUpgradeRulesAfterLogin = true;
        [SerializeField] private BackendEquipmentDatabaseLoader equipmentDatabaseLoader;
        [SerializeField] private BackendEquipmentUpgradeRuleLoader upgradeRuleLoader;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (autoLoginOnStart)
            {
                StartCoroutine(LoginWhenReady());
            }
        }

        [ContextMenu("Guest Login And Load Backend Charts")]
        public void GuestLoginAndLoad()
        {
            StartCoroutine(LoginWhenReady());
        }

        private IEnumerator LoginWhenReady()
        {
            ResolveReferences();

            float deadline = Time.realtimeSinceStartup + Mathf.Max(0f, waitForBackendInitializeSeconds);
            while (!BackendSdk.IsInitialized && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (!BackendSdk.IsInitialized)
            {
                Debug.LogWarning("[BackendGuestLoginBootstrapper] Backend SDK is not initialized.", this);
                yield break;
            }

            if (!BackendSdk.IsLogin)
            {
                BackendReturnObject response = BackendSdk.BMember.GuestLogin();
                if (!response.IsSuccess())
                {
                    Debug.LogError($"[BackendGuestLoginBootstrapper] Guest login failed: {response}", this);
                    yield break;
                }

                Debug.Log($"[BackendGuestLoginBootstrapper] Guest login succeeded: {response}", this);
            }
            else
            {
                Debug.Log("[BackendGuestLoginBootstrapper] Backend login is already completed.", this);
            }

            Debug.Log("[BackendGuestLoginBootstrapper] Backend login completed.", this);

            if (loadEquipmentDatabaseAfterLogin)
            {
                if (equipmentDatabaseLoader != null)
                {
                    equipmentDatabaseLoader.LoadFromBackend();
                }
                else
                {
                    Debug.LogWarning("[BackendGuestLoginBootstrapper] Equipment database loader is missing.", this);
                }
            }

            if (loadUpgradeRulesAfterLogin)
            {
                if (upgradeRuleLoader != null)
                {
                    upgradeRuleLoader.LoadFromBackend();
                }
                else
                {
                    Debug.LogWarning("[BackendGuestLoginBootstrapper] Upgrade rule loader is missing.", this);
                }
            }
        }

        private void ResolveReferences()
        {
            if (equipmentDatabaseLoader == null)
            {
                equipmentDatabaseLoader = GetComponent<BackendEquipmentDatabaseLoader>();
            }

            if (equipmentDatabaseLoader == null)
            {
                equipmentDatabaseLoader = GetComponentInChildren<BackendEquipmentDatabaseLoader>(true);
            }

            if (equipmentDatabaseLoader == null)
            {
                equipmentDatabaseLoader = GetComponentInParent<BackendEquipmentDatabaseLoader>();
            }

            if (upgradeRuleLoader == null)
            {
                upgradeRuleLoader = GetComponent<BackendEquipmentUpgradeRuleLoader>();
            }

            if (upgradeRuleLoader == null)
            {
                upgradeRuleLoader = GetComponentInChildren<BackendEquipmentUpgradeRuleLoader>(true);
            }

            if (upgradeRuleLoader == null)
            {
                upgradeRuleLoader = GetComponentInParent<BackendEquipmentUpgradeRuleLoader>();
            }
        }
    }
}
