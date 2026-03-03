using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class BlockMoveArrow : MonoBehaviour
{

    private block useBlock;
    public float speed = 3f;
    public float time = 0f;
    public int position;
    public bool haveBreak;//is break
    public bool ismove;

    void Start()
    {
        haveBreak = false;
        transform.position = GameObject.Find("方块位置" + (position+1)).transform.position;
        transform.GetComponentInChildren<TextMeshPro>().text = useBlock.getShowString();
        
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        moveBlock();
        
        if (time >= 3.8f)
        {
            autoDistory();
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.tag.CompareTo("projectile") == 0)
        {
            //Debug.Log(2);
            distory();
            //other.gameObject.;
        }
    }

    public void setAll(block a)
    {
        useBlock = a;
        position = a.getPosition();
    }

    //
    void moveBlock()
    {
        if(ismove)
        {
            transform.Translate(Vector3.up * Time.deltaTime * speed);
        }
        
    }

    
    void distory()
    {
        if (!haveBreak)
        {
           
            GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().PlaySoundhit(useBlock.getHittingBool());
            GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().choiceOrFalse(useBlock.getThisIsNoWord(), useBlock.getHittingBool());
            GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().havehit++;
            GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().realTime = 3.8f;
            string useString = transform.name.Substring(0, transform.name.Length - 1);
            Destroy(GameObject.Find(useString + "1"), 0.01f);//delete after 0.01
            Destroy(GameObject.Find(useString + "2"), 0.01f);
            Destroy(GameObject.Find(useString + "3"), 0.01f);
            Destroy(GameObject.Find(useString + "4"), 0.01f);
            GameObject.Find(useString + "1").GetComponent<BlockMoveArrow>().haveBreak = true;
            GameObject.Find(useString + "2").GetComponent<BlockMoveArrow>().haveBreak = true;
            GameObject.Find(useString + "3").GetComponent<BlockMoveArrow>().haveBreak = true;
            GameObject.Find(useString + "4").GetComponent<BlockMoveArrow>().haveBreak = true;
        }
        //Destroy(GameObject.Find(transform.name));
    }

    //auto distroy
    void autoDistory()
    {
        if (!haveBreak)
        {
            GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().PlaySoundhit(false);
            GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().choiceOrFalse(useBlock.getThisIsNoWord(), false);
            GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().havehit++;
            GameObject.Find("/脚本加载").GetComponent<StartGameArrow>().realTime = 3.8f;
            String useString = transform.name.Substring(0, transform.name.Length - 1);

            Destroy(GameObject.Find(useString + "1"), 0.01f);
            Destroy(GameObject.Find(useString + "2"), 0.01f);
            Destroy(GameObject.Find(useString + "3"), 0.01f);
            Destroy(GameObject.Find(useString + "4"), 0.01f);
            GameObject.Find(useString + "1").GetComponent<BlockMoveArrow>().haveBreak = true;
            GameObject.Find(useString + "2").GetComponent<BlockMoveArrow>().haveBreak = true;
            GameObject.Find(useString + "3").GetComponent<BlockMoveArrow>().haveBreak = true;
            GameObject.Find(useString + "4").GetComponent<BlockMoveArrow>().haveBreak = true;
        }
        
    }

    
}
