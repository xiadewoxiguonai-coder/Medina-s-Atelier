using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GameCreater
{
    // Core Constants (Rune Game Configuration)
    private const int TOTAL_RUNE_COUNT = 24;    // Fixed number of Elder Futhark runes
    private const int TOTAL_TONE_COUNT = 24;    // Matching phonetic symbols count
    private const int MAX_POSITION_RANGE = 10;  // Valid spawn positions (0-9)

    // Data Management
    private databaseword useAllWord;            // Master rune database (loaded from JSON)
    private GameUsingList useCheck;             // Word selection/filtering manager
    private List<word> gameUseWordList;         // Active runes for current game session
    private int gameModel;                      // Current game mode (1-4)
    private List<float> eachUsingTime = new List<float>(); // Spawn interval per rune
    private List<int> realList;                 // Randomized rune order list

    // Block Management (Correct + Distractor blocks)
    private List<block> allTrueWordUsingSHOW;   // Correct answer blocks
    private List<block> falseBlock1;            // Distractor block set 1
    private List<block> falseBlock2;            // Distractor block set 2
    private List<block> falseBlock3;            // Distractor block set 3

    // Phonetic & Rune Configuration
    public string[] allToneString;              // Phonetic symbol lookup table
    private int useWordNumber = TOTAL_RUNE_COUNT; // Number of runes per game (fixed at 24)

    // Elder Futhark Rune Unicode Symbols (core visual representation)
    private string[] runeSymbols = {
        "ᚠ", "ᚢ", "ᚦ", "ᚨ", "ᚱ", "ᚲ", "ᚷ", "ᚹ",
        "ᚺ", "ᚾ", "ᛁ", "ᛃ", "ᛇ", "ᛈ", "ᛉ", "ᛋ",
        "ᛏ", "ᛒ", "ᛖ", "ᛗ", "ᛚ", "ᛜ", "ᛞ", "ᛟ"
    };

    // Constructor: Initialize game creator with phonetic symbols
    public GameCreater(string[] a)
    {
        allToneString = a;

        // Initialize block lists
        allTrueWordUsingSHOW = new List<block>();
        falseBlock1 = new List<block>();
        falseBlock2 = new List<block>();
        falseBlock3 = new List<block>();
        realList = new List<int>();

        // Setup core game parameters
        setTimeListForRunes(); // Initialize spawn timing
        load(1);               // Load rune database (saveWord1.json)
        setUseCheck();         // Initialize word selection manager
    }

    // Initialize word selection filter (GameUsingList)
    public void setUseCheck()
    {
        useCheck = new GameUsingList();
        useCheck.setChoiceDatabase(useAllWord);
        useCheck.setUseWord(useWordNumber);
    }

    // Main game initialization: Create blocks based on selected game mode
    public void gameCreaterBYgameMode(int a)
    {
        gameModel = a;

        // Clear previous game session data
        allTrueWordUsingSHOW.Clear();
        falseBlock1.Clear();
        falseBlock2.Clear();
        falseBlock3.Clear();

        // Initialize blocks for selected game mode
        switch (gameModel)
        {
            case 1:
                CreatRuneEnglishModel(); // Mode 1: Rune → English translation
                break;
            case 2:
                CreatEnglishRuneModel(); // Mode 2: English → Rune translation
                break;
            case 3:
                CreatPhoneticModel();    // Mode 3: English → Phonetic symbol
                break;
            case 4:
                CreategameMode4();       // Mode 4: Listen to rune → Select rune symbol
                break;
        }
    }

    // Mode 1: Rune → English (Core Logic)
    // Flow: Show rune symbol → Correct block = English translation → Distractors = other English words
    public void CreatRuneEnglishModel()
    {
        useCheck.setListOnlyList();
        gameUseWordList = useCheck.getList();
        EnsureWordListLength(); // Ensure exactly 24 runes are loaded

        // Create randomized rune order
        List<int> uselist = new List<int>();
        for (int i = 0; i < useWordNumber; i++)
        {
            uselist.Add(i);
        }
        realList = randomCHange(uselist);

        // Generate blocks for each rune
        for (int ia = 0; ia < useWordNumber; ia++)
        {
            List<int> checkPositionN = new List<int>(); // Track used spawn positions
            List<int> useIdL = new List<int>();         // Track used word IDs (prevent duplicates)
            List<string> usedEnglish = new List<string>(); // Track used English translations
            int randomP = Random.Range(0, MAX_POSITION_RANGE);
            int blocknumber = 0; // Track number of blocks created (target: 4)

            // Create correct block (shows target English translation)
            string targetEnglish = gameUseWordList[ia].getEnglish();
            checkPositionN.Add(randomP);
            allTrueWordUsingSHOW.Add(new block(
                targetEnglish,          // Display English translation
                true,                   // Mark as correct answer
                eachUsingTime[ia],      // Spawn interval
                randomP,                // Spawn position
                ia                      // Rune index
            ));
            useIdL.Add(gameUseWordList[ia].getID());
            usedEnglish.Add(targetEnglish);
            blocknumber++;

            // Add distractor 1 (SID1 - secondary word ID)
            if (gameUseWordList[ia].getSid1() != -1 && !useIdL.Contains(gameUseWordList[ia].getSid1()))
            {
                randomP = GetUniqueRandomPosition(checkPositionN);
                for (int ib = 0; ib < useAllWord.allWord.Count; ib++)
                {
                    if (useAllWord.getword(ib).getID() == gameUseWordList[ia].getSid1())
                    {
                        string falseEnglish = useAllWord.getword(ib).getEnglish();
                        if (!usedEnglish.Contains(falseEnglish))
                        {
                            falseBlock1.Add(new block(
                                falseEnglish,
                                false,
                                eachUsingTime[ia],
                                randomP,
                                ia
                            ));
                            useIdL.Add(useAllWord.getword(ib).getID());
                            usedEnglish.Add(falseEnglish);
                            blocknumber++;
                            checkPositionN.Add(randomP);
                        }
                        break;
                    }
                }
            }

            // Add distractor 2 (SID2 - tertiary word ID)
            if (gameUseWordList[ia].getSid2() != -1 && !useIdL.Contains(gameUseWordList[ia].getSid2()))
            {
                randomP = GetUniqueRandomPosition(checkPositionN);
                for (int ib = 0; ib < useAllWord.allWord.Count; ib++)
                {
                    if (useAllWord.getword(ib).getID() == gameUseWordList[ia].getSid2())
                    {
                        string falseEnglish = useAllWord.getword(ib).getEnglish();
                        if (!usedEnglish.Contains(falseEnglish))
                        {
                            if (blocknumber == 1)
                            {
                                falseBlock1.Add(new block(falseEnglish, false, eachUsingTime[ia], randomP, ia));
                            }
                            else if (blocknumber == 2)
                            {
                                falseBlock2.Add(new block(falseEnglish, false, eachUsingTime[ia], randomP, ia));
                            }
                            useIdL.Add(useAllWord.getword(ib).getID());
                            usedEnglish.Add(falseEnglish);
                            blocknumber++;
                            checkPositionN.Add(randomP);
                        }
                        break;
                    }
                }
            }

            // Fill remaining blocks with random unique English distractors
            int randomInt;
            for (int i = blocknumber; i < 4; i++)
            {
                randomInt = GetUniqueRandomWordId(useIdL, useAllWord.allWord.Count, usedEnglish);
                randomP = GetUniqueRandomPosition(checkPositionN);

                string falseEnglish = useAllWord.getword(randomInt).getEnglish();
                switch (blocknumber)
                {
                    case 1:
                        falseBlock1.Add(new block(falseEnglish, false, eachUsingTime[ia], randomP, ia));
                        break;
                    case 2:
                        falseBlock2.Add(new block(falseEnglish, false, eachUsingTime[ia], randomP, ia));
                        break;
                    case 3:
                        falseBlock3.Add(new block(falseEnglish, false, eachUsingTime[ia], randomP, ia));
                        break;
                }

                useIdL.Add(randomInt);
                usedEnglish.Add(falseEnglish);
                checkPositionN.Add(randomP);
                blocknumber++;
            }
        }
    }

    // Mode 2: English → Rune (Core Logic)
    // Flow: Show English word → Correct block = Rune symbol → Distractors = other rune symbols
    public void CreatEnglishRuneModel()
    {
        useCheck.setListOnlyList();
        gameUseWordList = useCheck.getList();
        EnsureWordListLength();

        // Create randomized rune order
        List<int> uselist = new List<int>();
        for (int i = 0; i < useWordNumber; i++)
        {
            uselist.Add(i);
        }
        realList = randomCHange(uselist);

        // Generate blocks for each rune
        for (int ia = 0; ia < useWordNumber; ia++)
        {
            List<int> checkPositionN = new List<int>(); // Track used spawn positions
            List<int> useIdL = new List<int>();         // Track used word IDs
            List<string> checkRunes = new List<string>(); // Track used rune symbols
            int randomP = Random.Range(0, MAX_POSITION_RANGE);
            int blocknumber = 0; // Track number of blocks created

            // Create correct block (shows target rune symbol)
            string trueRune = gameUseWordList[ia].getRune();
            checkPositionN.Add(randomP);
            allTrueWordUsingSHOW.Add(new block(
                trueRune,               // Display rune symbol
                true,                   // Mark as correct answer
                eachUsingTime[ia],      // Spawn interval
                randomP,                // Spawn position
                ia                      // Rune index
            ));
            useIdL.Add(gameUseWordList[ia].getID());
            checkRunes.Add(trueRune);
            blocknumber++;

            // Add distractor (SID2 - tertiary word ID)
            if (gameUseWordList[ia].getSid2() != -1)
            {
                int sid2Index = gameUseWordList[ia].getSid2();
                string sid2Rune = useAllWord.getword(sid2Index).getRune();
                if (!checkRunes.Contains(sid2Rune))
                {
                    randomP = GetUniqueRandomPosition(checkPositionN);
                    for (int ib = 0; ib < useAllWord.allWord.Count; ib++)
                    {
                        if (useAllWord.getword(ib).getID() == gameUseWordList[ia].getSid2())
                        {
                            if (blocknumber == 1)
                            {
                                falseBlock1.Add(new block(useAllWord.getword(ib).getRune(), false, eachUsingTime[ia], randomP, ia));
                            }
                            else if (blocknumber == 2)
                            {
                                falseBlock2.Add(new block(useAllWord.getword(ib).getRune(), false, eachUsingTime[ia], randomP, ia));
                            }
                            useIdL.Add(useAllWord.getword(ib).getID());
                            checkRunes.Add(sid2Rune);
                            blocknumber++;
                            checkPositionN.Add(randomP);
                            break;
                        }
                    }
                }
            }

            // Fill remaining blocks with random unique rune distractors
            int randomInt;
            for (int i = blocknumber; i < 4; i++)
            {
                randomInt = GetUniqueRandomWordId(useIdL, useAllWord.allWord.Count, checkRunes);
                randomP = GetUniqueRandomPosition(checkPositionN);

                switch (blocknumber)
                {
                    case 1:
                        falseBlock1.Add(CreateFalseBlock_Rune(randomInt, ia, randomP));
                        break;
                    case 2:
                        falseBlock2.Add(CreateFalseBlock_Rune(randomInt, ia, randomP));
                        break;
                    case 3:
                        falseBlock3.Add(CreateFalseBlock_Rune(randomInt, ia, randomP));
                        break;
                }

                useIdL.Add(randomInt);
                checkPositionN.Add(randomP);
                checkRunes.Add(useAllWord.getword(randomInt).getRune());
                blocknumber++;
            }
        }
    }

    // Mode 3: English → Phonetic Symbol (Core Logic)
    // Flow: Show English word → Correct block = Phonetic symbol → Distractors = other phonetic symbols
    public void CreatPhoneticModel()
    {
        useCheck.setListOnlyList();
        gameUseWordList = useCheck.getList();
        EnsureWordListLength();

        // Generate blocks for each rune
        for (int ia = 0; ia < useWordNumber; ia++)
        {
            List<int> checkPositionN = new List<int>(); // Track used spawn positions
            List<int> useToneIds = new List<int>();     // Track used phonetic IDs
            int randomP = Random.Range(0, MAX_POSITION_RANGE);
            int blocknumber = 0; // Track number of blocks created

            // Get correct phonetic symbol ID (fallback to random if not found)
            int trueToneId = System.Array.IndexOf(allToneString, gameUseWordList[ia].gettone());
            if (trueToneId == -1) trueToneId = Random.Range(0, TOTAL_TONE_COUNT);

            // Create correct block (shows target phonetic symbol)
            checkPositionN.Add(randomP);
            useToneIds.Add(trueToneId);
            allTrueWordUsingSHOW.Add(new block(
                allToneString[trueToneId],
                true,
                eachUsingTime[ia],
                randomP,
                ia
            ));
            blocknumber++;

            // Fill remaining blocks with random unique phonetic distractors
            while (blocknumber < 4)
            {
                int randomToneId = Random.Range(0, TOTAL_TONE_COUNT);
                while (useToneIds.Contains(randomToneId))
                {
                    randomToneId = Random.Range(0, TOTAL_TONE_COUNT);
                }

                randomP = GetUniqueRandomPosition(checkPositionN);
                useToneIds.Add(randomToneId);
                checkPositionN.Add(randomP);

                if (blocknumber == 1)
                {
                    falseBlock1.Add(new block(allToneString[randomToneId], false, eachUsingTime[ia], randomP, ia));
                }
                else if (blocknumber == 2)
                {
                    falseBlock2.Add(new block(allToneString[randomToneId], false, eachUsingTime[ia], randomP, ia));
                }
                else if (blocknumber == 3)
                {
                    falseBlock3.Add(new block(allToneString[randomToneId], false, eachUsingTime[ia], randomP, ia));
                }

                blocknumber++;
            }
        }
    }

    // Mode 4: Listen to Rune → Select Rune Symbol (Core Logic)
    // Flow: Play rune pronunciation → Correct block = Rune symbol → Distractors = other rune symbols
    public void CreategameMode4()
    {
        for (int i = 0; i < TOTAL_RUNE_COUNT; i++)
        {
            List<int> checkNotSame = new List<int>(); // Track used rune indices
            List<int> checkPositionN = new List<int>(); // Track used spawn positions

            // Get target rune symbol (Unicode)
            string runeSymbol = runeSymbols[i];
            checkNotSame.Add(i); // Prevent duplicate rune selection
            int randomP = Random.Range(0, MAX_POSITION_RANGE);
            checkPositionN.Add(randomP);

            // Get spawn interval (fallback to 2s if missing)
            float spawnInterval = eachUsingTime.Count > i ? eachUsingTime[i] : 2f;

            // Create correct block (shows target rune symbol)
            allTrueWordUsingSHOW.Add(new block(
                runeSymbol,         // Display rune symbol
                true,               // Mark as correct answer
                spawnInterval,      // Spawn interval
                randomP,            // Spawn position
                i                   // Rune index
            ));

            // Fill remaining blocks with random unique rune distractors
            while (checkNotSame.Count < 4)
            {
                int randomRuneIndex = Random.Range(0, TOTAL_RUNE_COUNT);
                while (checkNotSame.Contains(randomRuneIndex))
                {
                    randomRuneIndex = Random.Range(0, TOTAL_RUNE_COUNT);
                }
                checkNotSame.Add(randomRuneIndex);
                randomP = GetUniqueRandomPosition(checkPositionN);
                checkPositionN.Add(randomP);

                string falseRune = runeSymbols[randomRuneIndex];
                switch (checkNotSame.Count)
                {
                    case 2:
                        falseBlock1.Add(new block(falseRune, false, spawnInterval, randomP, i));
                        break;
                    case 3:
                        falseBlock2.Add(new block(falseRune, false, spawnInterval, randomP, i));
                        break;
                    case 4:
                        falseBlock3.Add(new block(falseRune, false, spawnInterval, randomP, i));
                        break;
                }
            }
        }
    }

    // Helper: Get unique random word ID (Mode 1 - English distractors)
    // Ensures no duplicate English translations in blocks
    private int GetUniqueRandomWordId(List<int> usedIds, int maxCount, List<string> usedEnglish)
    {
        int randomInt = Random.Range(0, maxCount);
        while (usedIds.Contains(randomInt) || usedEnglish.Contains(useAllWord.getword(randomInt).getEnglish()))
        {
            randomInt = Random.Range(0, maxCount);
        }
        return randomInt;
    }

    // Helper: Create distractor block (Mode 2 - Rune symbols)
    // Returns pre-configured distractor block with rune symbol
    private block CreateFalseBlock_Rune(int wordId, int runeIndex, int position)
    {
        // Fallback to random word ID if out of bounds
        if (wordId < 0 || wordId >= useAllWord.allWord.Count)
        {
            wordId = Random.Range(0, useAllWord.allWord.Count);
        }

        return new block(
            useAllWord.getword(wordId).getRune(), // Display rune symbol
            false,                                   // Mark as distractor
            eachUsingTime[runeIndex],                // Spawn interval
            position,                                // Spawn position
            runeIndex                                // Rune index
        );
    }

    // Helper: Get unique random spawn position (prevents overlapping blocks)
    private int GetUniqueRandomPosition(List<int> usedPositions)
    {
        int randomP = Random.Range(0, MAX_POSITION_RANGE);
        while (usedPositions.Contains(randomP))
        {
            randomP = Random.Range(0, MAX_POSITION_RANGE);
        }
        return randomP;
    }

    // Helper: Get unique random word ID (generic - no translation check)
    private int GetUniqueRandomWordId(List<int> usedIds, int maxCount)
    {
        int randomInt = Random.Range(0, maxCount);
        while (usedIds.Contains(randomInt))
        {
            randomInt = Random.Range(0, maxCount);
        }
        return randomInt;
    }

    // Safety Check: Ensure word list has exactly 24 entries (matches TOTAL_RUNE_COUNT)
    private void EnsureWordListLength()
    {
        if (gameUseWordList == null) gameUseWordList = new List<word>();

        // Trim list if too long
        if (gameUseWordList.Count > TOTAL_RUNE_COUNT)
            gameUseWordList = gameUseWordList.GetRange(0, TOTAL_RUNE_COUNT);
        // Pad list with last entry if too short (prevents index errors)
        else if (gameUseWordList.Count < TOTAL_RUNE_COUNT && gameUseWordList.Count > 0)
        {
            word lastWord = gameUseWordList[gameUseWordList.Count - 1];
            while (gameUseWordList.Count < TOTAL_RUNE_COUNT)
            {
                gameUseWordList.Add(lastWord);
            }
        }
    }

    // Initialize spawn intervals for runes (variable timing for game difficulty)
    private void setTimeListForRunes()
    {
        eachUsingTime.Clear();
        for (int i = 0; i < TOTAL_RUNE_COUNT; i++)
        {
            float interval = 2f; // Base interval (2 seconds)
            // Varied intervals for dynamic gameplay
            if (i % 3 == 0) interval = 1f;
            if (i % 4 == 0) interval = 3f;
            if (i % 6 == 0) interval = 2.5f;
            eachUsingTime.Add(interval);
        }
    }

    // Load rune database from JSON (replace Chinese field with Unicode rune symbols)
    public void load(int a)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, $"saveWord{a}.json");

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            if (!string.IsNullOrEmpty(json))
            {
                useAllWord = JsonUtility.FromJson<databaseword>(json);
                // Override Chinese field with Unicode rune symbols (core visual fix)
                for (int i = 0; i < useAllWord.allWord.Count && i < TOTAL_RUNE_COUNT; i++)
                {
                    word tempWord = useAllWord.allWord[i];
                    useAllWord.allWord[i] = new word(
                        tempWord.getID(),
                        tempWord.getEnglish(), // Preserve English translation
                        runeSymbols[i],        // Replace Chinese with rune symbol
                        tempWord.gettone(),    // Preserve phonetic symbol
                        tempWord.getSid1(),    // Preserve distractor ID 1
                        tempWord.getSid2()     // Preserve distractor ID 2
                    );
                }
            }
        }
        else
        {
            Debug.LogWarning("Rune database not found - creating default Elder Futhark database");
            CreateDefaultRuneDatabase();
        }
    }

    // Create fallback default rune database (if JSON file is missing)
    private void CreateDefaultRuneDatabase()
    {
        useAllWord = new databaseword();
        string[] runeNames = System.Enum.GetNames(typeof(RuneType)); // Get rune names from enum

        // Populate database with 24 Elder Futhark runes
        for (int i = 0; i < TOTAL_RUNE_COUNT; i++)
        {
            useAllWord.allWord.Add(new word(
                i,
                runeNames[i],                    // English rune name (e.g., Fehu)
                runeSymbols[i],                  // Unicode rune symbol
                allToneString.Length > i ? allToneString[i] : "", // Phonetic symbol
                (i + 1) % TOTAL_RUNE_COUNT,      // Distractor ID 1 (next rune)
                (i + 2) % TOTAL_RUNE_COUNT       // Distractor ID 2 (second next rune)
            ));
        }
    }

    // Randomize list order (Fisher-Yates shuffle variant)
    public List<int> randomCHange(List<int> a)
    {
        for (int i = 0; i < a.Count; i++)
        {
            int randomP = i;
            // Ensure different random position (no self-swap)
            while (randomP == i) randomP = Random.Range(0, a.Count);
            // Swap elements
            int temp = a[i];
            a[i] = a[randomP];
            a[randomP] = temp;
        }
        return a;
    }

    // Convert phonetic ID to string (safety check for out-of-bounds indices)
    public string changeIntToToneString(int a)
    {
        return a >= 0 && a < allToneString.Length ? allToneString[a] : "";
    }

    public void set20List() => setTimeListForRunes();

    // Public getters (expose game data to StartGame manager)
    public List<block> getBlock1() => allTrueWordUsingSHOW;  // Correct blocks
    public List<block> getBlock2() => falseBlock1;           // Distractor blocks 1
    public List<block> getBlock3() => falseBlock2;           // Distractor blocks 2
    public List<block> getBlock4() => falseBlock3;           // Distractor blocks 3
    public List<float> getTimeList() => eachUsingTime;       // Spawn intervals
    public void setToneList(string[] a) => allToneString = a;// Update phonetic symbols
    public List<word> getUseList() => gameUseWordList;       // Active rune list
}