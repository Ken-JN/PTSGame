using TopDownCharacter2D.Health;
using UnityEngine;

namespace TopDownController2D.Scripts.TopDownCharacter2D.Animations
{
    public class SampleCharacterAnimation : TopDownAnimations
    {
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int IsHurt = Animator.StringToHash("IsHurt");
        
        [SerializeField] private bool createDustOnWalk = true;
        [SerializeField] private ParticleSystem dustParticleSystem;
        
        private HealthSystem _healthSystem;
        
        protected override void Awake()
        {
            base.Awake();
            _healthSystem = GetComponent<HealthSystem>();
        }

        protected void Start()
        {
            controller.OnAttackEvent.AddListener(_ => Attacking());
            controller.OnMoveEvent.AddListener(Move);

            if (_healthSystem != null)
            {
                _healthSystem.OnDamage.AddListener(Hurt);
                _healthSystem.OnInvincibilityEnd.AddListener(InvincibilityEnd);
            }
        }

      
        private void Move(Vector2 movementDirection)
        {
            animator.SetBool(IsWalking, movementDirection.magnitude > .5f);
        }

     
        private void Attacking()
        {
            animator.SetTrigger(Attack);
        }

      
        private void Hurt()
        {
            animator.SetBool(IsHurt, true);
        }

        public void InvincibilityEnd()
        {
            animator.SetBool(IsHurt, false);
        }

        
        public void CreateDustParticles()
        {
            if (createDustOnWalk)
            {
                dustParticleSystem.Stop();
                dustParticleSystem.Play();
            }
        }
    }
}