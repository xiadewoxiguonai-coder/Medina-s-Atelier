using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Warehouse
{
    [SerializeField] private List<Item> items = new List<Item>();
    [SerializeField] private int maxCapacity;

    public Warehouse() { }

    public Warehouse(List<Item> items, int maxCapacity)
    {
        this.items = new List<Item>(items ?? new List<Item>());
        this.maxCapacity = maxCapacity;
    }

    public List<Item> Items { get => new List<Item>(items); set => items = new List<Item>(value ?? new List<Item>()); }
    public int MaxCapacity { get => maxCapacity; set => maxCapacity = value; }
}


[Serializable]
public class AlchemyRecipe
{
    [SerializeField] private int resultItemId;
    [SerializeField] private List<Item> requiredItems = new List<Item>();
    [SerializeField] private List<Item> optionalItems = new List<Item>();
    [SerializeField] private int needColor;

    [SerializeField] private string demand1;
    [SerializeField] private string demand2;
    [SerializeField] private string demand3;

    [SerializeField] private string effect1;
    [SerializeField] private string effect2;
    [SerializeField] private string effect3;

    public AlchemyRecipe() { }

    public AlchemyRecipe(int resultItemId, List<Item> requiredItems, List<Item> optionalItems, int needColor,
                        string demand1, string demand2, string demand3, string effect1, string effect2, string effect3)
    {
        this.resultItemId = resultItemId;
        this.requiredItems = new List<Item>(requiredItems ?? new List<Item>());
        this.optionalItems = new List<Item>(optionalItems ?? new List<Item>());
        this.needColor = needColor;
        this.demand1 = demand1;
        this.demand2 = demand2;
        this.demand3 = demand3;
        this.effect1 = effect1;
        this.effect2 = effect2;
        this.effect3 = effect3;
    }

    public int ResultItemId { get => resultItemId; set => resultItemId = value; }
    public List<Item> RequiredItems { get => new List<Item>(requiredItems); set => requiredItems = new List<Item>(value ?? new List<Item>()); }
    public List<Item> OptionalItems { get => new List<Item>(optionalItems); set => optionalItems = new List<Item>(value ?? new List<Item>()); }
    public int NeedColor { get => needColor; set => needColor = value; }
    public string Demand1 { get => demand1; set => demand1 = value; }
    public string Demand2 { get => demand2; set => demand2 = value; }
    public string Demand3 { get => demand3; set => demand3 = value; }
    public string Effect1 { get => effect1; set => effect1 = value; }
    public string Effect2 { get => effect2; set => effect2 = value; }
    public string Effect3 { get => effect3; set => effect3 = value; }
}


[Serializable]
public class AlchemyStove
{
    [SerializeField] private int size;
    [SerializeField] private string effect;
    [SerializeField] private Item rewardItem;

    public AlchemyStove() { }

    public AlchemyStove(int size, string effect, Item rewardItem)
    {
        this.size = size;
        this.effect = effect;
        this.rewardItem = rewardItem;
    }

    public int Size { get => size; set => size = value; }
    public string Effect { get => effect; set => effect = value; }
    public Item RewardItem { get => rewardItem; set => rewardItem = value; }
}


[Serializable]
public class QuickAlchemy
{
    [SerializeField] private string action;
    [SerializeField] private List<Item> costItems = new List<Item>();

    public QuickAlchemy() { }

    public QuickAlchemy(string action, List<Item> costItems)
    {
        this.action = action;
        this.costItems = new List<Item>(costItems ?? new List<Item>());
    }

    public string Action { get => action; set => action = value; }
    public List<Item> CostItems { get => new List<Item>(costItems); set => costItems = new List<Item>(value ?? new List<Item>()); }
}


[Serializable]
public class Buff
{
    [SerializeField] private int grade;
    [SerializeField] private string desc;
    [SerializeField] private string effect;
    [SerializeField] private float duration;

    public Buff() { }

