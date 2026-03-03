using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class backgroundColorChange : MonoBehaviour
{
    // Start is called before the first frame update
    public Color useColor;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.GetComponent<Image>().color.a > 0)
        {
            transform.GetComponent<Image>().color = new Color(transform.GetComponent<Image>().color.r, transform.GetComponent<Image>().color.g, transform.GetComponent<Image>().color.b, transform.GetComponent<Image>().color.a - 0.05f);
        }
    }

    public void setRED()
    {
        transform.GetComponent<Image>().color = new Color(transform.GetComponent<Image>().color.r, transform.GetComponent<Image>().color.g, transform.GetComponent<Image>().color.b, 1);
    }
}
