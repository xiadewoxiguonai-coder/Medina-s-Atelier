using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//using to save word list
[System.Serializable]
public class databaseword
{
    [SerializeField]
    public List<word> allWord;

    [SerializeField]
    public string saveID;

    [SerializeField]
    public string Listname;

    [SerializeField]
    public List<int> ImportantWord;

    [SerializeField]
    public string ChangeTime;

    [SerializeField] 
    private List<word> extraList;

    public List<int> allWordId;

    public databaseword()
    {
        allWord = new List<word>();
    }

    public void setId(string a)
    {
        saveID = a;
    }

    public string getID()
    {
        return saveID;
    }

    public bool checkHave(string a, string b)
    {
        for (int i = 0; i < allWord.Count; i++)
        {
            if (allWord[i].getEnglish().Equals(a) && allWord[i].getRune().Equals(b))
            {
                return true;
            }
        }
        return false;
    }


    public void addToList(word a)
    {
        allWord.Add(a);
    }

    public void removeword(int a)
    {
        allWord.RemoveAt(a);
    }


    public int getIndex(string e, string c)
    {
        for (int i = 0; i < allWord.Count; i++)
        {
            if (allWord[i].getRune().Equals(c) && allWord[i].getEnglish().Equals(e))
            {
                return i;
            }
        }

        return -1;
    }

    public word getword(int a)
    {
        return allWord[a];
    }
    public void setAllID()
    {
        for (int i = 0; i < allWord.Count; i++)
        {
            allWord[i].setID(i);
        }
    }

    public void setListID(string a)
    {
        saveID = a;
    }

    public void ToChangeTime()
    {
        ChangeTime = System.DateTime.Now.ToString();
    }

    public string getChangeTime()
    {
        return ChangeTime;
    }

    public List<word> GetList()
    {
        return allWord;
    }

    public void setFalseWords()
    {
        int id1;
        int id2;
        for (int i = 0; i < allWord.Count; i++)
        {
            id1 = allWord[i].getSid1();
            id2 = allWord[i].getSid2();
            CheckORadd(id1);
            CheckORadd(id2);

        }
    }
    public void CheckORadd(int ID)
    {
        
        
        

        if (allWordId.IndexOf(ID) ==-1)
        {
            
            allWordId.Add(ID);
        }
    }

    //check id and ensure all id add
    public void setAllWordId()
    {
        for (int i = 0; i < allWord.Count; i++)
        {
            allWordId.Add(allWord[i].getID());
        }
        for (int i = 0; i < extraList.Count; i++)
        {
            allWordId.Add(extraList[i].getID());
        }
        
    }
}