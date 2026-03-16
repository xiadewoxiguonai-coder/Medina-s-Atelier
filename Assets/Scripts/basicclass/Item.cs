using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Item
{
    [SerializeField]
    string ID;
    [SerializeField]
    int itemType;
    [SerializeField]
    int atlierType;
    [SerializeField]
    int[] size;
    [SerializeField]
    int quality;
    [SerializeField]
    int color;// the color
    [SerializeField]
    int colorquality;// the color number, using to 
    [SerializeField]
    int[] effect;   //the effect 
    [SerializeField]
    int[] ELock;// effect id


    public Item(string id, int itemType, int atlierType, int[] size, int quality,
                int color, int colorquality, int[] effect, int[] eLock)
    {
        ID = id;
        this.itemType = itemType;
        this.atlierType = atlierType;
        this.size = size?.Clone() as int[];
        this.quality = quality;
        this.color = color;
        this.colorquality = colorquality;
        this.effect = effect?.Clone() as int[];

        if (eLock != null)
        {
            ELock = eLock?.Clone() as int[];
        }


    }

    public string ItemID
    {
        get { return ID; }
        set { ID = value; }
    }

    public int ItemType
    {
        get { return itemType; }
        set { itemType = value; }
    }

    public int AtlierType
    {
        get { return atlierType; }
        set { atlierType = value; }
    }

    public int[] Size
    {
        get
        {
            return size?.Clone() as int[];
        }
        set
        {
            size = value?.Clone() as int[];
        }
    }

    public int Quality
    {
        get { return quality; }
        set { quality = value; }
    }

    public int Color
    {
        get { return color; }
        set { color = value; }
    }

    public int ColorQuality
    {
        get { return colorquality; }
        set { colorquality = value; }
    }

    public int[] Effect
    {
        get { return effect?.Clone() as int[]; }
        set { effect = value?.Clone() as int[]; }
    }

    
    public int[] getELock()
    {
        int[] copy = ELock?.Clone() as int[];
        return copy;

    }

    public void setElock(int[] set)
    {
        if (set != null)
        {
            ELock = set?.Clone() as int[];
        }
    }

   
}
