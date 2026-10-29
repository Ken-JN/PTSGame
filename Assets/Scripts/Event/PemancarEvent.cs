using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Lesson 2: Event (Emitter)
// Attached to an Empty GameObject. Event = safe delegate:
// other classes can subscribe (+=), but ONLY the emitter can Invoke.
public class PemancarEvent : MonoBehaviour
{
    public static event Action OnSpacePressed;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Emitter: Space pressed, sending event.");
            OnSpacePressed?.Invoke();
        }
    }
}
