using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIsystem : MonoBehaviour
{
    // Start is called before the first frame update
    public int setNowOption = -1;
    public bool haveSection = false;
    public AudioClip useSound;
    
    void Start()
    {
        
    }


    // Update is called once per frame
    void Update()
    {
        if(!haveSection)
        {
            setNowOption = -1;
        }
    }

    private void LateUpdate()
    {
        
    }

    public void setNowOptions(int a)
    {
        setNowOption = a;
    }

    public void runFunction()
    {
        if(setNowOption==1)//game choice
        {
            SceneManager.LoadScene("GameChoice");
        }
        if(setNowOption==2)//back to menu
        {
            SceneManager.LoadScene("StartMenu");
        }
        if (setNowOption == 3)//see the record
        {
            SceneManager.LoadScene("showRawRecord");
        }
    }

    public void playSound()
    {
        transform.GetComponent<AudioSource>().PlayOneShot(useSound);
    }

    
}