    public Buff(int grade, string desc, string effect, float duration)
    {
        this.grade = grade;
        this.desc = desc;
        this.effect = effect;
        this.duration = duration;
    }

    public int Grade { get => grade; set => grade = value; }
    public string Desc { get => desc; set => desc = value; }
    public string Effect { get => effect; set => effect = value; }
    public float Duration { get => duration; set => duration = value; }
}


[Serializable]
public class PlayerAttr
{
    [SerializeField] private int hp;
    [SerializeField] private int attack;
    [SerializeField] private int def;
    [SerializeField] private int level;

    public PlayerAttr() { }

    public PlayerAttr(int hp, int attack, int def, int level)
    {
        this.hp = hp;
        this.attack = attack;
        this.def = def;
        this.level = level;
    }

    public int Hp { get => hp; set => hp = value; }
    public int Attack { get => attack; set => attack = value; }
    public int Def { get => def; set => def = value; }
    public int Level { get => level; set => level = value; }
}


[Serializable]
public class SaveData
{
    [SerializeField] private PlayerAttr playerAttr;
    [SerializeField] private Warehouse warehouse;
    [SerializeField] private bool isInTower;
    [SerializeField] private Tower tower;

    public SaveData() { }

    public SaveData(PlayerAttr playerAttr, Warehouse warehouse, bool isInTower, Tower tower)
    {
        this.playerAttr = playerAttr;
        this.warehouse = warehouse;
        this.isInTower = isInTower;
        this.tower = tower;
    }

    public PlayerAttr PlayerAttr { get => playerAttr; set => playerAttr = value; }
    public Warehouse Warehouse { get => warehouse; set => warehouse = value; }
    public bool IsInTower { get => isInTower; set => isInTower = value; }
    public Tower Tower { get => tower; set => tower = value; }
}


[Serializable]
public class Tower
{
    [SerializeField] private int level;
    [SerializeField] private List<Buff> buffs = new List<Buff>();
    [SerializeField] private int currentFloor;
    [SerializeField] private string position;
    [SerializeField] private string terrain;
    [SerializeField] private List<Monster> monsters = new List<Monster>();
    [SerializeField] private float time;
    [SerializeField] private int score;

    public Tower() { }

    public Tower(int level, List<Buff> buffs, int currentFloor, string position, string terrain, List<Monster> monsters, float time, int score)
    {
        this.level = level;
        this.buffs = new List<Buff>(buffs ?? new List<Buff>());
        this.currentFloor = currentFloor;
        this.position = position;
        this.terrain = terrain;
        this.monsters = new List<Monster>(monsters ?? new List<Monster>());
        this.time = time;
        this.score = score;
    }

    public int Level { get => level; set => level = value; }
    public List<Buff> Buffs { get => new List<Buff>(buffs); set => buffs = new List<Buff>(value ?? new List<Buff>()); }
    public int CurrentFloor { get => currentFloor; set => currentFloor = value; }
    public string Position { get => position; set => position = value; }
    public string Terrain { get => terrain; set => terrain = value; }
    public List<Monster> Monsters { get => new List<Monster>(monsters); set => monsters = new List<Monster>(value ?? new List<Monster>()); }
    public float Time { get => time; set => time = value; }
    public int Score { get => score; set => score = value; }
}


[Serializable]
public class Monster
{
    [SerializeField] public int MaxHp;
    [SerializeField] public int hp;
    [SerializeField] public int type;
    [SerializeField] public int race;
    [SerializeField] public int weakness;
    [SerializeField] public int toughness;
    [SerializeField] public List<Item> lootItems = new List<Item>();
    [SerializeField] public float lootRate;
    [SerializeField] public int level;
    [SerializeField] public List<Buff> buffs = new List<Buff>();

    public Monster() { }

