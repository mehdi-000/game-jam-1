using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Project uses Input System only (<c>activeInputHandler</c> = Input System Package).
/// Pointer/click routing for UI Toolkit + uGUI expects an <see cref="EventSystem"/> with
/// <see cref="InputSystemUIInputModule"/> (same setup as <c>Menu.unity</c>). Gameplay and
/// EndScreen scenes had none, which can make clicks unreliable after a scene load.
/// </summary>
public static class EnsureUiEventSystem
{
    public static void EnsureExists()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }
}
