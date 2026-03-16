using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    [SerializeField]
    private int[] hp;//£¨max,now£©
    [SerializeField]
    private int[] mp;
    [SerializeField]
    private int[] stamina;
    [SerializeField]
    private int attackPower;
    [SerializeField]
    private int defensePower;
    [SerializeField]
    private List<int> quickSkills;
    [SerializeField]
    private int strength;
    [SerializeField]
    private int agility;
    [SerializeField]
    private int intelligence;
    [SerializeField]
    private int staminaStat;
    [SerializeField]
    private int faith;
    [SerializeField]
    private int sense;
    [SerializeField]
    private int luck;
    [SerializeField]
    private float attackSpeed;
    [SerializeField]
    private List<Item> props;
    [SerializeField]
    private List<Item> equipments;
    [SerializeField]
    private List<Item> backpack;
    [SerializeField]
    private int skillPoints;
    [SerializeField]
    private List<int> skillTree;

    public CharacterStats()
    {
    }

    public CharacterStats(int[] hp, int[] mp, int[] stamina, int attackPower, int defensePower,
                          List<int> quickSkills, int strength, int agility, int intelligence,
                          int staminaStat, int faith, int sense, int luck, float attackSpeed,
                          List<Item> props, List<Item> equipments, List<Item> backpack,
                          int skillPoints, List<int> skillTree)
    {
        this.hp = hp?.Clone() as int[];
        this.mp = mp?.Clone() as int[];
        this.stamina = stamina?.Clone() as int[];
        this.attackPower = attackPower;
        this.defensePower = defensePower;
        this.quickSkills = new List<int>(quickSkills ?? new List<int>());
        this.strength = strength;
        this.agility = agility;
        this.intelligence = intelligence;
        this.staminaStat = staminaStat;
        this.faith = faith;
        this.sense = sense;
        this.luck = luck;
        this.attackSpeed = attackSpeed;
        this.props = new List<Item>(props ?? new List<Item>());
        this.equipments = new List<Item>(equipments ?? new List<Item>());
        this.backpack = new List<Item>(backpack ?? new List<Item>());
        this.skillPoints = skillPoints;
        this.skillTree = new List<int>(skillTree ?? new List<int>());
    }

    public int[] Hp
    {
        get { return hp?.Clone() as int[]; }
        set { hp = value?.Clone() as int[]; }
    }

    public int[] Mp
    {
        get { return mp?.Clone() as int[]; }
        set { mp = value?.Clone() as int[]; }
    }

    public int[] Stamina
    {
        get { return stamina?.Clone() as int[]; }
        set { stamina = value?.Clone() as int[]; }
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

    public List<int> QuickSkills
    {
        get { return new List<int>(quickSkills); }
        set { quickSkills = value != null ? new List<int>(value) : new List<int>(); }
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

    public int StaminaStat
    {
        get { return staminaStat; }
        set { staminaStat = value; }
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

    public float AttackSpeed
    {
        get { return attackSpeed; }
        set { attackSpeed = value; }
    }

    public List<Item> Props
    {
        get { return new List<Item>(props); }
        set { props = value != null ? new List<Item>(value) : new List<Item>(); }
    }

    public List<Item> Equipments
    {
        get { return new List<Item>(equipments); }
        set { equipments = value != null ? new List<Item>(value) : new List<Item>(); }
    }

    public List<Item> Backpack
    {
        get { return new List<Item>(backpack); }
        set { backpack = value != null ? new List<Item>(value) : new List<Item>(); }
    }

    public int SkillPoints
    {
        get { return skillPoints; }
        set { skillPoints = value; }
    }

    public List<int> SkillTree
    {
        get { return new List<int>(skillTree); }
        set { skillTree = value != null ? new List<int>(value) : new List<int>(); }
    }

    public void ModifyCurrentHp(int value)
    {
        if (hp == null) return;
        hp[0] = value;
    }

    public void ModifyCurrentMp(int value)
    {
        if (mp == null) return;
        mp[0] = value;
    }
}