    public Monster(int hp, int type, int race, int weakness, int toughness, List<Item> lootItems, float lootRate, int level, List<Buff> buffs)
    {
        this.hp = hp;
        this.MaxHp = hp;
        this.type = type;
        this.race = race;
        this.weakness = weakness;
        this.toughness = toughness;
        this.lootItems = new List<Item>(lootItems ?? new List<Item>());
        this.lootRate = lootRate;
        this.level = level;
        this.buffs = new List<Buff>(buffs ?? new List<Buff>());
    }

    public int Hp { get => hp; set => hp = value; }
    public int Type { get => type; set => type = value; }
    public int Race { get => race; set => race = value; }
    public int Weakness { get => weakness; set => weakness = value; }
    public int Toughness { get => toughness; set => toughness = value; }
    public List<Item> LootItems { get => new List<Item>(lootItems); set => lootItems = new List<Item>(value ?? new List<Item>()); }
    public float LootRate { get => lootRate; set => lootRate = value; }
    public int Level { get => level; set => level = value; }
    public List<Buff> Buffs { get => new List<Buff>(buffs); set => buffs = new List<Buff>(value ?? new List<Buff>()); }
}


[Serializable]
public class Floor
{
    [SerializeField] private int type;
    [SerializeField] private List<Room> rooms = new List<Room>();

    public Floor() { }

    public Floor(int type, List<Room> rooms)
    {
        this.type = type;
        this.rooms = new List<Room>(rooms ?? new List<Room>());
    }

    public int Type { get => type; set => type = value; }
    public List<Room> Rooms { get => new List<Room>(rooms); set => rooms = new List<Room>(value ?? new List<Room>()); }
}


[Serializable]
public class Room
{
    [SerializeField] private int type;
    [SerializeField] private List<Monster> creatures = new List<Monster>();
    [SerializeField] private Collectable collectable;

    public Room() { }

    public Room(int type, List<Monster> creatures, Collectable collectable)
    {
        this.type = type;
        this.creatures = new List<Monster>(creatures ?? new List<Monster>());
        this.collectable = collectable;
    }

    public int Type { get => type; set => type = value; }
    public List<Monster> Creatures { get => new List<Monster>(creatures); set => creatures = new List<Monster>(value ?? new List<Monster>()); }
    public Collectable Collectable { get => collectable; set => collectable = value; }
}


[Serializable]
public class TreasureBox
{
    [SerializeField] private Item item;
    [SerializeField] private int lockType;

    public TreasureBox() { }

    public TreasureBox(Item item, int lockType)
    {
        this.item = item;
        this.lockType = lockType;
    }

    public Item Item { get => item; set => item = value; }
    public int LockType { get => lockType; set => lockType = value; }
}


[Serializable]
public class Collectable
{
    [SerializeField] private int type;
    [SerializeField] private Item item;
    [SerializeField] private int quality;

    public Collectable() { }

    public Collectable(int type, Item item, int quality)
    {
        this.type = type;
        this.item = item;
        this.quality = quality;
    }

    public int Type { get => type; set => type = value; }
    public Item Item { get => item; set => item = value; }
    public int Quality { get => quality; set => quality = value; }
}


[Serializable]
public class Rune
{
    [SerializeField] private int runeType;
    [SerializeField] private string desc;

    public Rune() { }

    public Rune(int runeType, string desc)
    {
        this.runeType = runeType;
        this.desc = desc;
    }

    public int RuneType { get => runeType; set => runeType = value; }
    public string Desc { get => desc; set => desc = value; }
}


[Serializable]
public class Magic
{
    [SerializeField] private List<Rune> runeCombo = new List<Rune>();
    [SerializeField] private string desc;
    [SerializeField] private int type;

    public Magic() { }

    public Magic(List<Rune> runeCombo, string desc, int type)
    {
        this.runeCombo = new List<Rune>(runeCombo ?? new List<Rune>());
        this.desc = desc;
        this.type = type;
    }

    public List<Rune> RuneCombo { get => new List<Rune>(runeCombo); set => runeCombo = new List<Rune>(value ?? new List<Rune>()); }
    public string Desc { get => desc; set => desc = value; }
    public int Type { get => type; set => type = value; }
}