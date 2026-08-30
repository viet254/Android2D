using UnityEngine;

[System.Serializable]
public struct DamageInfo
{
    public int Amount;
    public DamageType DamageType;
    public GameObject Source;

    public DamageInfo(int amount, DamageType damageType, GameObject source)
    {
        Amount = amount;
        DamageType = damageType;
        Source = source;
    }
}
