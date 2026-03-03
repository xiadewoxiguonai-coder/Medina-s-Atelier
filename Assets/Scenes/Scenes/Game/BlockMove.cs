using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class BlockMove : MonoBehaviour
{
    private block useBlock;               // Data container for current block
    public float speed = 3f;              // Vertical movement speed
    public float time = 0f;               // Lifetime timer (triggers auto-destroy)
    public int position;                  // Spawn position index (0-9)
    public bool haveBreak = false;        // Flag: Block destroyed/hit
    public bool ismove = true;            // Flag: Enable/disable movement (default: enabled)

    void Start()
    {
        haveBreak = false;

        // Set initial position (fallback to calculated position if spawn point not found)
        GameObject posObj = GameObject.Find("方块位置" + (position + 1));
        if (posObj != null)
        {
            transform.position = posObj.transform.position;
        }
        else
        {
            transform.position = new Vector3(position * 2f, 0f, 0f);
        }

        // Set display text (rune/phonetic/english)
        TextMeshPro tmp = transform.GetComponentInChildren<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = useBlock.getShowString();
        }
    }

    void Update()
    {
        if (haveBreak) return; // Exit if block is already destroyed

        time += Time.deltaTime;
        moveBlock();

        // Auto-destroy after 2.4 seconds (timeout)
        if (time >= 2.4f)
        {
            autoDistory();
        }
    }

    // Trigger on collision with lightsaber
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("lightSaber") && !haveBreak)
        {
            distory();
        }
    }

    // Initialize block with data from GameCreator
    public void setAll(block a)
    {
        useBlock = a;
        position = a.getPosition();
    }

    // Vertical movement logic
    void moveBlock()
    {
        if (ismove)
        {
            transform.Translate(Vector3.up * Time.deltaTime * speed);
        }
    }

    // Destroy block on lightsaber hit (process game logic)
    void distory()
    {
        if (!haveBreak && useBlock != null)
        {
            try
            {
                haveBreak = true;

                // Get game manager reference and process hit logic
                StartGame gameManager = GameObject.Find("/脚本加载")?.GetComponent<StartGame>();
                if (gameManager != null)
                {
                    gameManager.PlaySoundhit(useBlock.getHittingBool());
                    gameManager.choiceOrFalse(useBlock.getThisIsNoWord(), useBlock.getHittingBool());
                    gameManager.havehit++; // Increment hit counter
                }

                // Extract block group name (remove last character from game object name)
                string useString = transform.name.Substring(0, transform.name.Length - 1);

                // Destroy all blocks in the same group
                DestroyBlock(useString + "1");
                DestroyBlock(useString + "2");
                DestroyBlock(useString + "3");
                DestroyBlock(useString + "4");
            }
            catch (Exception e)
            {
                Debug.LogError("Hit destruction error: " + e.Message);
            }
        }
    }

    // Auto-destroy block after timeout (mark as incorrect)
    void autoDistory()
    {
        if (!haveBreak && useBlock != null)
        {
            try
            {
                haveBreak = true;

                // Get game manager reference and process timeout logic
                StartGame gameManager = GameObject.Find("/脚本加载")?.GetComponent<StartGame>();
                if (gameManager != null)
                {
                    gameManager.PlaySoundhit(false); // Play incorrect sound
                    gameManager.choiceOrFalse(useBlock.getThisIsNoWord(), false);
                    gameManager.havehit++; // Increment hit counter (timeout counts as attempt)
                }

                // Extract block group name
                string useString = transform.name.Substring(0, transform.name.Length - 1);

                // Destroy all blocks in the same group
                DestroyBlock(useString + "1");
                DestroyBlock(useString + "2");
                DestroyBlock(useString + "3");
                DestroyBlock(useString + "4");
            }
            catch (Exception e)
            {
                Debug.LogError("Timeout destruction error: " + e.Message);
            }
        }
    }

    // Helper: Destroy target block with null safety
    private void DestroyBlock(string blockName)
    {
        GameObject blockObj = GameObject.Find(blockName);
        if (blockObj != null)
        {
            BlockMove bm = blockObj.GetComponent<BlockMove>();
            if (bm != null)
            {
                bm.haveBreak = true; // Mark as destroyed to prevent duplicate processing
            }
            Destroy(blockObj, 0.01f); // Destroy after 0.01s delay (prevents frame issues)
        }
    }
}