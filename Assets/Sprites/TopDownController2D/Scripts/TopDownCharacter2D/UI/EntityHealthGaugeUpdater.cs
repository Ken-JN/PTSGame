using TopDownCharacter2D.Health;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TopDownCharacter2D.UI
{
    /// <summary>
    ///     Updates the health gauge of an entity
    /// </summary>
    public class EntityHealthGaugeUpdater : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private GameObject entityObject;

        [SerializeField] private UnityEvent onHealthUpdate;

        private HealthSystem _entityHealth;

        private void Awake()
        {
            if (entityObject == null)
            {
                entityObject = gameObject;
            }

            _entityHealth = entityObject.GetComponentInParent<HealthSystem>();

            if (healthSlider == null)
            {
                healthSlider = GetComponent<Slider>();
            }
        }

        private void Start()
        {
            if (_entityHealth != null)
            {
                _entityHealth.OnDamage.AddListener(UpdateHealth);
                _entityHealth.OnHeal.AddListener(UpdateHealth);
                UpdateHealth();
            }
        }

        /// <summary>
        ///     Updates the health bar slider's value
        /// </summary>
        public void UpdateHealth()
        {
            if (_entityHealth != null && healthSlider != null && _entityHealth.MaxHealth > 0)
            {
                healthSlider.value = _entityHealth.CurrentHealth / _entityHealth.MaxHealth;
                onHealthUpdate.Invoke();
            }
        }
    }
}
