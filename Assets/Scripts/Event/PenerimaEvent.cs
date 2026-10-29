using UnityEngine;

// Lesson 2: Event (Receiver / Subscriber)
// Attached to another GameObject. Receiver does not need to know emitter details.
public class PenerimaEvent : MonoBehaviour
{
    void OnEnable()
    {
        PemancarEvent.OnSpacePressed += OnEventTriggered;
    }

    void OnDisable()
    {
        PemancarEvent.OnSpacePressed -= OnEventTriggered;
    }

    void OnEventTriggered()
    {
        Debug.Log("Receiver: Heard space event!");
    }
}
