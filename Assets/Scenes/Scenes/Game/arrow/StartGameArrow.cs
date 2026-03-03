using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR.InteractionSystem;
using UnityEngine.SceneManagement;

public class StartGameArrow : MonoBehaviour
{
    private const int TOTAL_RUNE_COUNT = 24;

    private databaseword useAllWord;// Rune database
    public int gameModel; //1:Rune-English, 2:English-Rune, 3:See English → Type Rune, 4:Hear Rune → Hit Rune (Archery Mode)
    private List<block> allTrueWordUsingSHOW;// Correct rune blocks
    private List<block> falseBlock1;// Distractor block 1
    private List<block> falseBlock2;// Distractor block 2
    private List<block> falseBlock3;// Distractor block 3
    public string[] allToneSingle;
    public int nowListNumbr = 0;// Current rune index
    private float paruseTime = 3f;// Default pause time (3s)
    public float nextCreateTime = 0f;
    public float realTime = 0f;
    public GameObject Newblock;// Block prefab
    public GameObject toraw;// Block parent container
    public List<int> allErrorShow;// Incorrect rune indices
    public int createNumber = 0;// Block creation counter
    public List<word> ralUseWord;// 24 rune data list
    public int linkNumber = 0;// Combo count
    public bool goend = false;// Game end flag
    public AudioClip[] allsoundTone;// Rune pronunciation audio clips
    public float finalEndTime;// Game timer (60s total)
    public AudioClip[] truesound;// Correct/Incorrect feedback sounds
    public int havehit = 0;// Total hit blocks
    public bool isFirst = true;

    private string[] runeSymbols = {
        "ᚠ", "ᚢ", "ᚦ", "ᚨ", "ᚱ", "ᚲ", "ᚷ", "ᚹ",
        "ᚺ", "ᚾ", "ᛁ", "ᛃ", "ᛇ", "ᛈ", "ᛉ", "ᛋ",
        "ᛏ", "ᛒ", "ᛖ", "ᛗ", "ᛚ", "ᛜ", "ᛞ", "ᛟ"
    };

    // Initialization
    void Start()
    {
        goend = true;
        firstPlay();
    }

    // Frame Update
    void Update()
    {
        
        setTrueSHow();

        // Toggle start button visibility
        if (!goend)
        {
            GameObject.Find("/开始游戏方块").GetComponent<MeshRenderer>().enabled = false;
            GameObject.Find("/开始游戏方块/显示").GetComponent<TextMeshPro>().text = "";
            showTime();
        }
        else
        {
            GameObject.Find("/开始游戏方块").GetComponent<MeshRenderer>().enabled = true;
            GameObject.Find("/开始游戏方块/显示").GetComponent<TextMeshPro>().text = "Start Game";
        }

        // Game timer logic
        if (realTime < 5f)
        {
            if (!isFirst)
            {
                realTime += Time.deltaTime;
                finalEndTime += Time.deltaTime;
            }
        }

        // Game end after 60s
        if (finalEndTime >= 60)
        {
            goend = true;
        }

        // Spawn blocks (4s interval)
        if (!goend && realTime >= 4f)
        {
            addNewNeed();
            createBlockS();
            realTime = 0f;
        }

        // Show end screen after 5s of game end
        if (goend && realTime >= 5f)
        {
            realTime = 5f;
            showEnd();
        }

        // Update combo/error UI
        showLinkAndError();
    }

    // Spawn all blocks (1 correct + 3 distractors)
    public void createBlockS()
    {
        if (nowListNumbr < allTrueWordUsingSHOW.Count)
        {
            while (createNumber != 4)
            {
                createBlockBYINT(createNumber);
                createNumber++;
            }

            if (createNumber == 4)
            {
                createNumber = 0;

                int runeIndex = nowListNumbr % TOTAL_RUNE_COUNT;
                if (runeIndex >= 0 && runeIndex < allsoundTone.Length)
                {
                    playsound(runeIndex);
                }
                nowListNumbr++;
            }
        }
    }

