using UnityEngine;

public class MonsterCountManager : MonoBehaviour
{
    [Header("")]
    public int totalMonsterCount;

    [Header("telepoter door")]
    public GameObject targetActiveObj;

    private int currentCount;

    void Start()
    {
        
        currentCount = totalMonsterCount;

        
        if (targetActiveObj != null)
            targetActiveObj.SetActive(false);
    }

    
    public void MonsterDie()
    {
        if (currentCount <= 0) return;

        currentCount--;
        Debug.Log("：" + currentCount);

        
        if (currentCount <= 0)
        {
            AllMonsterClear();
        }
    }

    void AllMonsterClear()
    {
        Debug.Log("");
        if (targetActiveObj != null)
            targetActiveObj.SetActive(true);
    }

    
    
}