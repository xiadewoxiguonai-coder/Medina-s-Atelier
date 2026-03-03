using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR.InteractionSystem;
using UnityEngine.SceneManagement;
using System.IO;

// 24 Core Rune Symbols Enum (mapped to Unicode characters)
public enum RuneType
{
    Fehu, Uruz, Thurisaz, Ansuz, Raidho, Kenaz, Gebo, Wunjo,
    Hagalaz, Nauthiz, Isa, Jera, Eihwaz, Perthro, Algiz, Sowilo,
    Tiwaz, Berkano, Ehwaz, Mannaz, Laguz, Ingwaz, Dagaz, Othala
}

public class StartGame : MonoBehaviour
{
    // Core Configuration: Fixed 24 Rune Symbols
    private const int TOTAL_RUNE_COUNT = 24;

    // Data Management
    private GameCreater useCreater;
    public int gameModel; //1:Rune-English, 2:English-Rune, 3:See English Type Rune Phonetic, 4:Hear Rune Sound Type Phonetic
    private List<block> allTrueWordUsingSHOW;// Correct rune blocks
    private List<block> falseBlock1;// Distractor block 1
    private List<block> falseBlock2;// Distractor block 2
    private List<block> falseBlock3;// Distractor block 3
    public string[] allToneSingle;// Rune phonetic symbol mapping table
    public int nowListNumbr = 0;// Current rune index
    private float paruseTime = 3f;// Default pause time
    public float nextCreateTime = 0f;
    public float realTime = 0f;
    public GameObject Newblock;// Block prefab
    public GameObject toraw;// Block parent container
    public List<int> allErrorShow;// Incorrect rune indices
    public int createNumber = 0;// Block creation counter
    public List<word> ralUseWord;// 24 rune data entries
    public int linkNumber = 0;// Combo count
    public bool goend = false;
    public AudioClip[] allsoundTone;// Rune pronunciation audio clips
    public AudioClip[] truesound;// Correct/Incorrect feedback sounds
    public int havehit = 0;// Number of hit blocks
    public bool haveSaved = false;

    // Rune Unicode Symbol Table (Core: Replaces Chinese characters)
    private string[] runeSymbols = {
        "ᚠ", "ᚢ", "ᚦ", "ᚨ", "ᚱ", "ᚲ", "ᚷ", "ᚹ",
        "ᚺ", "ᚾ", "ᛁ", "ᛃ", "ᛇ", "ᛈ", "ᛉ", "ᛋ",
        "ᛏ", "ᛒ", "ᛖ", "ᛗ", "ᛚ", "ᛜ", "ᛞ", "ᛟ"
    };

    // Initialization
    void Start()
    {
        allErrorShow = new List<int>();
        goend = true;
        restart();
    }

    // Frame Update
    void Update()
    {
        firstPlay();
        setTrueSHow(); // Fixed display logic

        if (realTime < 5f)
        {
            realTime += Time.deltaTime;
        }

        // Spawn blocks
        if (!goend && realTime >= nextCreateTime)
        {
            createBlockS();
            realTime = 0f;
        }

        // Game end condition
        if (goend && realTime >= 5f)
        {
            realTime = 5f;
            showEnd();
        }

        // Display combo and error stats
        showLinkAndError();
    }