    // Spawn single block by index (0=correct, 1-3=distractors)
    public void createBlockBYINT(int a)
    {
        GameObject go = Instantiate(Newblock, toraw.transform);

        if (a == 0)
        {
            go.name = "方块_" + this.nowListNumbr + "_1";
            go.GetComponent<BlockMoveArrow>().setAll(allTrueWordUsingSHOW[this.nowListNumbr]);
        }
        else if (a == 1)
        {
            go.name = "方块_" + this.nowListNumbr + "_2";
            go.GetComponent<BlockMoveArrow>().setAll(falseBlock1[this.nowListNumbr]);
        }
        else if (a == 2)
        {
            go.name = "方块_" + this.nowListNumbr + "_3";
            go.GetComponent<BlockMoveArrow>().setAll(falseBlock2[this.nowListNumbr]);
        }
        else if (a == 3)
        {
            go.name = "方块_" + this.nowListNumbr + "_4";
            go.GetComponent<BlockMoveArrow>().setAll(falseBlock3[this.nowListNumbr]);
        }

        // Enable block rendering and movement
        go.GetComponent<MeshRenderer>().enabled = true;
        go.GetComponent<BlockMoveArrow>().enabled = true;
    }

    // Update combo and error statistics UI
    public void showLinkAndError()
    {
        TextMeshPro comboText = GameObject.Find("/黑色背景测试/得分")?.GetComponent<TextMeshPro>();
        if (comboText != null)
        {
            comboText.text = linkNumber + " combo";
        }

        if (havehit != 0)
        {
            TextMeshPro errorText = GameObject.Find("/黑色背景测试/错误次数")?.GetComponent<TextMeshPro>();
            if (errorText != null)
            {
                int correctCount = havehit - allErrorShow.Count;
                float accuracy = (float)correctCount / havehit * 100;
                errorText.text = $"{correctCount} / {havehit}\n{accuracy:F1}%";
            }
        }
    }

    // Process correct/incorrect hit logic
    public void choiceOrFalse(int number, bool Tf)
    {
        if (Tf)
        {
            linkNumber++;
            TextMeshPro feedbackText = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();
            if (feedbackText != null)
            {
                feedbackText.text = $"the{number}is right";
            }
        }
        else
        {
            if (allErrorShow.IndexOf(number) == -1)
            {
                allErrorShow.Add(number);
                linkNumber = 0;
                TextMeshPro feedbackText = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();
                if (feedbackText != null)
                {
                    feedbackText.text = $"the {number}is error";
                }
            }
        }
    }

