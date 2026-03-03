using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class block
{
    private string showstring;          // Text displayed on the block (rune/phonetic/english)
    private bool hittingBool;           // Flag: Whether this block is the correct answer (true = correct)
    private float createTime;           // Spawn interval (time delay before block appears)
    private int position;               // Predefined spawn position index (0-9)
    private float extraSpeed;           // Additional speed applied when block is hit (animation)
    private float ridioByhIT;           // Rotation angle applied when block is hit (animation)
    private int thisIsNoWord;           // Index of the associated rune/word in the game list (for quick lookup)


    public block(string a, bool b, float d, int e, int f)
    {
        showstring = a;
        hittingBool = b;
        createTime = d;
        position = e;
        thisIsNoWord = f;
    }


    public string getShowString()
    {
        return showstring;
    }


    public bool getHittingBool()
    {
        return hittingBool;
    }


    public float getCreateTime()
    {
        return createTime;
    }


    public int getPosition()
    {
        return position;
    }


    public void setHitRidoAndSpeed(float hit, float a)
    {
        extraSpeed = hit;
        ridioByhIT = a;
    }

    public int getThisIsNoWord()
    {
        return thisIsNoWord;
    }
}