    // Spawn all blocks (Preserves original logic + null safety)
    public void createBlockS()
    {
        // ========== Mode 4: Play rune pronunciation (Preserve your logic) ==========
        if (gameModel == 4)
        {
            int runeIndex = nowListNumbr % TOTAL_RUNE_COUNT;
            if (runeIndex >= 0 && runeIndex < allsoundTone.Length)
            {
                playsound(runeIndex); // Play corresponding rune pronunciation
            }
        }

        if (allTrueWordUsingSHOW == null || nowListNumbr >= allTrueWordUsingSHOW.Count)
        {
            goend = true;
            return;
        }

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

                if (gameModel != 4)
                {
                    int soundIndex = System.Array.IndexOf(allToneSingle, allTrueWordUsingSHOW[nowListNumbr].getShowString());
                    if (soundIndex >= 0 && soundIndex < allsoundTone.Length)
                    {
                        playsound(soundIndex);
                    }
                }

                nowListNumbr++;
                if (nowListNumbr < allTrueWordUsingSHOW.Count)
                {
                    nextCreateTime = allTrueWordUsingSHOW[nowListNumbr].getCreateTime();
                }
                else
                {
                    goend = true;
                }
            }
        }
    }

    public void createBlockBYINT(int a)
    {
        if (Newblock == null || toraw == null)
        {
            Debug.LogError("Newblock or toraw not assigned!");
            return;
        }

        GameObject go = Instantiate(Newblock, toraw.transform);

        if (a == 0)
        {
            go.name = "方块_" + this.nowListNumbr + "_1";
            go.GetComponent<BlockMove>().setAll(allTrueWordUsingSHOW[this.nowListNumbr]);
        }
        else if (a == 1)
        {
            go.name = "方块_" + this.nowListNumbr + "_2";
            go.GetComponent<BlockMove>().setAll(falseBlock1[this.nowListNumbr]);
        }
        else if (a == 2)
        {
            go.name = "方块_" + this.nowListNumbr + "_3";
            go.GetComponent<BlockMove>().setAll(falseBlock2[this.nowListNumbr]);
        }
        else if (a == 3)
        {
            go.name = "方块_" + this.nowListNumbr + "_4";
            go.GetComponent<BlockMove>().setAll(falseBlock3[this.nowListNumbr]);
        }

        // Enable rendering and movement
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = true;

        BlockMove bm = go.GetComponent<BlockMove>();
        if (bm != null) bm.enabled = true;
    }

    // Display combo and error statistics (Fixed percentage precision)
    public void showLinkAndError()
    {
        // Combo display
        TextMeshPro comboText = GameObject.Find("/黑色背景测试/得分")?.GetComponent<TextMeshPro>();
        if (comboText != null)
        {
            comboText.text = $"{linkNumber} combo";
        }

        // Error statistics
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

    // Correct/Incorrect judgment logic
    public void choiceOrFalse(int number, bool Tf)
    {
        if (Tf)
        {
            linkNumber++;
        }
        else
        {
            if (!allErrorShow.Contains(number))
            {
                allErrorShow.Add(number);
                linkNumber = 0;

                // Change background to red (null safety)
                backgroundColorChange bgColor = GameObject.Find("/[CameraRig]/Camera/Canvas/颜色")?.GetComponent<backgroundColorChange>();
                if (bgColor != null)
                {
                    bgColor.setRED();
                }
            }
        }
    }

    // Play rune pronunciation
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

    // First play background music
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

    // Show game end screen (Removed vocabulary book saving)
    public void showEnd()
    {
        AudioSource audioSource = transform.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.enabled = false;
        }

        string endText = endString();
        TextMeshPro endTextUI = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();
        if (endTextUI != null)
        {
            endTextUI.text = endText;
        }
    }

    // End screen text (Displays rune symbols)
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
                if (errorIndex >= 0 && errorIndex < TOTAL_RUNE_COUNT && errorIndex < ralUseWord.Count)
                {
                    errorCount++;
                    if (gameModel == 1 || gameModel == 2)
                    {
                        // Display rune symbol + English
                        final += $"{errorCount}: {ralUseWord[errorIndex].getRune()} / {ralUseWord[errorIndex].getEnglish()}\n";
                    }
                    else if (gameModel == 4)
                    {
                        final += $"{errorCount}: {allTrueWordUsingSHOW[errorIndex].getShowString()}\n";
                    }
                }
            }

            // Statistics
            int totalAttempts = havehit;
            int correctAttempts = totalAttempts - allErrorShow.Count;
            float accuracy = totalAttempts > 0 ? (float)correctAttempts / totalAttempts * 100 : 0;
            final += $"\nAccuracy: {accuracy:F1}%\nTotal Runes: {TOTAL_RUNE_COUNT}";
        }
        return final;
    }

    // Restart game
    public void restart()
    {
        if (goend)
        {
            ralUseWord = new List<word>();
            goend = false;
            allErrorShow.Clear();
            linkNumber = 0;
            havehit = 0;
            haveSaved = false;

            // Initialize game creator (Rune version)
            useCreater = new GameCreater(allToneSingle);
            //gameModel = int.Parse(PlayerPrefs.GetString("model"));
            useCreater.gameCreaterBYgameMode(gameModel);
            ralUseWord = useCreater.getUseList();

            // Get block lists (null safety)
            allTrueWordUsingSHOW = useCreater.getBlock1() ?? new List<block>();
            falseBlock1 = useCreater.getBlock2() ?? new List<block>();
            falseBlock2 = useCreater.getBlock3() ?? new List<block>();
            falseBlock3 = useCreater.getBlock4() ?? new List<block>();

            // Initialize spawn timing
            nextCreateTime = allTrueWordUsingSHOW.Count > 0 ? allTrueWordUsingSHOW[0].getCreateTime() : 0f;
            realTime = 0f;
            nowListNumbr = 0;
            createNumber = 0;

            // Clear UI text
            TextMeshPro errorText = GameObject.Find("/黑色背景测试/错误次数")?.GetComponent<TextMeshPro>();
            if (errorText != null) errorText.text = "";

            TextMeshPro correctText = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();
            if (correctText != null) correctText.text = "";
        }
    }

    // Core Fix: Display all unhit runes (no text accumulation)
    public void setTrueSHow()
    {
        string useAllString = "";
        TextMeshPro displayText = GameObject.Find("/黑色背景测试/第几个正确")?.GetComponent<TextMeshPro>();

        // Null safety
        if (displayText == null || ralUseWord == null || ralUseWord.Count == 0)
        {
            if (displayText != null) displayText.text = "";
            return;
        }

        // Iterate through "spawned but unprocessed" blocks (original logic)
        for (int i = havehit; i < nowListNumbr && i < ralUseWord.Count; i++)
        {
            if (gameModel == 1)
            {
                // Display rune symbol (replaces Chinese)
                useAllString += ralUseWord[i].getRune() + "\n";
            }
            else if (gameModel == 2)
            {
                // Display English
                useAllString += ralUseWord[i].getEnglish() + "\n";
            }
            else if (gameModel == 3)
            {
                // Display rune symbol
                useAllString += ralUseWord[i].getRune() + "\n";
            }
        }

        // Direct assignment (overwrites existing text, fixes accumulation)
        displayText.text = useAllString;
    }

    // Return to menu
    public void backToMenu()
    {
        if (goend)
        {
            SceneManager.LoadScene("StartMenu");
        }
    }
}