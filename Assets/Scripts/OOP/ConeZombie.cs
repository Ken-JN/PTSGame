using UnityEngine;

public class ConeZombie : Enemy
{
    public bool cone = true;

    public override void Attack()
    {
        Debug.Log("Cone Bites");
    }
}
