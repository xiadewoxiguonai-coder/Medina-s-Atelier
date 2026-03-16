using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UsableItem : Item
{
    [SerializeField]
    private int usableType;
    [SerializeField]
    private int useCount;
    [SerializeField]
    private int useCost;
    [SerializeField]
    private int baseValue;
    [SerializeField]
    private int attribute;


    public UsableItem(
        string id, int itemType, int atlierType, int[] size, int quality,
        int color, int colorquality, int[] effect, int[] eLock,
        int usableType, int useCount, int useCost, int baseValue, int attribute
    ) : base(id, itemType, atlierType, size, quality, color, colorquality, effect, eLock)
    {
        this.usableType = usableType;
        this.useCount = useCount;
        this.useCost = useCost;
        this.baseValue = baseValue;
        this.attribute = attribute;
    }

    public int UsableType
    {
        get { return usableType; }
        set { usableType = value; }
    }

    public int UseCount
    {
        get { return useCount; }
        set { useCount = Mathf.Max(0, value); }
    }

    public int UseCost
    {
        get { return useCost; }
        set { useCost = Mathf.Max(0, value); }
    }

    public int BaseValue
    {
        get { return baseValue; }
        set { baseValue = value; }
    }

    public int Attribute
    {
        get { return attribute; }
        set { attribute = value; }
    }
}