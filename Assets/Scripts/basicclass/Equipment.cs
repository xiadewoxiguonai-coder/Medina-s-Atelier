using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Equipment : Item
{
    [SerializeField]
    private int durability;
    [SerializeField]
    private int maxDurability;
    [SerializeField]
    private int equipType;//
    [SerializeField]
    private int attackPower;
    [SerializeField]
    private int defensePower;
    [SerializeField]
    private int strength;
    [SerializeField]
    private int agility;
    [SerializeField]
    private int intelligence;
    [SerializeField]
    private int stamina;
    [SerializeField]
    private int faith;
    [SerializeField]
    private int sense;
    [SerializeField]
    private int luck;


    public Equipment(
        string id, int itemType, int atlierType, int[] size, int quality,
        int color, int colorquality, int[] effect, int[] eLock,
        int durability, int maxDurability, int equipType, int attackPower,
        int defensePower, int strength, int agility, int intelligence,
        int stamina, int faith, int sense, int luck
    ) : base(id, itemType, atlierType, size, quality, color, colorquality, effect, eLock)
    {
        this.durability = durability;
        this.maxDurability = maxDurability;
        this.equipType = equipType;
        this.attackPower = attackPower;
        this.defensePower = defensePower;
        this.strength = strength;
        this.agility = agility;
        this.intelligence = intelligence;
        this.stamina = stamina;
        this.faith = faith;
        this.sense = sense;
        this.luck = luck;
    }

    public int Durability
    {
        get { return durability; }
        set { durability = Mathf.Clamp(value, 0, maxDurability); }
    }

    public int MaxDurability
    {
        get { return maxDurability; }
        set
        {
            maxDurability = Mathf.Max(0, value);
            durability = Mathf.Clamp(durability, 0, maxDurability);
        }
    }

    public int EquipType
    {
        get { return equipType; }
        set { equipType = value; }
    }

    public int AttackPower
    {
        get { return attackPower; }
        set { attackPower = value; }
    }

    public int DefensePower
    {
        get { return defensePower; }
        set { defensePower = value; }
    }

    public int Strength
    {
        get { return strength; }
        set { strength = value; }
    }

    public int Agility
    {
        get { return agility; }
        set { agility = value; }
    }

    public int Intelligence
    {
        get { return intelligence; }
        set { intelligence = value; }
    }

    public int Stamina
    {
        get { return stamina; }
        set { stamina = value; }
    }

    public int Faith
    {
        get { return faith; }
        set { faith = value; }
    }

    public int Sense
    {
        get { return sense; }
        set { sense = value; }
    }

    public int Luck
    {
        get { return luck; }
        set { luck = value; }
    }
}