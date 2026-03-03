using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class soundUIbutton : MonoBehaviour
{
    public int sectionNumber = 0; 

    private soundUIsystem uiSystem;
    private AudioSource audioSource;
    private TMP_Text textComponent;

    void Start()
    {
        
        uiSystem = GameObject.Find("/脚本加载").GetComponent<soundUIsystem>();
        audioSource = GetComponent<AudioSource>();
        Transform textChild = transform.Find("文本");
        if (textChild != null)
            textComponent = textChild.GetComponent<TMP_Text>();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("linechoice") && uiSystem != null)
        {
            uiSystem.haveSection = true;
            uiSystem.setNowOptions(sectionNumber);
            audioSource?.Play();
            uiSystem.playSound();
            if (textComponent != null)
                textComponent.color = Color.yellow;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("linechoice") && uiSystem != null)
        {
            uiSystem.haveSection = false;
            uiSystem.setNowOptions(-1);
            if (textComponent != null)
                textComponent.color = Color.white;
        }
    }
}
