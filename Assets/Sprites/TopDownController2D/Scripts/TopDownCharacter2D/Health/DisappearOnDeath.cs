using TopDownCharacter2D.FX;
using UnityEngine;

namespace TopDownCharacter2D.Health
{
    /// <summary>
    ///     Handles the removal of an entity when it dies
    /// </summary>
    public class DisappearOnDeath : MonoBehaviour
    {
        [Tooltip("Delay in seconds before the GameObject is destroyed on death")]
        [SerializeField] private float destroyDelay = 1.5f;

        public void OnDeath()
        {
            // Safety: If the Main Camera is attached to this entity, detach it so view isn't lost
            if (Camera.main != null && Camera.main.transform.IsChildOf(transform))
            {
                Camera.main.transform.SetParent(null);
            }

            // Stop physics
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }

            // Disable all colliders
            foreach (Collider2D col in transform.GetComponentsInChildren<Collider2D>())
            {
                col.enabled = false;
            }

            // Disable behaviours except audio/FX
            foreach (Behaviour component in transform.GetComponentsInChildren<Behaviour>())
            {
                if (!(component is AudioSource) && !(component is TopDownFx))
                {
                    component.enabled = false;
                }
            }

            // Disable renderers except particle systems
            foreach (Renderer component in transform.GetComponentsInChildren<Renderer>())
            {
                if (!(component is ParticleSystemRenderer))
                {
                    component.enabled = false;
                }
            }
            
            // Destroy the object after short delay to allow effects to finish
            Destroy(gameObject, destroyDelay);
        }
    }
}
