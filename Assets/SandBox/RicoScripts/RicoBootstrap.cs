using UnityEngine;
using UnityEngine.SceneManagement;

// RICO BOOTSTRAP
// Adds my scripts to the game automatically when I press Play.
// I don't attach this to anything, and I don't have to click Add Component.
// It keeps the owner's scene untouched, so syncing my fork stays easy.
//
// New script for the Player? Add a line: AddIfMissing<ScriptName>(player);
public static class RicoBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Attach();
        Attach();
    }

    private static void Attach()
    {
        // My scripts that go on the Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            AddIfMissing<DebugOverlay>(player);
            AddIfMissing<FallSafety>(player);
        }

        // My bag + phone screens (replaces the old text HUD)
        if (Object.FindFirstObjectByType<RicoHUD>() == null)
            new GameObject("RicoHUD (auto)").AddComponent<RicoHUD>();
    }

    private static void AddIfMissing<T>(GameObject target) where T : Component
    {
        if (target.GetComponent<T>() == null)
            target.AddComponent<T>();
    }
}