    // Play rune pronunciation audio (Mode 4 only)
    public void playsound(int a)
    {
        if (gameModel == 4 && a >= 0 && a < allsoundTone.Length)
        {
            AudioSource audioSource = GameObject.Find("/地板")?.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(allsoundTone[a], 1.5f);
            }
        }
    }

    // Play background music on first run
    public void firstPlay()
    {
        AudioSource audioSource = transform.GetComponent<AudioSource>();
        if (audioSource != null && !audioSource.enabled)
        {
            audioSource.enabled = true;
            audioSource.Play();
        }
    }

    // Play hit feedback sound
    public void PlaySoundhit(bool isCorrect)
    {
        AudioSource audioSource = GameObject.Find("/地板")?.GetComponent<AudioSource>();
        if (audioSource == null) return;

        if (isCorrect && truesound.Length > 0)
        {
            audioSource.PlayOneShot(truesound[0]);
        }
        else if (!isCorrect && truesound.Length > 1)
        {
            audioSource.PlayOneShot(truesound[1]);
        }
    }

    // Show game end screen
    public void showEnd()
    {
        
        AudioSource bgmSource = transform.GetComponent<AudioSource>();
        if (bgmSource != null)
        {
            bgmSource.enabled = false;
        }

        string endText = endString();
        TextMeshPro endTextUI = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();
        if (endTextUI != null)
        {
            endTextUI.text = endText;
        }
    }

    // Show remaining game time (60s countdown)
    public void showTime()
    {
        int remainingTime = (int)(60 - finalEndTime);
        TextMeshPro timeText = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();
        if (timeText != null)
        {
            timeText.text = $"{remainingTime}s";
        }
    }

    // Generate end screen text (show incorrect runes)
    public string endString()
    {
        string final = "";
        if (allErrorShow.Count == 0)
        {
            final = "Perfect! No mistakes";
        }
        else
        {
            final = "Incorrect Runes:\n";
            int errorCount = 0;

            foreach (int errorIndex in allErrorShow)
            {
                errorCount++;
                if (errorIndex >= 0 && errorIndex < allTrueWordUsingSHOW.Count)
                {
                    final += $"{errorCount}: {allTrueWordUsingSHOW[errorIndex].getShowString()}   ";
                    if (errorCount % 2 == 0)
                    {
                        final += "\n";
                    }
                }
            }

            // Add accuracy statistics
            int totalAttempts = havehit;
            int correctAttempts = totalAttempts - allErrorShow.Count;
            float accuracy = totalAttempts > 0 ? (float)correctAttempts / totalAttempts * 100 : 0;
            final += $"\n\nAccuracy: {accuracy:F1}%\nTotal Attempts: {totalAttempts}";
        }
        return final;
    }

    // Restart game (reset all state)
    public void restart()
    {
        if (goend)
        {
            ralUseWord = new List<word>();
            goend = false;
            allErrorShow.Clear();
            linkNumber = 0;
            gameModel = 4; // Force archery mode to Rune listening

            // Reset block lists
            allTrueWordUsingSHOW = new List<block>();
            falseBlock1 = new List<block>();
            falseBlock2 = new List<block>();
            falseBlock3 = new List<block>();

            // Reset timers and counters
            finalEndTime = 0;
            realTime = 0f;
            nowListNumbr = 0;
            havehit = 0;
            createNumber = 0;

            // Clear UI
            TextMeshPro errorText = GameObject.Find("/黑色背景测试/错误次数")?.GetComponent<TextMeshPro>();
            if (errorText != null) errorText.text = "";

            TextMeshPro feedbackText = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();
            if (feedbackText != null) feedbackText.text = "";
            isFirst = false;
        }
    }

    // Update display text (empty for Mode 4 - no hints)
    public void setTrueSHow()
    {
        // Mode 4: No hints (listen only)
        if (gameModel == 4)
        {
            TextMeshPro displayText = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();
            if (displayText != null && goend)
            {
                displayText.text = "";
            }
            return;
        }

        // Preserve original logic for other modes (unused in archery mode)
        string useAllString = "";
        for (int i = havehit; i < nowListNumbr; i++)
        {
            if (gameModel == 1 && i < ralUseWord.Count)
            {
                useAllString += ralUseWord[i].getRune() + "\n";
            }
            if (gameModel == 2 && i < ralUseWord.Count)
            {
                useAllString += ralUseWord[i].getEnglish() + "\n";
            }
            if (gameModel == 3 && i < ralUseWord.Count)
            {
                useAllString += ralUseWord[i].getRune() + "\n";
            }
        }

        TextMeshPro modeText = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();
        if (modeText != null)
        {
            modeText.text = useAllString;
        }
    }

    // Return to main menu
    public void backToMenu()
    {
        SceneManager.LoadScene("StartMenu");
    }

    // Core: Generate rune blocks (replace phonetic logic with rune symbols)
    public void addNewNeed()
    {
        if (gameModel == 4)
        {
            List<int> usedRuneIndices = new List<int>();
            List<int> usedPositions = new List<int>();

            // Randomly select target rune (0-23)
            int targetRuneIndex = Random.Range(0, TOTAL_RUNE_COUNT);
            usedRuneIndices.Add(targetRuneIndex);

            // Random spawn position (0-9)
            int targetPosition = Random.Range(0, 10);
            usedPositions.Add(targetPosition);

            // Create correct block (show rune symbol)
            allTrueWordUsingSHOW.Add(new block(
                runeSymbols[targetRuneIndex], // Display rune symbol
                true,                          // Correct answer
                0,                             // Spawn time (unused in archery mode)
                targetPosition,                // Spawn position
                allTrueWordUsingSHOW.Count     // Rune index
            ));

            // Generate 3 distractor blocks (different runes + unique positions)
            while (usedRuneIndices.Count < 4)
            {
                // Get unique rune index
                int randomRuneIndex = Random.Range(0, TOTAL_RUNE_COUNT);
                while (usedRuneIndices.Contains(randomRuneIndex))
                {
                    randomRuneIndex = Random.Range(0, TOTAL_RUNE_COUNT);
                }
                usedRuneIndices.Add(randomRuneIndex);

                // Get unique spawn position
                int randomPosition = Random.Range(0, 10);
                while (usedPositions.Contains(randomPosition))
                {
                    randomPosition = Random.Range(0, 10);
                }
                usedPositions.Add(randomPosition);

                // Create distractor block
                string distractorRune = runeSymbols[randomRuneIndex];
                int blockCount = usedRuneIndices.Count;

                if (blockCount == 2)
                {
                    falseBlock1.Add(new block(distractorRune, false, 0, randomPosition, allTrueWordUsingSHOW.Count - 1));
                }
                else if (blockCount == 3)
                {
                    falseBlock2.Add(new block(distractorRune, false, 0, randomPosition, allTrueWordUsingSHOW.Count - 1));
                }
                else if (blockCount == 4)
                {
                    falseBlock3.Add(new block(distractorRune, false, 0, randomPosition, allTrueWordUsingSHOW.Count - 1));
                }
            }
        }
    }
}