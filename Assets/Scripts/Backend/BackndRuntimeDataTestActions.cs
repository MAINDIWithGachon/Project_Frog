using System;
using BackEnd;
using UnityEngine;

/// <summary>
/// RuntimeData backend save/load test actions.
///
/// This class has no UI code. Connect these actions from an existing scene
/// button through TestManager, so the current hamburger menu UI stays intact.
/// </summary>
public static class BackndRuntimeDataTestActions
{
    private const int TestGoldAmount = 10000;

    public static bool SaveCurrentRuntimeDataJson(out string message)
    {
        message = "";

        if (!EnsureReady(out message))
            return false;

        if (!TryGetCurrentRuntimeDataJson(out string runtimeDataJson))
        {
            message = "RuntimeData JSON not found.";
            return false;
        }

        BackndRuntimeDataRepository.RuntimeDataBackendResult result =
            BackndRuntimeDataRepository.Instance.SaveRuntimeDataJson(runtimeDataJson);

        Debug.Log("[RuntimeData Save Result] " + result.ToJson());
        Debug.Log("[RuntimeData Save JSON] " + runtimeDataJson);

        message = result.isSuccess ? "Save OK" : result.errorCode + " / " + result.message;
        return result.isSuccess;
    }

    public static bool LoadRuntimeDataJson(out string message)
    {
        message = "";

        if (!EnsureReady(out message))
            return false;

        // LOAD 버튼의 백엔드 책임은 여기까지입니다.
        // Repository가 뒤끝 테이블들을 읽고 RuntimeData.mockJson과 같은 단일 JSON을 만들어 반환합니다.
        BackndRuntimeDataRepository.RuntimeDataBackendResult result =
            BackndRuntimeDataRepository.Instance.LoadRuntimeDataJson();

        Debug.Log("[RuntimeData Load Result] " + result.ToJson());

        if (!string.IsNullOrEmpty(result.runtimeDataJson))
            Debug.Log("[RuntimeData Load JSON] " + result.runtimeDataJson);

        // 중요: 여기서 RuntimeData.root에 바로 적용하지 않습니다.
        // result.runtimeDataJson을 받은 뒤 파싱/캐시 반영은 클라이언트 RuntimeData 담당 코드에서 처리합니다.
        message = result.isSuccess ? "Load OK. RuntimeData JSON returned." : result.errorCode + " / " + result.message;
        return result.isSuccess;
    }

    public static bool LoadAndApplyRuntimeDataJson(out string message)
    {
        message = "";

        if (!EnsureReady(out message))
            return false;

        BackndRuntimeDataRepository.RuntimeDataBackendResult result =
            BackndRuntimeDataRepository.Instance.LoadRuntimeDataJson();

        Debug.Log("[RuntimeData Load Result] " + result.ToJson());

        if (!string.IsNullOrEmpty(result.runtimeDataJson))
            Debug.Log("[RuntimeData Load JSON] " + result.runtimeDataJson);

        if (!result.isSuccess)
        {
            message = result.errorCode + " / " + result.message;
            return false;
        }

        RuntimeData runtimeData = UnityEngine.Object.FindFirstObjectByType<RuntimeData>(FindObjectsInactive.Include);
        if (runtimeData == null)
        {
            message = "RuntimeData object not found in scene.";
            return false;
        }

        if (!runtimeData.ApplyRuntimeDataJson(result.runtimeDataJson, out string applyMessage))
        {
            message = applyMessage;
            return false;
        }

        message = "Load OK. " + applyMessage;
        return true;
    }

    public static bool AddTestGold(out string message)
    {
        message = "";

        RuntimeData runtimeData = UnityEngine.Object.FindFirstObjectByType<RuntimeData>(FindObjectsInactive.Include);
        if (runtimeData == null)
        {
            message = "RuntimeData object not found in scene.";
            return false;
        }

        // matest 검증 전용 함수입니다.
        // 업그레이드 비용이 부족해서 저장/로드 흐름을 확인할 수 없을 때만 골드를 지급합니다.
        // 실제 저장/로드 Repository는 여전히 인게임 데이터를 직접 수정하지 않습니다.
        runtimeData.AddGold(TestGoldAmount);
        runtimeData.NotifyDataChanged();

        message = $"Add test gold OK: +{TestGoldAmount}";
        Debug.Log("[RuntimeData Add Test Gold] " + message);
        return true;
    }


    private static bool EnsureReady(out string message)
    {
        EnsureRepositoryExists();

        if (!BackendManager.IsInitialized)
        {
            message = "Backend init needed.";
            return false;
        }

        if (string.IsNullOrEmpty(Backend.UserInDate))
        {
            message = "Guest Login first.";
            return false;
        }

        message = "";
        return true;
    }

    private static void EnsureRepositoryExists()
    {
        if (BackndRuntimeDataRepository.Instance != null)
            return;

        if (UnityEngine.Object.FindFirstObjectByType<BackndRuntimeDataRepository>(FindObjectsInactive.Include) != null)
            return;

        GameObject repository = new GameObject("BackndRuntimeDataRepository");
        repository.AddComponent<BackndRuntimeDataRepository>();
    }

    private static bool TryGetCurrentRuntimeDataJson(out string runtimeDataJson)
    {
        runtimeDataJson = "";

        RuntimeData runtimeData = UnityEngine.Object.FindFirstObjectByType<RuntimeData>(FindObjectsInactive.Include);
        if (runtimeData == null)
            return false;

        Type runtimeDataType = runtimeData.GetType();

        System.Reflection.MethodInfo getRuntimeDataJson = runtimeDataType.GetMethod("GetRuntimeDataJson");
        if (getRuntimeDataJson != null)
        {
            runtimeDataJson = getRuntimeDataJson.Invoke(runtimeData, null) as string;
            return !string.IsNullOrEmpty(runtimeDataJson);
        }

        System.Reflection.MethodInfo exportJson = runtimeDataType.GetMethod("ExportJson", new[] { typeof(bool) });
        if (exportJson != null)
        {
            runtimeDataJson = exportJson.Invoke(runtimeData, new object[] { false }) as string;
            return !string.IsNullOrEmpty(runtimeDataJson);
        }

        return false;
    }

}
