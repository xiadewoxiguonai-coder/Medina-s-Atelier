using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTrigger : MonoBehaviour
{
    [Header("Target Scene Name")]
    public string targetSceneName;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Portal triggered! Object: " + other.name + ", Tag: " + other.tag);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered! Loading scene: " + targetSceneName);
            SceneManager.LoadScene(targetSceneName);
        }
    }
}