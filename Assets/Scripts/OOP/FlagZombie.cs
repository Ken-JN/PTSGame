using UnityEngine;

public class FlagZombie : Enemy
{
    public bool flag = true;

    public override void Attack()
    {
        Debug.Log("FlagZombie Bites");
    }
}
