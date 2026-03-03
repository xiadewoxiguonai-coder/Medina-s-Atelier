using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro; 

public class showErrorWord : MonoBehaviour
{
    public RawDatabaseword saveRawData;
    string show;
    // Start is called before the first frame update
    void Start()
    {
        load();
        createStringAndShow();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void load()
    {
        saveRawData = new RawDatabaseword();
        if (File.Exists(Application.streamingAssetsPath + "/RawWord" + ".json"))
        {

            string json = File.ReadAllText(Application.streamingAssetsPath + "/RawWord" + ".json");
            if (!json.Equals(""))
            {
                saveRawData = JsonUtility.FromJson<RawDatabaseword>(json);

                Debug.Log("∂¡»°" + json);
            }

        }
    }

    public void createStringAndShow()
    {
        show = "";
        for(int i=0;i< saveRawData.allWord.Count;i++)
        {
            show = show + saveRawData.allWord[i].getRune() + "    " + saveRawData.allWord[i].getEnglish() + "    " + saveRawData.errorNumbers[i] + "    " + saveRawData.AllErrorTimes[i] + "     ";
            if((i+1)%4==0)
            {
                show = show + "\n";
            }
        }
        transform.GetComponent<TextMeshPro>().text = show;
    }
}
