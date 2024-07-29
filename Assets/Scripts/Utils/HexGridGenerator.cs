using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class HexagonTerrainGenerator : MonoBehaviour
{
    public GameObject hexPrefab;
    public GameObject hexPrefabPlain;
    public List<GameObject> hexPrefabForest;
    public GameObject hexPrefabSand;
    public GameObject hexPrefabWater;
    public List<GameObject> ListAnimalsPlainPrefab;
    public List<GameObject> ListAnimalsForestPrefab;
    public List<GameObject> ListAnimalsSandPrefab;
    public List<GameObject> ListAnimalsWaterPrefab;
    public GameObject animalPrefab;
    public int gridRow = 24;
    public int gridColumn = 24;
    public int minHeight;
    public int maxHeight;
    public float heightFactor;
    public float noiseScale = 0.1f;
    public float wait = 0.001f;
    public float adjustmentFactor;
    private Dictionary<(int, int), int[]> terrainGrid;
    private float hexSize;
    private float horizontalSpacing = 1.496f;
    private float verticalSpacing = 1.726f;
    private float plainFocusPercent, forestFocusPercent, waterFocusPercent;
    private int plainCount, sandCount, forestCount, waterCount;
    private bool isGenerating = false;
    private int counterFillHoles;
    private GameObject combinedGrid;
    private float simplificationQuality = 0.15f;

    private void Start()
    {
        StartCoroutine(GenerateHexGrid());
    }

    public void OnClickRegenerate()
    {
        if(!isGenerating) RegenerateHexGrid();
    }

    private void SetTerrainArchetype()
    {
        float plainRandom = Random.Range(0.60f, 0.80f);
        float forestRandom = Random.Range(0.15f, 0.25f);
        float waterRandom = Random.Range(0.05f, 0.15f);

        float totalRandom = plainRandom + forestRandom + waterRandom;
        plainFocusPercent = plainRandom / totalRandom;
        forestFocusPercent = forestRandom / totalRandom;
        waterFocusPercent = waterRandom / totalRandom;

        int totalCases = 576;
        int plainExpected = Mathf.RoundToInt(plainFocusPercent * totalCases);
        int forestExpected = Mathf.RoundToInt(forestFocusPercent * totalCases);
        int waterExpected = Mathf.RoundToInt(waterFocusPercent * totalCases);

        Debug.Log("Cases pour plaine: " + plainExpected + ", Cases pour forêt: " + forestExpected + ", Cases pour eau: " + waterExpected);
    }

    private IEnumerator GenerateHexGrid()
    {
        isGenerating = true;
        SetTerrainArchetype();
        plainCount = 0;
        forestCount = 0;
        waterCount = 0;
        sandCount = 0;

        terrainGrid = new Dictionary<(int, int), int[]>();

        for (int row = 0; row < gridRow; row++)
        {
            for (int column = 0; column < gridColumn; column++)
            {
                terrainGrid.Add((row, column), new int[4] { -1, -1, -1, -1 });
            }
        }

        Random.InitState(System.DateTime.Now.Millisecond);

        for (int row = 0; row < gridRow / 2; row++)
        {
            GameObject rowObject = new GameObject("Row" + row);
            GameObject rowObjectSymmetry = new GameObject("Row" + (gridRow - 1 - row));
            rowObject.transform.SetParent(transform);
            rowObjectSymmetry.transform.SetParent(transform);

            for (int column = 0; column < gridColumn; column++)
            {
                Vector3 pos = CalculateHexPosition(row, column);
                Vector3 posSymmetry = CalculateHexPosition(gridRow - 1 - row, gridColumn - 1 - column);
                SetTerrainType(row, column, pos, rowObject, gridRow - 1 - row, gridColumn - 1 - column, posSymmetry, rowObjectSymmetry);
                //yield return null;
            }
        }
        counterFillHoles = 0;
        FillHolls();
        CountTerrain();
        yield return null;

        RemoveIsolatedForest();

        SpawnHex();
        yield return null;

        SetTerrainHeight();

        RefineHeight();

        Debug.Log("Plains: " + plainCount + ", Forests: " + forestCount + ", Water: " + waterCount + ", Sand: " + sandCount);
        Debug.Log("Plaines: " + (float)((plainCount / 576.0f) * 100) + "% " + "Forets: " + (float)((forestCount / 576.0f) * 100) + "% " + " " + "Eau: " + (float)((waterCount / 576.0f) * 100) + "% " + " " + "Sable: " + (float)((sandCount / 576.0f) * 100) + "% " + " ");
        yield return null;
        combinedGrid = transform.GetComponent<MeshCombiner>().CombineMeshesRecursive(transform.gameObject);
        isGenerating = false;
    }

    private void RegenerateHexGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        Destroy(combinedGrid);
        StartCoroutine(GenerateHexGrid());
    }

    private Vector3 CalculateHexPosition(int row, int column)
    {
        hexSize = hexPrefabPlain.GetComponent<MeshRenderer>().bounds.size.x / 2;
        float posZ;
        if (row % 2 != 0)
        {
            posZ = column * verticalSpacing * hexSize - verticalSpacing * hexSize / 2;
        }
        else
        {
            posZ = column * verticalSpacing * hexSize;
        }
        float posX = row * horizontalSpacing * hexSize;
        float posY = 0;

        return new Vector3(posX, posY, posZ);
    }

    private void SetTerrainType(int row, int column, Vector3 hexPos, GameObject rowObject, int rowSymmetry, int columnSymmetry, Vector3 hexPosSymmetry, GameObject rowObjectSymmetry)
    {
        int totalTerrains = plainCount + forestCount + waterCount;
        float plainPercent = plainFocusPercent;
        float forestPercent = forestFocusPercent;
        float waterPercent = waterFocusPercent;
        float plainPercentNorm = plainFocusPercent;
        float forestPercentNorm = forestFocusPercent;
        float waterPercentNorm = waterFocusPercent;
        float terrainRand = Random.Range(0f, 1f);
        int terrainType; // Par défaut, terrain de plaines

        if(totalTerrains > 0)
        {
            // Adjust probabilities based on difference from target counts
            float plainCountAdjustment = Mathf.Clamp01(plainCount / (float)(plainFocusPercent * totalTerrains) - 1f);
            float forestCountAdjustment = Mathf.Clamp01(forestCount / (float)(forestFocusPercent * totalTerrains) - 1f);
            float waterCountAdjustment = Mathf.Clamp01(waterCount / (float)(waterFocusPercent * totalTerrains) - 1f);
        
            plainPercentNorm *= (1f - plainCountAdjustment * adjustmentFactor);
            forestPercentNorm *= (1f - forestCountAdjustment * adjustmentFactor);
            waterPercentNorm *= (1f - waterCountAdjustment * adjustmentFactor);

            float sumProbabilities = plainPercentNorm + forestPercentNorm + waterPercentNorm;

            plainPercentNorm /= sumProbabilities;
            forestPercentNorm /= sumProbabilities;
            waterPercentNorm /= sumProbabilities;
        }
        if (terrainRand <= plainPercentNorm)
        {
            terrainType = 0; // Plaines 
        }
        else if (terrainRand <= plainPercentNorm + forestPercentNorm)
        {
            terrainType = 1; // Forets 
        }
        else
        {
            terrainType = 3; // Eau 
        }

        Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)> adjacentForestGroupSize = CountListAdjacentGroupSizes(row, column, 1); // Taille des groupes de forêt adjacente
        Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)> adjacentWaterGroupSize = CountListAdjacentGroupSizes(row, column, 3); // Taille des groupe d'eau adjacente

        foreach (var forestGroup in adjacentForestGroupSize)
        {
            (int originRow, int originColumn) = forestGroup.Key;
            (int groupSize, List<(int, int)> groupPositions) = adjacentForestGroupSize[forestGroup.Key];

            if (terrainType != 1 && groupSize > 0 && groupSize < terrainGrid[(originRow, originColumn)][1]) // Si le terrain n'est pas une forêt et que le groupe de forêt adjacent est entre 1 et 4
            {
                List<(int, int)> adjacentHexagons = GetAdjacentHexagons(originRow, originColumn);
                int counter = 0;
                foreach (var adjacent in adjacentHexagons)
                {
                    if (terrainGrid[(adjacent.Item1, adjacent.Item2)][0] == -1) 
                    {
                        counter++;
                    }
                }
                int randomInt = Random.Range(0, counter);
                if (counter > 0)
                {
                    if (randomInt < 1 / (float)counter)
                    {
                        terrainType = 1; // Changer en forêt
                        terrainGrid[(row, column)][1] = terrainGrid[(originRow, originColumn)][1];
                    }
                }
                else
                {
                    terrainType = 1; // Changer en forêt
                    terrainGrid[(row, column)][1] = terrainGrid[(originRow, originColumn)][1];
                }
            }
        }

        foreach (var waterGroup in adjacentWaterGroupSize)
        {
            (int originRow, int originColumn) = waterGroup.Key;
            (int groupSize, List<(int, int)> groupPositions) = adjacentWaterGroupSize[waterGroup.Key];

            if (terrainType != 3 && groupSize > 0 && groupSize < terrainGrid[(originRow, originColumn)][1]) // Si le terrain n'est pas de l'eau et que le groupe d'eau adjacent est inferieur a la taille attendu du groupe d'eau
            {
                bool cancelSwitch = false;
                List<(int, int)> adjHexagonCurrentHex = GetAdjacentHexagons(row, column);
                foreach (var currentAdj in adjHexagonCurrentHex)
                {
                    List<(int, int)> adjAdjHexagonCurrentHex = GetAdjacentHexagons(currentAdj.Item1, currentAdj.Item2);
                    foreach((int, int) currentAdjAdj in adjAdjHexagonCurrentHex)
                    {
                        if(terrainGrid[(currentAdjAdj)][0] == 3 && terrainGrid[(currentAdj)][0] != 3 && !IsAjdFrom(currentAdjAdj.Item1, currentAdjAdj.Item2, row, column))
                        {
                            cancelSwitch = true;
                            Dictionary<(int, int), (int size, List<(int, int)> positions)> adjacentWaterGroup = CountListAdjacentGroupSizes(currentAdjAdj.Item1, currentAdjAdj.Item2, 3); // Taille des groupe d'eau adjacente
                            foreach (var adjWaterGroup in adjacentWaterGroup)
                            {
                                (int size, List<(int, int)> positions) = adjacentWaterGroup[adjWaterGroup.Key];
                                foreach ((int, int) pos in groupPositions)
                                {
                                    if(IsAjdFrom(pos.Item1, pos.Item2, row, column))
                                    {
                                        cancelSwitch = false;
                                        break;
                                    }
                                }
                            }
                            if(cancelSwitch) break;
                        }
                        if(cancelSwitch) break;
                    }
                    if(cancelSwitch) break;
                }

                if(cancelSwitch)
                {
                    break;   
                }
                else
                {
                    List<(int, int)> adjacentHexagons = GetAdjacentHexagons(originRow, originColumn);
                    int counter = 0;
                    foreach (var adjacent in adjacentHexagons)
                    {
                        if (terrainGrid[(adjacent.Item1, adjacent.Item2)][0] == -1) 
                        {
                            counter++;
                        }
                    }
                    int randomInt = Random.Range(0, counter);
                    if (counter > 0)
                    {
                        if (randomInt < 1 / (float)counter)
                        {
                            terrainType = 3; // Changer en eau
                            terrainGrid[(row, column)][1] = terrainGrid[(originRow, originColumn)][1];
                        }
                    }
                    else
                    {
                        terrainType = 3; // Changer en eau
                        terrainGrid[(row, column)][1] = terrainGrid[(originRow, originColumn)][1];
                    } 
                }
            }
        }
        
        foreach (var forestGroup in adjacentForestGroupSize)
        {
            (int originRow, int originColumn) = forestGroup.Key;
            (int groupSize, List<(int, int)> groupPositions) = adjacentForestGroupSize[forestGroup.Key];
            if (terrainType == 3 && groupSize >= 1) // Si le terrain est de l'eau et qu'il y a une foret en ajdacent
            {
                terrainType = 0; // Changer en plaines
            }
        }

        if (row == 0 || row == gridRow -1 || column == 0 || column == gridColumn -1 )
        {
            terrainType = 0;
        }
        else if (row == 1 || row == gridRow -2 || column == 1 || column == gridColumn -2 )
        {
            if(terrainType == 3) terrainType = 0;
        }
        
        if (terrainType == 3) // Si c'est une eau 
        {
            bool shouldBePlain = false;
            List<(int, int)> adjHexagonCurrentHex = GetAdjacentHexagons(row, column);
            foreach (var currentAdj in adjHexagonCurrentHex)
            {
                List<(int, int)> adjAdjHexagonCurrentHex = GetAdjacentHexagons(currentAdj.Item1, currentAdj.Item2);
                foreach((int, int) currentAdjAdj in adjAdjHexagonCurrentHex)
                {
                    if(terrainGrid[(currentAdjAdj)][0] == 3 && terrainGrid[(currentAdj)][0] != 3 && !IsAjdFrom(currentAdjAdj.Item1, currentAdjAdj.Item2, row, column))
                    {
                        shouldBePlain = true;
                        Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)> adjacentWaterGroup = CountListAdjacentGroupSizes(currentAdjAdj.Item1, currentAdjAdj.Item2, 3); // Taille des groupe d'eau adjacente
                        foreach (var waterGroup in adjacentWaterGroup)
                        {
                            (int groupSize, List<(int, int)> groupPositions) = adjacentWaterGroup[waterGroup.Key];
                            foreach ((int, int) pos in groupPositions)
                            {
                                if(IsAjdFrom(pos.Item1, pos.Item2, row, column))
                                {
                                    shouldBePlain = false;
                                    break;
                                }
                            }
                        }
                        if(shouldBePlain) break;
                    }
                    if(shouldBePlain) break;
                }
                if(shouldBePlain) break;
            }
            if(shouldBePlain)
            {
                terrainType = 0;  
            }
        }

        foreach (var waterGroup in adjacentWaterGroupSize)
        {
            (int groupSize, List<(int, int)> groupPositions) = adjacentWaterGroupSize[waterGroup.Key];
            if ((terrainType == 0 || terrainType == 1) && groupSize >= 1) // Si le terrain est de la plaine ou de la foret et que le groupe d'eau adjacent est de 1 ou plus
            {
                terrainType = 2; // Changer en sable
            }
        }

        if (terrainType == 3) // Si c'est une eau 
        {
            List<(int, int)> adjacentHexagons = GetAdjacentHexagons(row, column);
            foreach (var adjacent in adjacentHexagons)
            {
                if (terrainGrid.ContainsKey(adjacent))
                {
                    if (terrainGrid[adjacent][0] == 0 || terrainGrid[adjacent][0] == 1) 
                    {
                        terrainGrid[adjacent][0] = 2; // Changer en sable
                    }
                }
            }
            List<(int, int)> adjacentHexagonsSymmetry = GetAdjacentHexagons(rowSymmetry, columnSymmetry);
            foreach (var adjacent in adjacentHexagonsSymmetry)
            {
                if (terrainGrid.ContainsKey(adjacent))
                {
                    if (terrainGrid[adjacent][0] == 0 || terrainGrid[adjacent][0] == 2) 
                    {
                        terrainGrid[adjacent][0] = 2; // Changer en sable
                    }
                }
            }
        }

        switch (terrainType)
        {
            case 0:
                break;
            case 1:
                if (terrainGrid[(row, column)][1] == -1)
                {
                    int[] possibleCounts = { 4, 5 };
                    int randomCountIndex = Random.Range(0, possibleCounts.Length);
                    int randomCount = possibleCounts[randomCountIndex];
                    terrainGrid[(row, column)][1] = randomCount;
                }
                break;
            case 2:
                break;
            case 3:
                if(terrainGrid[(row, column)][1] == -1)
                {
                    int[] possibleCounts = { 2, 3, 4, 5, 6, 7, 8 };
                    int randomCountIndex = Random.Range(0, possibleCounts.Length);
                    int randomCount = possibleCounts[randomCountIndex];
                    terrainGrid[(row, column)][1] = randomCount;
                }
                break;
        }
        
        // Ajouter ce terrain à la grille
        terrainGrid[(row, column)][0] = terrainType;
        terrainGrid[(rowSymmetry, columnSymmetry)][0] = terrainType;
        
        CountTerrain();
    }

    public void SetTerrainHeight()
    {
        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);
        int randomHeight = 1;
        bool firstStep = true;
        if (terrainGrid[(0, 0)][2] != -1) firstStep = false;
        for (int row = 0; row < gridRow / 2; row++)
        {
            for (int column = 0; column < gridColumn; column++)
            {
                if (firstStep || terrainGrid[(row, column)][0] == 2)
                {
                    Transform hex = transform.Find("Row" + row + "/Hexagon_" + row + "_" + column);
                    Transform hexSymmetry = transform.Find("Row" + (gridRow - 1 - row) + "/Hexagon_" + (gridRow - 1 - row) + "_" + (gridColumn - 1 - column));
                    if (terrainGrid[(row, column)][0] != 2 && terrainGrid[(row, column)][0] != 3)
                    {
                        float xCoord = (column * noiseScale) + offsetX;
                        float yCoord = (row * noiseScale) + offsetY;

                        float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
                        randomHeight = Mathf.RoundToInt(Mathf.Lerp(minHeight, maxHeight + 1, perlinValue));
                    }
                    else
                    {
                        List<(int, int)> adjacentHexagons = GetAdjacentHexagons(row, column);
                        foreach ((int, int) adj in adjacentHexagons)
                        {
                            if (terrainGrid[(adj.Item1, adj.Item2)][0] == 3 && terrainGrid[(adj.Item1, adj.Item2)][2] != -1)
                            {
                                randomHeight = terrainGrid[(adj.Item1, adj.Item2)][2];
                                break;
                            }
                            else
                            {
                                float xCoord = (column * noiseScale) + offsetX;
                                float yCoord = (row * noiseScale) + offsetY;

                                float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
                                randomHeight = Mathf.RoundToInt(Mathf.Lerp(minHeight, maxHeight + 1, perlinValue));
                            }
                        }
                    }
                    hex.transform.position = new Vector3(hex.transform.position.x, randomHeight * heightFactor, hex.transform.position.z);
                    hex.transform.localScale = new Vector3(hex.transform.localScale.x, (2.32f * randomHeight) * heightFactor, hex.transform.localScale.z);
                    
                    hexSymmetry.transform.position = new Vector3(hexSymmetry.transform.position.x, randomHeight * heightFactor, hexSymmetry.transform.position.z);
                    hexSymmetry.transform.localScale = new Vector3(hexSymmetry.transform.localScale.x, (2.32f * randomHeight) * heightFactor, hexSymmetry.transform.localScale.z);

                    terrainGrid[(row, column)][2] = randomHeight;
                    terrainGrid[((gridRow - 1 - row), (gridColumn - 1 - column))][2] = randomHeight;
                }
            }
        }
        if (firstStep) SetTerrainHeight();
    }

    public void RefineHeight()
    {
        for (int row = 0; row < gridRow / 2; row++)
        {
            for (int column = 0; column < gridColumn; column++)
            {
                int counter = 0;
                int total = 0;
                float average = 0;
                List<(int, int)> adjacentHexagons = GetAdjacentHexagons(row, column);
                foreach((int, int) hex in adjacentHexagons)
                {
                    if(terrainGrid[(row, column)][2] != terrainGrid[(hex.Item1, hex.Item2)][2])
                    {
                        counter++;
                        total += terrainGrid[(hex.Item1, hex.Item2)][2];
                    } 
                }
                if(counter == 6)
                {
                    average = (total / counter);
                    
                    Transform hex = transform.Find("Row" + row + "/Hexagon_" + row + "_" + column);
                    Transform hexSymmetry = transform.Find("Row" + (gridRow - 1 - row) + "/Hexagon_" + (gridRow - 1 - row) + "_" + (gridColumn - 1 - column));

                    hex.transform.position = new Vector3(hex.transform.position.x, (int)average * heightFactor, hex.transform.position.z);
                    hex.transform.localScale = new Vector3(hex.transform.localScale.x, (2.32f * (int)average) * heightFactor, hex.transform.localScale.z);

                    hexSymmetry.transform.position = new Vector3(hexSymmetry.transform.position.x, (int)average * heightFactor, hexSymmetry.transform.position.z);
                    hexSymmetry.transform.localScale = new Vector3(hexSymmetry.transform.localScale.x, (2.32f * (int)average) * heightFactor, hexSymmetry.transform.localScale.z);

                    terrainGrid[(row, column)][2] = (int)average;
                    terrainGrid[((gridRow - 1 - row), (gridColumn - 1 - column))][2] = (int)average;
                }
            }
        }
    }

    public void FillHolls()
    {
        int minForestAdj = 5;
        int minWaterAdj = 4;
        bool hasChanged = false;
        counterFillHoles ++;

        for (int row = 0; row < gridRow; row++)
        {
            for (int column = 0; column < gridColumn; column++)
            {
                if(terrainGrid[(row, column)][0] != -1)
                {
                    int terrainType = 0;
                    int counterForest = 0;
                    int counterWater = 0;
                    List<(int, int)> adjacentHexagons = GetAdjacentHexagons(row, column);
                    foreach((int, int) hex in adjacentHexagons)
                    {
                        if(terrainGrid[(hex.Item1, hex.Item2)][0] == 1) counterForest++;
                        if(terrainGrid[(hex.Item1, hex.Item2)][0] == 3) counterWater++;
                    }

                    if((counterForest >= minForestAdj && terrainGrid[(row, column)][0] != 1) || (counterWater >= minWaterAdj  && terrainGrid[(row, column)][0] != 3))
                    {
                        Transform hexTransform = transform.Find("Row" + row + "/Hexagon_" + row + "_" + column);
                        if(counterForest >= minForestAdj) 
                        {
                            terrainType = 1;
                            terrainGrid[(row, column)][0] = terrainType;
                            hasChanged = true;
                        }
                        if(counterWater >= minWaterAdj) 
                        {
                            terrainType = 3;
                            terrainGrid[(row, column)][0] = terrainType;
                            hasChanged = true;

                            foreach (var adjacent in adjacentHexagons)
                            {
                                if (terrainGrid[adjacent][0] == 0 || terrainGrid[adjacent][0] == 1) 
                                {
                                    terrainGrid[adjacent][0] = 2; // Changer en sable
                                }
                            }
                        } 
                    }
                }
            }
        }
        if(hasChanged && counterFillHoles < 100) FillHolls();
    }

    public void RemoveIsolatedForest()
    {
        for (int row = 0; row < gridRow; row++)
        {
            for (int column = 0; column < gridColumn; column++)
            {
                if(terrainGrid[(row, column)][0] == 1)
                {
                    bool isSize2 = false;
                    int terrainType = 0;

                    Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)> adjacentForestGroupSize = CountListAdjacentGroupSizes(row, column, 1); // Taille des groupes de forêt adjacente
                    foreach (var forestGroup in adjacentForestGroupSize)
                    {
                        if(forestGroup.Value.groupSize == 2)
                        {
                            isSize2 = true;
                        }
                    }

                    if(isSize2)
                    {
                        terrainGrid[(row, column)][0] = 0;
                    }
                }
            }
        }
        for (int row = 0; row < gridRow; row++)
        {
            for (int column = 0; column < gridColumn; column++)
            {
                if(terrainGrid[(row, column)][0] == 1)
                {
                    int terrainType = 0;
                    int counterForest = 0;
                    int counterWater = 0;
                    List<(int, int)> adjacentHexagons = GetAdjacentHexagons(row, column);
                    foreach((int, int) hex in adjacentHexagons)
                    {
                        if(terrainGrid[(hex.Item1, hex.Item2)][0] == 1) counterForest++;
                        if(terrainGrid[(hex.Item1, hex.Item2)][0] == 3) counterWater++;
                    }

                    if((counterForest == 0 && terrainGrid[(row, column)][0] == 1))
                    {
                        terrainGrid[(row, column)][0] = 0;
                    }
                }
            }
        }
    }

    public void CountTerrain()
    {
        plainCount = 0; forestCount = 0; sandCount = 0; waterCount = 0;
        for (int row = 0; row < gridRow; row++)
        {
            for (int column = 0; column < gridColumn; column++)
            {
                if(terrainGrid[(row, column)][0] == 0) plainCount += 1;
                if(terrainGrid[(row, column)][0] == 1) forestCount += 1;
                if(terrainGrid[(row, column)][0] == 2) sandCount += 1;
                if(terrainGrid[(row, column)][0] == 3) waterCount += 1;
            }
        }
    }

    public void SpawnHex()
    {
        Vector3 hexPos;
        GameObject newHex;
        for (int row = 0; row < gridRow; row++)
        {
            for (int column = 0; column < gridColumn; column++)
            {
                switch(terrainGrid[(row, column)][0])
                {
                    case 0:
                        hexPos = CalculateHexPosition(row, column);
                        hexPrefab = hexPrefabPlain;
                        newHex = Instantiate(hexPrefab, hexPos, Quaternion.identity);
                        newHex.name = "Hexagon_" + row + "_" + column;
                        newHex.transform.SetParent(transform.Find("Row" + row));
                        break;
                    case 1:
                        hexPos = CalculateHexPosition(row, column);
                        int randomPrefabIndex = Random.Range(0, hexPrefabForest.Count);
                        hexPrefab = hexPrefabForest[randomPrefabIndex];
                        newHex = Instantiate(hexPrefab, hexPos, Quaternion.identity);
                        int randomRotation = Random.Range(1, 7);
                        newHex.transform.Rotate(0, 60 * randomRotation, 0);
                        newHex.name = "Hexagon_" + row + "_" + column;
                        newHex.transform.SetParent(transform.Find("Row" + row));
                        break;
                    case 2:
                        hexPos = CalculateHexPosition(row, column);
                        hexPrefab = hexPrefabSand;
                        newHex = Instantiate(hexPrefab, hexPos, Quaternion.identity);
                        newHex.name = "Hexagon_" + row + "_" + column;
                        newHex.transform.SetParent(transform.Find("Row" + row));
                        break;
                    case 3:
                        hexPos = CalculateHexPosition(row, column);
                        hexPrefab = hexPrefabWater;
                        newHex = Instantiate(hexPrefab, hexPos, Quaternion.identity);
                        newHex.name = "Hexagon_" + row + "_" + column;
                        newHex.transform.SetParent(transform.Find("Row" + row));
                        break;
                }
            }
        }
    }

    public void SpawnAnimal()
    {
        bool plainAnimalDone = false;
        int randomColumn = 0;
        for (int row = 0; row < gridRow / 2; row++)
        {
            if (row % 3 == 0)
            {
                plainAnimalDone = false;
                randomColumn = Random.Range(0, gridColumn); // Choose a random column for each group of rows
            }

            for (int column = 0; column < gridColumn; column++)
            {
                bool alreadySpawn = false;
                switch(terrainGrid[(row, column)][0])
                {
                    case 0:
                        if(row % 3 == 0 && !plainAnimalDone) 
                        {
                            plainAnimalDone = true;
                            
                            int randomPrefabIndex = Random.Range(0, ListAnimalsPlainPrefab.Count);
                            animalPrefab = ListAnimalsPlainPrefab[randomPrefabIndex];

                            Transform hexTransform = transform.Find("Row" + row + "/Hexagon_" + row + "_" + randomColumn);
                            GameObject newAnimal = Instantiate(animalPrefab, hexTransform.position, Quaternion.identity);
                            newAnimal.transform.SetParent(hexTransform);

                            int randomPrefabIndexSymmetry = Random.Range(0, ListAnimalsPlainPrefab.Count);
                            animalPrefab = ListAnimalsPlainPrefab[randomPrefabIndexSymmetry];

                            Transform hexTransformSymmetry = transform.Find("Row" + (gridRow - 1 - row) + "/Hexagon_" + (gridRow - 1 - row) + "_" + (gridColumn - 1 - randomColumn));
                            GameObject newAnimalSymmetry = Instantiate(animalPrefab, hexTransformSymmetry.position, Quaternion.identity);
                            newAnimalSymmetry.transform.SetParent(hexTransformSymmetry);
                        }
                        break;
                    case 1:
                        Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)> adjacentForestGroupSize = CountListAdjacentGroupSizes(row, column, 1); 
                        if (adjacentForestGroupSize.Count > 0)
                        {
                            foreach (var forestGroup in adjacentForestGroupSize)
                            {
                                var value = forestGroup.Value;
                                foreach (var hex in value.groupPositions)
                                {
                                    if(terrainGrid[(hex.Item1, hex.Item2)][3] == -1)
                                    {
                                        terrainGrid[(hex.Item1, hex.Item2)][3] = 0;
                                    }
                                    else
                                    {
                                        alreadySpawn = true;
                                    }
                                }
                            }
                            if(!alreadySpawn)
                            {
                                int randomPrefabIndex = Random.Range(0, ListAnimalsForestPrefab.Count);
                                animalPrefab = ListAnimalsForestPrefab[randomPrefabIndex];

                                Transform hexTransform = transform.Find("Row" + row + "/Hexagon_" + row + "_" + column);
                                GameObject newAnimal = Instantiate(animalPrefab, hexTransform.position, Quaternion.identity);
                                newAnimal.transform.SetParent(hexTransform);

                                randomPrefabIndex = Random.Range(0, ListAnimalsForestPrefab.Count);
                                animalPrefab = ListAnimalsForestPrefab[randomPrefabIndex];

                                Transform hexTransformSymmetry = transform.Find("Row" + (gridRow - 1 - row) + "/Hexagon_" + (gridRow - 1 - row) + "_" + (gridColumn - 1 - column));
                                GameObject newAnimalSymmetry = Instantiate(animalPrefab, hexTransformSymmetry.position, Quaternion.identity);
                                newAnimalSymmetry.transform.SetParent(hexTransformSymmetry);
                            }
                        }
                        else
                        {
                            int randomPrefabIndex = Random.Range(0, ListAnimalsForestPrefab.Count);
                            animalPrefab = ListAnimalsForestPrefab[randomPrefabIndex];

                            Transform hexTransform = transform.Find("Row" + row + "/Hexagon_" + row + "_" + column);
                            GameObject newAnimal = Instantiate(animalPrefab, hexTransform.position, Quaternion.identity);
                            newAnimal.transform.SetParent(hexTransform);

                            randomPrefabIndex = Random.Range(0, ListAnimalsForestPrefab.Count);
                            animalPrefab = ListAnimalsForestPrefab[randomPrefabIndex];

                            Transform hexTransformSymmetry = transform.Find("Row" + (gridRow - 1 - row) + "/Hexagon_" + (gridRow - 1 - row) + "_" + (gridColumn - 1 - column));
                            GameObject newAnimalSymmetry = Instantiate(animalPrefab, hexTransformSymmetry.position, Quaternion.identity);
                            newAnimalSymmetry.transform.SetParent(hexTransformSymmetry);
                        }
                        break;
                    case 2:
                        Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)> adjacentSandGroupSize = CountListAdjacentGroupSizes(row, column, 2); // Taille des groupe d'eau adjacente
                        if (adjacentSandGroupSize.Count > 0)
                        {
                            foreach (var sandGroup in adjacentSandGroupSize)
                            {
                                var value = sandGroup.Value;
                                foreach (var hex in value.groupPositions)
                                {
                                    if(terrainGrid[(hex.Item1, hex.Item2)][3] == -1)
                                    {
                                        terrainGrid[(hex.Item1, hex.Item2)][3] = 0;
                                    }
                                    else
                                    {
                                        alreadySpawn = true;
                                    }
                                }
                            }
                            if(!alreadySpawn)
                            {
                                int randomPrefabIndex = Random.Range(0, ListAnimalsSandPrefab.Count);
                                animalPrefab = ListAnimalsSandPrefab[randomPrefabIndex];

                                Transform hexTransform = transform.Find("Row" + row + "/Hexagon_" + row + "_" + column);
                                GameObject newAnimal = Instantiate(animalPrefab, hexTransform.position, Quaternion.identity);
                                newAnimal.transform.SetParent(hexTransform);
                                Vector3 newPosition = new Vector3(newAnimal.transform.position.x, hexTransform.position.y + 0.5f, newAnimal.transform.position.z);
                                newAnimal.transform.position = newPosition;

                                randomPrefabIndex = Random.Range(0, ListAnimalsSandPrefab.Count);
                                animalPrefab = ListAnimalsSandPrefab[randomPrefabIndex];

                                Transform hexTransformSymmetry = transform.Find("Row" + (gridRow - 1 - row) + "/Hexagon_" + (gridRow - 1 - row) + "_" + (gridColumn - 1 - column));
                                GameObject newAnimalSymmetry = Instantiate(animalPrefab, hexTransformSymmetry.position, Quaternion.identity);
                                newAnimalSymmetry.transform.SetParent(hexTransformSymmetry);
                                Vector3 newPositionSymmetry = new Vector3(newAnimalSymmetry.transform.position.x, hexTransformSymmetry.position.y + 0.5f, newAnimalSymmetry.transform.position.z);
                                newAnimalSymmetry.transform.position = newPositionSymmetry;
                            }
                        }
                        else
                        {
                            int randomPrefabIndex = Random.Range(0, ListAnimalsSandPrefab.Count);
                            animalPrefab = ListAnimalsSandPrefab[randomPrefabIndex];

                            Transform hexTransform = transform.Find("Row" + row + "/Hexagon_" + row + "_" + column);
                            GameObject newAnimal = Instantiate(animalPrefab, hexTransform.position, Quaternion.identity);
                            newAnimal.transform.SetParent(hexTransform);
                            Vector3 newPosition = new Vector3(newAnimal.transform.position.x, hexTransform.position.y + 0.5f, newAnimal.transform.position.z);
                            newAnimal.transform.position = newPosition;

                            randomPrefabIndex = Random.Range(0, ListAnimalsSandPrefab.Count);
                            animalPrefab = ListAnimalsSandPrefab[randomPrefabIndex];

                            Transform hexTransformSymmetry = transform.Find("Row" + (gridRow - 1 - row) + "/Hexagon_" + (gridRow - 1 - row) + "_" + (gridColumn - 1 - column));
                            GameObject newAnimalSymmetry = Instantiate(animalPrefab, hexTransformSymmetry.position, Quaternion.identity);
                            newAnimalSymmetry.transform.SetParent(hexTransformSymmetry);
                            Vector3 newPositionSymmetry = new Vector3(newAnimalSymmetry.transform.position.x, hexTransformSymmetry.position.y + 0.5f, newAnimalSymmetry.transform.position.z);
                            newAnimalSymmetry.transform.position = newPositionSymmetry;
                        }
                    break;
                    case 3:
                        Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)> adjacentWaterGroupSize = CountListAdjacentGroupSizes(row, column, 3); // Taille des groupe d'eau adjacente
                        if (adjacentWaterGroupSize.Count > 0)
                        {
                            foreach (var waterGroup in adjacentWaterGroupSize)
                            {
                                var value = waterGroup.Value;
                                foreach (var hex in value.groupPositions)
                                {
                                    if(terrainGrid[(hex.Item1, hex.Item2)][3] == -1)
                                    {
                                        terrainGrid[(hex.Item1, hex.Item2)][3] = 0;
                                    }
                                    else
                                    {
                                        alreadySpawn = true;
                                    }
                                }
                            }
                            if(!alreadySpawn)
                            {
                                int randomPrefabIndex = Random.Range(0, ListAnimalsWaterPrefab.Count);
                                animalPrefab = ListAnimalsWaterPrefab[randomPrefabIndex];

                                Transform hexTransform = transform.Find("Row" + row + "/Hexagon_" + row + "_" + column);
                                GameObject newAnimal = Instantiate(animalPrefab, hexTransform.position, Quaternion.identity);
                                newAnimal.transform.SetParent(hexTransform);
                                Vector3 newPosition = new Vector3(newAnimal.transform.position.x, hexTransform.position.y - 0.5f, newAnimal.transform.position.z);
                                newAnimal.transform.position = newPosition;

                                randomPrefabIndex = Random.Range(0, ListAnimalsWaterPrefab.Count);
                                animalPrefab = ListAnimalsWaterPrefab[randomPrefabIndex];

                                Transform hexTransformSymmetry = transform.Find("Row" + (gridRow - 1 - row) + "/Hexagon_" + (gridRow - 1 - row) + "_" + (gridColumn - 1 - column));
                                GameObject newAnimalSymmetry = Instantiate(animalPrefab, hexTransformSymmetry.position, Quaternion.identity);
                                newAnimalSymmetry.transform.SetParent(hexTransformSymmetry);
                                Vector3 newPositionSymmetry = new Vector3(newAnimalSymmetry.transform.position.x, hexTransformSymmetry.position.y - 0.5f, newAnimalSymmetry.transform.position.z);
                                newAnimalSymmetry.transform.position = newPositionSymmetry;
                            }
                        }
                        else
                        {
                            int randomPrefabIndex = Random.Range(0, ListAnimalsWaterPrefab.Count);
                            animalPrefab = ListAnimalsWaterPrefab[randomPrefabIndex];

                            Transform hexTransform = transform.Find("Row" + row + "/Hexagon_" + row + "_" + column);
                            GameObject newAnimal = Instantiate(animalPrefab, hexTransform.position, Quaternion.identity);
                            newAnimal.transform.SetParent(hexTransform);
                            Vector3 newPosition = new Vector3(newAnimal.transform.position.x, hexTransform.position.y - 0.5f, newAnimal.transform.position.z);
                            newAnimal.transform.position = newPosition;

                            randomPrefabIndex = Random.Range(0, ListAnimalsWaterPrefab.Count);
                            animalPrefab = ListAnimalsWaterPrefab[randomPrefabIndex];

                            Transform hexTransformSymmetry = transform.Find("Row" + (gridRow - 1 - row) + "/Hexagon_" + (gridRow - 1 - row) + "_" + (gridColumn - 1 - column));
                            GameObject newAnimalSymmetry = Instantiate(animalPrefab, hexTransformSymmetry.position, Quaternion.identity);
                            newAnimalSymmetry.transform.SetParent(hexTransformSymmetry);
                            Vector3 newPositionSymmetry = new Vector3(newAnimalSymmetry.transform.position.x, hexTransformSymmetry.position.y - 0.5f, newAnimalSymmetry.transform.position.z);
                            newAnimalSymmetry.transform.position = newPositionSymmetry;
                        }
                        break;
                }
            }
        }
    }

    private Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)> CountListAdjacentGroupSizes(int row, int column, int terrainType)
    {
        HashSet<(int, int)> visited = new HashSet<(int, int)>(); // Pour garder une trace des hexagones visités
        Queue<(int, int)> queue = new Queue<(int, int)>(); // File pour le parcours en largeur
        Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)> groupSizes = new Dictionary<(int, int), (int groupSize, List<(int, int)> groupPositions)>(); // Dictionnaire pour stocker la position d'origine, la taille du groupe, et les positions du groupe

        // Parcourez les hexagones adjacents à la cellule de départ
        foreach (var neighbor in GetAdjacentHexagons(row, column))
        {
            if (!visited.Contains(neighbor) && terrainGrid.ContainsKey(neighbor) && terrainGrid[neighbor][0] == terrainType)
            {
                queue.Enqueue(neighbor); // Ajoutez l'hexagone adjacent non visité à la file d'attente
                visited.Add(neighbor); // Marquez l'hexagone adjacent comme visité
                int groupSize = 0; // Taille du groupe actuel
                (int, int) groupOrigin = neighbor; // Position d'origine du groupe
                List<(int, int)> groupPositions = new List<(int, int)>(); // Liste des positions du groupe

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    groupSize++;
                    groupPositions.Add(current); // Ajoutez la position actuelle à la liste des positions du groupe

                    // Parcourez les hexagones adjacents
                    foreach (var adj in GetAdjacentHexagons(current.Item1, current.Item2))
                    {
                        if (!visited.Contains(adj) && terrainGrid.ContainsKey(adj) && terrainGrid[adj][0] == terrainType)
                        {
                            queue.Enqueue(adj); // Ajoutez l'hexagone adjacent non visité à la file d'attente
                            visited.Add(adj); // Marquez l'hexagone adjacent comme visité
                        }
                    }
                }
                groupSizes[groupOrigin] = (groupSize, groupPositions); // Ajoutez la position d'origine, la taille du groupe et les positions du groupe au dictionnaire
            }
        }
        return groupSizes;
    }

    private List<(int, int)> GetAdjacentHexagons(int row, int column)
    {
        List<(int, int)> adjacentHexagons = new List<(int, int)>();
        (int, int)[] directions = new (int, int)[5];
        // Directions pour les hexagones adjacents dans une grille axiale
        if(row % 2 == 0)
        {
            directions = new (int, int)[]
            {
                (+1, +1), (1, 0) , (0, -1), (0, + 1), (-1, + 1), (-1, 0)
            };
        }
        else
        {
            directions = new (int, int)[]
            {
                (1, 0), (1, -1), (0, -1), (0, 1), (-1, -1), (-1, 0)
            };
        }
        
        foreach (var (dRow, dColumn) in directions)
        {
            int newRow = row + dRow;
            int newColumn = column + dColumn;

            if (IsHexagonValid(newRow, newColumn))
            {
                adjacentHexagons.Add((newRow, newColumn));
            }
        }

        return adjacentHexagons;
    }

    private bool IsAjdFrom(int row, int column, int rowTarget, int columnTarget)
    {
        (int, int)[] directions;
        // Directions pour les hexagones adjacents dans une grille axiale
        if (row % 2 == 0)
        {
            directions = new (int, int)[]
            {
                (+1, +1), (1, 0), (0, -1), (0, +1), (-1, +1), (-1, 0)
            };
        }
        else
        {
            directions = new (int, int)[]
            {
                (1, 0), (1, -1), (0, -1), (0, +1), (-1, -1), (-1, 0)
            };
        }

        foreach (var (dRow, dColumn) in directions)
        {
            int newRow = row + dRow;
            int newColumn = column + dColumn;

            if (newRow == rowTarget && newColumn == columnTarget)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsHexagonValid(int row, int column)
    {
        // Vérifiez si les coordonnées sont dans les limites de la grille
        return row >= 0 && row < gridRow && column >= 0 && column < gridColumn;
    }

}
