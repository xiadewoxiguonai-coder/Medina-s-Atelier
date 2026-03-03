using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class soundUIsystem : MonoBehaviour
{
    public int setNowOption = -1;
    public bool haveSection = false;
    public AudioClip useSound;
    public RuneVoiceGame_3Model usemodel;

    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {
        if (!haveSection)
        {
            setNowOption = -1;
        }
    }


    public void setNowOptions(int a)
    {
        setNowOption = a;
    }

    public void runFunction()
    {
        if (setNowOption == 1)//game start
        {
            usemodel.StartGame();
        }
        if (setNowOption == 2)//back to menu
        {
            SceneManager.LoadScene("StartMenu");
        }
        if (setNowOption >= 3)
        {
            int micIndex = setNowOption - 3;

            if (usemodel != null)
            {
                usemodel.OnMicDeviceSelected(micIndex);
            }
        }
    }

    public void playSound()
    {
        transform.GetComponent<AudioSource>().PlayOneShot(useSound);
    }
}
