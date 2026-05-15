using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class autoRemove : MonoBehaviour
{
    float time;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (time < 2 && !transform.GetComponent<TextMeshPro>().text.Equals(""))
        {
            time += Time.deltaTime;
        }
        if (time >= 1f && !transform.GetComponent<TextMeshPro>().text.Equals(""))
        {
            time = 0f;
            transform.GetComponent<TextMeshPro>().text = "";
        }
        if(transform.GetComponent<TextMeshPro>().text.Equals(""))
        {
            time = 0f;
        }
    }
}
