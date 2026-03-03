using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockStartGame : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.tag.CompareTo("projectile") == 0)
        {
            if(GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().goend)
            {
                GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().restart();
            }
        }
    }
}
