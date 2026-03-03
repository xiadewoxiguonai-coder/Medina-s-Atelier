using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class word
{
    [SerializeField]
    int id;
    [SerializeField]
    string English; 
    [SerializeField]
    string Rune; 
    [SerializeField]
    string tone;

    [SerializeField]
    private int[] SimularIdArr = new int[2];

    public word(string a, string b, string c)
    {
        id = -1;
        English = a;
        Rune = b; 
        tone = c;
        SimularIdArr = new int[2];
    }

    public word(int id1, string a, string b, string c, int sid1, int sid2)
    {
        id = id1;
        English = a; 
        Rune = b; 
        tone = c;
        SimularIdArr = new int[2];
        SimularIdArr[0] = sid1;
        SimularIdArr[1] = sid2;
    }

    public int getID()
    {
        return id;
    }

    public string getEnglish()
    {
        return English;
    }

    public string getRune()
    {
        return Rune; 
    }

    public string gettone()
    {
        return tone;
    }

    public void setID(int a)
    {
        id = a;
    }

    public void setSimularID(int position, int value)
    {
        SimularIdArr[position] = value;
    }

    public int getSid1()
    {
        return SimularIdArr[0];
    }

    public int getSid2()
    {
        return SimularIdArr[1];
    }
}