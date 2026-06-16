using UnityEngine;

public static class RuntimeServicesBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureServices()
    {
        if (Object.FindObjectOfType<GameInput>() != null)
        {
            return;
        }

        GameObject services = new GameObject("RuntimeServices");
        Object.DontDestroyOnLoad(services);
        services.AddComponent<GameInput>();
        services.AddComponent<SaveGameSystem>();
    }
}
