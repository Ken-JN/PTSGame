using UnityEngine;

// INHERITANCE + POLYMORPHISM
// BucketHeadZombie inherits from Enemy and overrides Attack().
public class BucketHeadZombie : Enemy
{
    public bool bucket = true;

    public override void Attack()
    {
        Debug.Log("Buckethead Bites");
    }
}
