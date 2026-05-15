using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    [SerializeField] private int[] hp;
    [SerializeField] private int[] mp;
    [SerializeField] private int[] stamina;
    [SerializeField] public int attackPower;
    [SerializeField] private int defensePower;
    [SerializeField] private List<int> quickSkills;
    [SerializeField] private int strength;
    [SerializeField] private int agility;
    [SerializeField] private int intelligence;
    [SerializeField] private int staminaStat;
    [SerializeField] private int faith;
    [SerializeField] private int sense;
    [SerializeField] private int luck;
    [SerializeField] private float attackSpeed;
    [SerializeField] private List<Item> props;
    [SerializeField] private List<Item> equipments;
    [SerializeField] private List<Item> backpack;
    [SerializeField] private int skillPoints;
    [SerializeField] private List<int> skillTree;

    [SerializeField] private int level;
    [SerializeField] private int exp;
    [SerializeField] private int expToNextLevel;

    public CharacterStats() { }

    public CharacterStats(int[] hp, int[] mp, int[] stamina, int attackPower, int defensePower,
                          List<int> quickSkills, int strength, int agility, int intelligence,
                          int staminaStat, int faith, int sense, int luck, float attackSpeed,
                          List<Item> props, List<Item> equipments, List<Item> backpack,
                          int skillPoints, List<int> skillTree,
                          int level, int exp, int expToNextLevel)
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

        this.level = level;
        this.exp = exp;
        this.expToNextLevel = expToNextLevel;
    }

    
    public int[] Hp
    {
        get { return hp; }
        set { hp = value; }
    }

    public int[] Mp
    {
        get { return mp; }
        set { mp = value; }
    }

    public int[] Stamina
    {
        get { return stamina; }
        set { stamina = value; }
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

    public int Level
    {
        get => level;
        set => level = value;
    }
    public int Exp
    {
        get => exp;
        set => exp = value;
    }
    public int ExpToNextLevel
    {
        get => expToNextLevel;
        set => expToNextLevel = value;
    }

    
    public void ModifyCurrentHp(int value)
    {
        if (hp == null || hp.Length < 2) return;
        hp[1] = value; 
    }

    public void ModifyCurrentMp(int value)
    {
        if (mp == null || mp.Length < 2) return;
        mp[1] = value; 
    }

    public void AddExp(int addExp)
    {
        exp += addExp;

        while (exp >= expToNextLevel)
        {
            exp -= expToNextLevel;
            level++;

            skillPoints += 1;
            attackPower += 5;
            defensePower += 3;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.2f);

            hp[0] += 20;
            hp[1] = hp[0];

            mp[0] += 10;
            mp[1] = mp[0];

            
            if (LevelUpEffectManager.Instance != null)
            {
                GameObject rig = GameObject.Find("Camera");
                if (rig != null)
                {
                    LevelUpEffectManager.Instance.PlayLevelUpEffect(rig.transform.position);
                }
            }
        }
    }

    public void ResetLevelAndStats()
    {
        level = 1;
        exp = 0;
        expToNextLevel = 100;

        strength = 1;
        agility = 1;
        intelligence = 1;
        staminaStat = 1;
        faith = 1;
        sense = 1;
        luck = 1;

        attackPower = 40;
        defensePower = 5;
        attackSpeed = 2f;

        hp = new int[] { 100, 100 };
        mp = new int[] { 50, 50 };
        stamina = new int[] { 0, 0 };

        skillPoints = 0;
        skillTree?.Clear();
        quickSkills?.Clear();
    }
}