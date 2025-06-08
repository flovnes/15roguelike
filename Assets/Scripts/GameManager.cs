using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    #region Unity Methods
    public static GameManager Instance { get; private set; }
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GenerateLevel();
        if (messageText) messageText.text = "";
        currentScore = 0;
        UpdateScoreDisplay();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        if (isGameOver) return;

        Vector3 currentMouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currentMouseWorldPos.z = 0;

        HandleKeyOverrides();

        if (tileBeingDragged == null && !Input.GetMouseButton(0) && !swapAnimationActive)
        {
            HandleTileHover(currentMouseWorldPos);
        }
        else if (currentlyHoveredTileForVisuals != null && tileBeingDragged != null)
        {
            ClearHoverVisuals(currentlyHoveredTileForVisuals);
            currentlyHoveredTileForVisuals = null;
            hoveredEnemy = null;
            UpdateAllHighlightDisplays();
        }


        HandleKeyboardInput();
        HandleMouseInput(currentMouseWorldPos);

        ProcessMoveQueue();
        lastMouseWorldPos = currentMouseWorldPos;
    }

    #endregion

    #region Unity Fields
    public string gameSceneName = "Game";
    public string mainMenuSceneName = "Menu";
    public float tileSize = 1.0f;
    public int minGridSize = 4;
    public int maxGridSize = 7;
    public int floorsPerSizeIncrease = 3;
    public int numberOfEnemies = 1;
    public int numberOfTraps = 2;
    public int numberOfHealthPickups = 3;
    public GameObject lizardTilePrefab;
    public GameObject rhinoTilePrefab;
    public GameObject deerTilePrefab;
    public GameObject trollTilePrefab;
    public GameObject trapTilePrefab;
    public GameObject rockTilePrefab;
    public GameObject boulderTilePrefab;
    public GameObject healthPickupPrefab;
    public GameObject playerPrefab;
    public GameObject playerTilePrefab;
    public GameObject emptyTilePrefab;
    public GameObject goalTilePrefab;
    public GameObject keyTilePrefab;
    public GameObject playerAttackOutlinePrefab;
    public Text scenePlayerHealthText;
    public Text messageText;
    public GameObject fullScreenFlashPanel;
    public Color deathFlashColor = new Color(1f, 0f, 0f, 0.7f);
    public Color winFlashColor = new Color(1f, 0.9f, 0.3f, 0.7f);
    public float flashDuration = 0.5f;
    public Color attackHighlightColor = new Color(1f, 0.5f, 0.5f, 0.75f);
    public KeyCode showAllAttackAreasKey = KeyCode.LeftAlt;
    public Color enemyHitFlashColor = Color.white;
    public float enemyHitFlashDuration = 0.15f;
    public int currentFloor = 1;
    public int finalFloor = 10;
    public Text floorDisplayUIText;
    private bool hasKey = false;
    private Vector2Int currentLevelGoalPosition;
    public int scoreKillEnemy = 100;
    public int scoreAdvanceLevel = 250;
    public int scoreCollectKey = 250;
    public int scorePenaltyPerTurn = 50;    
    private int currentScore = 0;
    public Text scoreDisplayUIText;
    public Vector2Int playerFacing = Vector2Int.up;
    public float hoverScaleMultiplier = 1.1f;
    public float hoverScaleDuration = 0.1f;
    public float maxDragTiltAngle = 10f;
    public float dragTiltSpeedFactor = 0.5f;
    public float dragTiltSmoothTime = 0.1f;
    public float swapAnimationDuration = 0.25f;
    public float enemySwapAnimationDuration = 0.1f;
    private bool swapAnimationActive = false;
    public int maxQueuedMoves = 2;
    private Queue<Vector2Int> moveQueue = new Queue<Vector2Int>();

    #endregion

    #region State Variables
    private Tile[,] grid;
    private GameObject[,] tileGameObjects;
    private int currentDynamicGridSize;
    private Player playerController;
    private GameObject playerGameObject;
    private Vector2Int playerGridPos;
    private List<GameObject> currentPlayerAttackOutlineGOs = new List<GameObject>();
    private List<Tile> currentlyHighlightedEnemyAttackTiles = new List<Tile>();
    private Tile currentlyHoveredTileForVisuals = null;
    private Vector3 originalScaleHoveredTile;
    private Tweener currentHoverScaleTween = null;
    private EnemyTile hoveredEnemy = null;
    private bool showAllHighlightsOverride = false;
    private Tile tileBeingDragged = null;
    private Vector2Int dragTileOriginalGridPos;
    private GameObject visualTileBeingDragged = null;
    private Vector3 mouseOffsetFromTileCenter;
    private int draggedTileOriginalSortingOrder;
    private SpriteRenderer draggedTileSpriteRenderer;
    private bool isGameOver = false;
    private int activeEnemiesCount = 0;
    private Vector3 lastMouseWorldPos;
    private Tweener currentTiltTween = null;

    public bool IsAnimating() => swapAnimationActive;
    public bool HasKey() => hasKey;

    #endregion

    #region Input 

    void HandleTileHover(Vector3 mouseWorldPos)
    {
        Vector2Int mouseGridPos = WorldToGridPosition(mouseWorldPos);
        Tile tileUnderMouse = null;

        if (InBounds(mouseGridPos))
        {
            tileUnderMouse = grid[mouseGridPos.x, mouseGridPos.y];
        }

        EnemyTile enemyForAttackAreaHighlight = null;
        if (tileUnderMouse is EnemyTile enemy && !enemy.IsDefeated())
        {
            enemyForAttackAreaHighlight = enemy;
        }

        if (hoveredEnemy != enemyForAttackAreaHighlight)
        {
            hoveredEnemy = enemyForAttackAreaHighlight;
            UpdateAllHighlightDisplays();
        }

        if (tileUnderMouse != null)
        {
            if (currentlyHoveredTileForVisuals != tileUnderMouse)
            {
                if (currentlyHoveredTileForVisuals != null)
                {
                    ClearHoverVisuals(currentlyHoveredTileForVisuals);
                }

                currentlyHoveredTileForVisuals = tileUnderMouse;
                GameObject tileGO = tileGameObjects[tileUnderMouse.gridPosition.x, tileUnderMouse.gridPosition.y];
                if (tileGO != null)
                {
                    originalScaleHoveredTile = tileGO.transform.localScale;
                    DOTween.Kill(tileGO.transform, true);
                    currentHoverScaleTween = tileGO.transform.DOScale(originalScaleHoveredTile * hoverScaleMultiplier, hoverScaleDuration).SetEase(Ease.OutQuad);
                }
            }
        }
        else
        {
            if (currentlyHoveredTileForVisuals != null)
            {
                ClearHoverVisuals(currentlyHoveredTileForVisuals);
                currentlyHoveredTileForVisuals = null;
            }
        }
    }

    void HandleKeyOverrides()
    {
        bool needsHighlightUpdate = false;

        if (Input.GetKeyDown(showAllAttackAreasKey))
        {
            if (!showAllHighlightsOverride)
            {
                showAllHighlightsOverride = true;
                needsHighlightUpdate = true;
            }
        }
        else if (Input.GetKeyUp(showAllAttackAreasKey))
        {
            if (showAllHighlightsOverride)
            {
                showAllHighlightsOverride = false;
                needsHighlightUpdate = true;
            }
        }
        else if (Input.GetKey(showAllAttackAreasKey) && !showAllHighlightsOverride)
        {
            showAllHighlightsOverride = true;
            needsHighlightUpdate = true;
        }
        else if (!Input.GetKey(showAllAttackAreasKey) && showAllHighlightsOverride)
        {
            showAllHighlightsOverride = false;
            needsHighlightUpdate = true;
        }

        if (needsHighlightUpdate)
            UpdateAllHighlightDisplays();
    }

    void HandleKeyboardInput()
    {
        Vector2Int worldDirectionPlayerWantsToMove = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) worldDirectionPlayerWantsToMove = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) worldDirectionPlayerWantsToMove = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) worldDirectionPlayerWantsToMove = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) worldDirectionPlayerWantsToMove = Vector2Int.right;

        if (worldDirectionPlayerWantsToMove != Vector2Int.zero)
            EnqueueMove(worldDirectionPlayerWantsToMove);
    }

    void HandleMouseInput(Vector3 mouseWorldPos)
    {
        if (swapAnimationActive && moveQueue.Count >= maxQueuedMoves && tileBeingDragged == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (tileBeingDragged == null && (!swapAnimationActive || moveQueue.Count < maxQueuedMoves))
            {
                Vector2Int clickedGridPos = WorldToGridPosition(mouseWorldPos);
                if (InBounds(clickedGridPos))
                {
                    if (IsAdjacent(clickedGridPos, playerGridPos) && grid[clickedGridPos.x, clickedGridPos.y].type != TileType.Player)
                    {
                        tileBeingDragged = grid[clickedGridPos.x, clickedGridPos.y];
                        dragTileOriginalGridPos = clickedGridPos;
                        visualTileBeingDragged = tileGameObjects[clickedGridPos.x, clickedGridPos.y];

                        if (visualTileBeingDragged != null)
                        {
                            if (currentlyHoveredTileForVisuals != null)
                            {
                                ClearHoverVisuals(currentlyHoveredTileForVisuals);
                                currentlyHoveredTileForVisuals = null;
                            }
                            mouseOffsetFromTileCenter = visualTileBeingDragged.transform.position - mouseWorldPos;
                            draggedTileSpriteRenderer = visualTileBeingDragged.GetComponent<SpriteRenderer>();
                            if (draggedTileSpriteRenderer != null)
                            {
                                draggedTileOriginalSortingOrder = draggedTileSpriteRenderer.sortingOrder;
                                draggedTileSpriteRenderer.sortingOrder = 9999;
                            }
                            if (hoveredEnemy != null)
                            {
                                hoveredEnemy = null;
                                UpdateAllHighlightDisplays();
                            }
                            if (currentTiltTween != null && currentTiltTween.IsActive()) currentTiltTween.Kill();
                        }
                        else { tileBeingDragged = null; }
                    }
                }
            }
        }

        if (Input.GetMouseButton(0) && tileBeingDragged != null && visualTileBeingDragged != null)
        {
            visualTileBeingDragged.transform.position = mouseWorldPos + mouseOffsetFromTileCenter;

            Vector3 mouseVelocity = (mouseWorldPos - lastMouseWorldPos) / Time.deltaTime;

            float targetZRotation = Mathf.Clamp(-mouseVelocity.x * dragTiltSpeedFactor, -maxDragTiltAngle, maxDragTiltAngle);
            float targetXRotation = Mathf.Clamp(mouseVelocity.y * dragTiltSpeedFactor, -maxDragTiltAngle, maxDragTiltAngle);

            if (currentTiltTween != null && currentTiltTween.IsActive()) currentTiltTween.Kill(false);

            currentTiltTween = visualTileBeingDragged.transform.DORotate(
                new Vector3(targetXRotation, 0, targetZRotation),
                dragTiltSmoothTime
            ).SetEase(Ease.OutQuad);
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (tileBeingDragged != null && visualTileBeingDragged != null)
            {
                if (currentTiltTween != null && currentTiltTween.IsActive()) currentTiltTween.Kill(false);
                currentTiltTween = visualTileBeingDragged.transform.DORotate(Vector3.zero, swapAnimationDuration * 0.5f).SetEase(Ease.OutBack);

                bool canSwapFromDrop = IsDropValid(mouseWorldPos, dragTileOriginalGridPos, playerGridPos);
                if (canSwapFromDrop)
                {
                    Vector2Int worldDirectionPlayerMoves = dragTileOriginalGridPos - playerGridPos;
                    if (moveQueue.Count < maxQueuedMoves)
                    {
                        EnqueueMove(worldDirectionPlayerMoves);
                    }
                    else
                    {
                        StartCoroutine(AnimateSnapBackCoroutine(visualTileBeingDragged, GridToWorldPosition(dragTileOriginalGridPos), draggedTileOriginalSortingOrder));
                    }
                }
                else
                {
                    StartCoroutine(AnimateSnapBackCoroutine(visualTileBeingDragged, GridToWorldPosition(dragTileOriginalGridPos), draggedTileOriginalSortingOrder));
                }

                tileBeingDragged = null;
                visualTileBeingDragged = null;
                draggedTileSpriteRenderer = null;
            }
        }
    }

    void EnqueueMove(Vector2Int direction)
    {
        if (moveQueue.Count < maxQueuedMoves)
            moveQueue.Enqueue(direction);
    }

    void ProcessMoveQueue()
    {
        if (!swapAnimationActive && moveQueue.Count > 0)
        {
            Vector2Int nextMoveDirection = moveQueue.Dequeue();
            AttemptSwap(nextMoveDirection);
        }
    }

    #endregion

    #region Tile Management

    void GenerateLevel()
    {
        currentDynamicGridSize = GetGridSizeForFloor(currentFloor);
        if (Camera.main != null) Camera.main.orthographicSize = GetCameraOrthoSizeForGrid(currentDynamicGridSize);

        DestroyOldTilesAndClearArrays();
        grid = new Tile[currentDynamicGridSize, currentDynamicGridSize];
        tileGameObjects = new GameObject[currentDynamicGridSize, currentDynamicGridSize];
        activeEnemiesCount = 0;
        hasKey = false;
        hoveredEnemy = null;
        tileBeingDragged = null;
        visualTileBeingDragged = null;

        for (int x = 0; x < currentDynamicGridSize; x++)
        {
            for (int y = 0; y < currentDynamicGridSize; y++)
            {
                SpawnTile(TileType.Empty, new Vector2Int(x, y), emptyTilePrefab);
            }
        }

        playerGridPos = new Vector2Int(currentDynamicGridSize / 2, currentDynamicGridSize / 2);
        ReplaceTileInGrid(playerGridPos, TileType.Player, playerTilePrefab);

        List<Vector2Int> occupiedTiles = new() { playerGridPos };

        int goalQuadrant = Random.Range(0, 4);
        int keyQuadrant;
        do
        {
            keyQuadrant = Random.Range(0, 4);
        } while (keyQuadrant == goalQuadrant || IsAdjacentQuadrant(goalQuadrant, keyQuadrant, currentDynamicGridSize));

        currentLevelGoalPosition = GetRandomPositionInQuadrant(goalQuadrant, occupiedTiles);
        ReplaceTileInGrid(currentLevelGoalPosition, TileType.Goal, goalTilePrefab);
        occupiedTiles.Add(currentLevelGoalPosition);
        if (grid[currentLevelGoalPosition.x, currentLevelGoalPosition.y] is GoalTile gt) gt.UpdateVisualState();

        if (currentFloor < finalFloor)
        {
            Vector2Int keyPosition = GetRandomPositionInQuadrant(keyQuadrant, occupiedTiles);
            ReplaceTileInGrid(keyPosition, TileType.Key, keyTilePrefab);
            occupiedTiles.Add(keyPosition);
        }

        int enemiesToPlace = (currentFloor == finalFloor) ? 1 : GetScaledValue(numberOfEnemies, currentFloor, 1, 1, 0);
        PlaceDynamicTiles(TileType.Enemy, enemiesToPlace, occupiedTiles, 1);

        int trapsToPlace = GetScaledValue(numberOfTraps, currentFloor, 3, 1, 0);
        PlaceDynamicTiles(TileType.Trap, trapsToPlace, occupiedTiles);

        if (currentFloor % 3 == 2)
            PlaceDynamicTiles(TileType.HealthPickup, 1, occupiedTiles);

        float environmentFillDensity = Mathf.Min(0.15f + (currentFloor * 0.02f), 1f);

        for (int x = 0; x < currentDynamicGridSize; x++)
        {
            for (int y = 0; y < currentDynamicGridSize; y++)
            {
                Vector2Int currentPos = new(x, y);
                if (Random.value < environmentFillDensity)
                {
                    if (!occupiedTiles.Contains(currentPos))
                    {
                        GameObject envPrefab = Random.value < 0.6f ? rockTilePrefab : boulderTilePrefab;
                        ReplaceTileInGrid(currentPos, TileType.Environment, envPrefab);
                    }
                    // else Empty
                }
            }
        }

        SpawnPlayerVisual();
        UpdateFloorDisplay();
        UpdateAllHighlightDisplays();
    }

    int GetScaledValue(int baseValue, int floor, int interval, int amountPerInterval, int minValue)
    {
        int calculatedValue = baseValue + (floor / interval * amountPerInterval);
        return Mathf.Max(minValue, calculatedValue);
    }

    bool IsAdjacentQuadrant(int q1, int q2, int gridSize)
    {
        if (gridSize < 5) return false;
        if (q1 == q2) return true;

        if (q1 == 0 && (q2 == 1 || q2 == 3)) return true; // TR to TL BR
        if (q1 == 1 && (q2 == 0 || q2 == 2)) return true; // TL to TR BL
        if (q1 == 2 && (q2 == 1 || q2 == 3)) return true; // BL to TL BR
        if (q1 == 3 && (q2 == 0 || q2 == 2)) return true; // BR to TR BL
        return false;
    }

    Vector2Int GetRandomPositionInQuadrant(int quadrantIndex, List<Vector2Int> occupiedSpots)
    {
        int halfSize = currentDynamicGridSize / 2;
        int attempts = 0;
        Vector2Int pos;

        int minX, maxX, minY, maxY;

        switch (quadrantIndex)
        {
            case 0: // TR
                minX = halfSize; maxX = currentDynamicGridSize - 1;
                minY = halfSize; maxY = currentDynamicGridSize - 1;
                break;
            case 1: // TL
                minX = 0; maxX = halfSize - 1;
                minY = halfSize; maxY = currentDynamicGridSize - 1;
                break;
            case 2: // BL
                minX = 0; maxX = halfSize - 1;
                minY = 0; maxY = halfSize - 1;
                break;
            case 3: // BR
            default:
                minX = halfSize; maxX = currentDynamicGridSize - 1;
                minY = 0; maxY = halfSize - 1;
                break;
        }

        maxX = Mathf.Max(minX, maxX);
        maxY = Mathf.Max(minY, maxY);

        do
        {
            pos = new Vector2Int(Random.Range(minX, maxX + 1), Random.Range(minY, maxY + 1));
            attempts++;
        } while (occupiedSpots.Contains(pos) && attempts < 50);

        if (occupiedSpots.Contains(pos))
        {
            do
            {
                pos = new Vector2Int(Random.Range(0, currentDynamicGridSize), Random.Range(0, currentDynamicGridSize));
            } while (occupiedSpots.Contains(pos));
        }
        return pos;
    }

    void PlaceDynamicTiles(
        TileType typeToPlace, int count,
        List<Vector2Int> occupiedSpots,
        int minDistanceFromPlayer = 0)
    {
        int placed = 0;

        List<Vector2Int> freeTiles = new List<Vector2Int>();
        for (int x = 0; x < currentDynamicGridSize; x++)
        {
            for (int y = 0; y < currentDynamicGridSize; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                if (!occupiedSpots.Contains(currentPos))
                {
                    freeTiles.Add(currentPos);
                }
            }
        }

        Debug.Log($"{freeTiles.Count} free spots left");
        for (int i = 0; i < freeTiles.Count - 1; i++)
        {

            int randomIndex = Random.Range(i, freeTiles.Count);
            (freeTiles[randomIndex], freeTiles[i]) = (freeTiles[i], freeTiles[randomIndex]);
        }

        Debug.Log($"Placing {count} of {typeToPlace}");
        foreach (var tile in freeTiles)
        {
            if (placed >= count) break;

            int distToPlayer = Mathf.Abs(tile.x - playerGridPos.x) + Mathf.Abs(tile.y - playerGridPos.y);
            if (distToPlayer >= minDistanceFromPlayer)
            {
                GameObject prefabToUse = null;
                prefabToUse = (typeToPlace == TileType.Enemy)
                    ? GetEnemyPrefabForFloor(currentFloor)
                    : GetPrefabForType(typeToPlace);

                Debug.Log($"Placing {typeToPlace} rn fr ong at {tile}");
                ReplaceTileInGrid(tile, typeToPlace, prefabToUse);
                occupiedSpots.Add(tile);
                placed++;
            }
        }
    }

    Tile SpawnTile(TileType type, Vector2Int gridPos, GameObject specificPrefab = null)
    {
        GameObject prefabToUse = specificPrefab != null ? specificPrefab : GetPrefabForType(type);
        if (prefabToUse == null)
            return null;

        Vector3 worldPos = GridToWorldPosition(gridPos);
        GameObject tileGO = Instantiate(prefabToUse, worldPos, Quaternion.identity, this.transform);

        Tile tileScript = tileGO.GetComponent<Tile>();

        if (tileScript == null)
        {
            Destroy(tileGO);
            return null;
        }

        tileScript.Initialize(gridPos, type);
        grid[gridPos.x, gridPos.y] = tileScript;
        tileGameObjects[gridPos.x, gridPos.y] = tileGO;
        return tileScript;
    }

    void SpawnPlayerVisual()
    {
        if (playerGameObject != null)
        {
            Destroy(playerGameObject);
        }

        Vector3 playerVisualWorldPos = GridToWorldPosition(playerGridPos);
        playerVisualWorldPos.z -= 0.1f;

        playerGameObject = Instantiate(playerPrefab, playerVisualWorldPos, Quaternion.identity);
        Player newPlayerControllerInstance = playerGameObject.GetComponent<Player>();

        if (newPlayerControllerInstance != null)
        {
            if (playerController != null)
            {
                newPlayerControllerInstance.currentHealth = playerController.currentHealth;
                newPlayerControllerInstance.maxHealth = playerController.maxHealth;
                newPlayerControllerInstance.attackDamage = playerController.attackDamage;
                newPlayerControllerInstance.currentAttackMode = playerController.currentAttackMode;
            }
            else
            {
                newPlayerControllerInstance.currentHealth = newPlayerControllerInstance.maxHealth;
            }

            playerController = newPlayerControllerInstance;
            playerController.healthText = scenePlayerHealthText;
            playerController.ForceInitialHealthUIDisplayUpdate();
        }
    }

    void ReplaceTileInGrid(Vector2Int pos, TileType newType, GameObject specificPrefab = null)
    {
        if (tileGameObjects[pos.x, pos.y] != null)
            Destroy(tileGameObjects[pos.x, pos.y]);
        SpawnTile(newType, pos, specificPrefab);
    }

    void UpdateFloorDisplay()
    {
        if (floorDisplayUIText != null)
        {
            floorDisplayUIText.text = $"{currentFloor}";
        }
    }

    public void ReplaceTileInGridDataAndVisuals(Vector2Int pos, TileType newType, GameObject specificPrefab = null)
    {
        if (!InBounds(pos)) return;
        ReplaceTileInGrid(pos, newType, specificPrefab);
    }

    int GetGridSizeForFloor(int floor)
    {
        int sizeIncreases = (floor - 1) / floorsPerSizeIncrease;
        int calculatedSize = minGridSize + sizeIncreases;
        return Mathf.Min(calculatedSize, maxGridSize);
    }

    float GetCameraOrthoSizeForGrid(int actualGridSize)
    {
        float desiredHeight = (actualGridSize * tileSize) + (tileSize * 1.0f);
        return desiredHeight / 2.0f;
    }

    void DestroyOldTilesAndClearArrays()
    {
        if (grid != null)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (tileGameObjects != null &&
                        x < tileGameObjects.GetLength(0) && y < tileGameObjects.GetLength(1) &&
                        tileGameObjects[x, y] != null)
                    {
                        Destroy(tileGameObjects[x, y]);
                    }
                }
            }
        }
        grid = null;
        tileGameObjects = null;

        foreach (GameObject outlineGO in currentPlayerAttackOutlineGOs) { if (outlineGO != null) Destroy(outlineGO); }
        currentPlayerAttackOutlineGOs.Clear();

        foreach (Tile tile in currentlyHighlightedEnemyAttackTiles) { if (tile != null && tile.gameObject != null) { } }
        currentlyHighlightedEnemyAttackTiles.Clear();
    }

    GameObject GetEnemyPrefabForFloor(int floor)
    {
        if (floor < 2) return lizardTilePrefab;
        if (floor < 4)
        {
            return Random.value < 0.7f ? lizardTilePrefab : rhinoTilePrefab;
        }
        if (floor < finalFloor)
        {
            float rand = Random.value;
            if (rand < 0.5f) return lizardTilePrefab;
            if (rand < 0.8f) return rhinoTilePrefab;
            return deerTilePrefab;
        }
        if (floor == finalFloor)
        {
            return trollTilePrefab;
        }
        return lizardTilePrefab;
    }

    #endregion

    #region Game Logic

    void AttemptSwap(Vector2Int moveDirection)
    {
        if (swapAnimationActive) return;

        Vector2Int playerTileGridPos = playerGridPos;
        Vector2Int targetTileGridPos = playerTileGridPos + moveDirection;

        if (!InBounds(targetTileGridPos)) return;

        Tile playerTile = grid[playerTileGridPos.x, playerTileGridPos.y];
        Tile targetTile = grid[targetTileGridPos.x, targetTileGridPos.y];

        GameObject playerTileGO = tileGameObjects[playerTileGridPos.x, playerTileGridPos.y];
        GameObject targetTileGO = tileGameObjects[targetTileGridPos.x, targetTileGridPos.y];

        grid[playerTileGridPos.x, playerTileGridPos.y] = targetTile;
        grid[targetTileGridPos.x, targetTileGridPos.y] = playerTile;

        playerTile.gridPosition = targetTileGridPos;
        targetTile.gridPosition = playerTileGridPos;

        tileGameObjects[playerTileGridPos.x, playerTileGridPos.y] = targetTileGO;
        tileGameObjects[targetTileGridPos.x, targetTileGridPos.y] = playerTileGO;

        playerGridPos = targetTileGridPos;

        if (moveDirection != Vector2Int.zero)
            playerFacing = moveDirection;

        ClearPlayerAttackAreaVisuals();

        Vector3 finalPosPlayerTile = GridToWorldPosition(targetTileGridPos);
        Vector3 finalPosTargetTile = GridToWorldPosition(playerTileGridPos);
        Vector3 finalPosPlayerVisual = finalPosPlayerTile;
        if (playerGameObject) finalPosPlayerVisual.z = playerGameObject.transform.position.z;

        StartCoroutine(AnimateSwapCoroutine(
            playerTileGO, finalPosPlayerTile,
            targetTileGO, finalPosTargetTile,
            playerGameObject, finalPosPlayerVisual,
            targetTile, playerGridPos
        ));
    }

    public bool AttemptEnemyTileSwap(Tile enemyTileToMove, GameObject enemyGO, Vector2Int worldDirectionEnemyMoves)
    {
        if (swapAnimationActive) return false;

        Vector2Int enemyCurrentPos_Grid = enemyTileToMove.gridPosition;
        Vector2Int enemyTargetPos_Grid = enemyCurrentPos_Grid + worldDirectionEnemyMoves;

        if (!InBounds(enemyTargetPos_Grid)) return false;

        Tile tileAtTargetLocation = grid[enemyTargetPos_Grid.x, enemyTargetPos_Grid.y];

        if (tileAtTargetLocation.type == TileType.Empty ||
            (tileAtTargetLocation is EnvironmentTile && !(tileAtTargetLocation is TrapTile)))
        {
            GameObject goOfTileAtTarget = tileGameObjects[enemyTargetPos_Grid.x, enemyTargetPos_Grid.y];

            grid[enemyCurrentPos_Grid.x, enemyCurrentPos_Grid.y] = tileAtTargetLocation;
            grid[enemyTargetPos_Grid.x, enemyTargetPos_Grid.y] = enemyTileToMove;

            enemyTileToMove.gridPosition = enemyTargetPos_Grid;
            tileAtTargetLocation.gridPosition = enemyCurrentPos_Grid;

            tileGameObjects[enemyCurrentPos_Grid.x, enemyCurrentPos_Grid.y] = goOfTileAtTarget;
            tileGameObjects[enemyTargetPos_Grid.x, enemyTargetPos_Grid.y] = enemyGO;

            if (enemyTileToMove is EnemyTile et && worldDirectionEnemyMoves != Vector2Int.zero)
            {
                et.SetFacingDirection(worldDirectionEnemyMoves);
            }

            StartCoroutine(AnimateSimpleTilePairSwap(
                enemyGO, GridToWorldPosition(enemyTargetPos_Grid),
                goOfTileAtTarget, GridToWorldPosition(enemyCurrentPos_Grid)
            ));

            return true;
        }
        return false;
    }

    List<Tile> PerformPlayerAttackAndGetHitTiles(Vector2Int playerAttackOriginGridPos)
    {
        List<Tile> hitTiles = new List<Tile>();
        if (playerController == null || isGameOver) return hitTiles;

        List<Vector2Int> relativeAttackPattern = playerController.GetCurrentAttackPatternRelative(playerFacing);

        foreach (Vector2Int relativePos in relativeAttackPattern)
        {
            Vector2Int targetGridPos = playerAttackOriginGridPos + relativePos;
            if (InBounds(targetGridPos))
            {
                Tile targetTile = grid[targetGridPos.x, targetGridPos.y];
                if (targetTile != null) hitTiles.Add(targetTile);

                if (targetTile is EnemyTile enemy && !enemy.IsDefeated())
                {
                    enemy.TakeDamage(playerController.attackDamage);
                }
            }
        }
        playerController.CycleAttackMode();
        return hitTiles;
    }

    void ProcessEnemyActions()
    {
        if (isGameOver || playerController == null) return;

        for (int x = 0; x < currentDynamicGridSize; x++)
        {
            for (int y = 0; y < currentDynamicGridSize; y++)
            {
                if (grid[x, y] is EnemyTile enemyTile)
                {
                    if (enemyTile != null && enemyTile.gameObject.activeInHierarchy && !enemyTile.IsDefeated())
                    {
                        enemyTile.PerformAction(playerController, playerGridPos, grid);
                        if (isGameOver) return;
                    }
                }
            }
        }
    }

    public void EnemyDefeated()
    {
        if (isGameOver) return;
        activeEnemiesCount--;
        currentScore += scoreKillEnemy;
        UpdateScoreDisplay();
        UpdateAllHighlightDisplays();
    }

    public void GameOver(string message)
    {
        if (isGameOver) return;
        isGameOver = true;

        StartCoroutine(FlashScreen(message.Contains("Congradulations!") ? winFlashColor : deathFlashColor));

        if (messageText != null)
        {
            messageText.text = $"{message}\nPress 'R' to restart.";
        }
    }

    public void LevelCleared()
    {
        if (isGameOver) return;

        currentScore += scoreAdvanceLevel;
        UpdateScoreDisplay();

        currentFloor++;

        if (currentFloor > finalFloor)
        {
            GameOver($"Congradulations!\nYou cleared the dungeon.\nYour score is: {currentScore}");
        }
        else
        {
            ClearBoardForNewLevel();
            GenerateLevel();
        }
    }

    void ClearBoardForNewLevel()
    {
        for (int x = 0; x < currentDynamicGridSize; x++)
        {
            for (int y = 0; y < currentDynamicGridSize; y++)
            {
                if (tileGameObjects[x, y] != null)
                {
                    Destroy(tileGameObjects[x, y]);
                }
                grid[x, y] = null;
            }
        }

        ClearAllDisplayedEnemyAttackHighlights();
        ClearPlayerAttackAreaVisuals();
        hoveredEnemy = null;
        moveQueue.Clear();
    }

    public void CollectKey()
    {
        hasKey = true;
        currentScore += scoreCollectKey;
        UpdateScoreDisplay();

        if (InBounds(currentLevelGoalPosition))
        {
            Tile goalInstance = grid[currentLevelGoalPosition.x, currentLevelGoalPosition.y];
            if (goalInstance is GoalTile gt)
            {
                gt.UpdateVisualState();
            }
        }
    }

    void ApplyTurnPenalty()
    {
        if (isGameOver) return;

        currentScore -= scorePenaltyPerTurn;
        if (currentScore < 0)
        {
            currentScore = 0;
        }
        UpdateScoreDisplay();
        Debug.Log($"Turn penalty applied. Score: {currentScore}");
    }

    #endregion

    #region Visuals

    void UpdateAllHighlightDisplays()
    {
        if (isGameOver)
        {
            ClearAllDisplayedEnemyAttackHighlights();
            ClearPlayerAttackAreaVisuals();
            return;
        }

        if (swapAnimationActive)
            return;

        ClearAllDisplayedEnemyAttackHighlights();
        if (showAllHighlightsOverride)
        {
            HighlightAllEnemyAttackAreas();
        }
        else if (hoveredEnemy != null && !hoveredEnemy.IsDefeated())
        {
            HighlightSingleEnemyAttackArea(hoveredEnemy);
        }

        UpdatePlayerAttackAreaVisuals();
    }

    void ClearPlayerAttackAreaVisuals()
    {
        foreach (GameObject outlineGO in currentPlayerAttackOutlineGOs)
        {
            Destroy(outlineGO);
        }
        currentPlayerAttackOutlineGOs.Clear();
    }

    void ClearAllDisplayedEnemyAttackHighlights()
    {
        foreach (Tile tile in currentlyHighlightedEnemyAttackTiles)
        {
            tile?.SetHighlight(false, attackHighlightColor);
        }
        currentlyHighlightedEnemyAttackTiles.Clear();
    }

    void UpdatePlayerAttackAreaVisuals()
    {
        if (playerController == null || playerAttackOutlinePrefab == null) return;

        List<Vector2Int> relativePattern = playerController.GetCurrentAttackPatternRelative(playerFacing);
        foreach (Vector2Int relativePos in relativePattern)
        {
            Vector2Int targetGridPos = playerGridPos + relativePos;
            if (InBounds(targetGridPos))
            {
                Vector3 worldPos = GridToWorldPosition(targetGridPos);
                GameObject outlineGO = Instantiate(playerAttackOutlinePrefab, worldPos, Quaternion.identity, this.transform);
                currentPlayerAttackOutlineGOs.Add(outlineGO);
            }
        }
    }

    void HighlightSingleEnemyAttackArea(EnemyTile enemy)
    {
        if (enemy == null || enemy.IsDefeated()) return;

        List<Vector2Int> attackPattern = enemy.GetCurrentAttackPatternWorldPositions();
        foreach (Vector2Int targetPos in attackPattern)
        {
            if (InBounds(targetPos))
            {
                Tile tileToHighlight = grid[targetPos.x, targetPos.y];
                if (tileToHighlight != null)
                {
                    tileToHighlight.SetHighlight(true, attackHighlightColor);
                    if (!currentlyHighlightedEnemyAttackTiles.Contains(tileToHighlight))
                    {
                        currentlyHighlightedEnemyAttackTiles.Add(tileToHighlight);
                    }
                }
            }
        }
    }

    void HighlightAllEnemyAttackAreas()
    {
        for (int x = 0; x < currentDynamicGridSize; x++)
        {
            for (int y = 0; y < currentDynamicGridSize; y++)
            {
                if (grid[x, y] is EnemyTile enemy && !enemy.IsDefeated())
                {
                    HighlightSingleEnemyAttackArea(enemy);
                }
            }
        }
    }

    void UpdateScoreDisplay()
    {
        if (scoreDisplayUIText != null)
        {
            scoreDisplayUIText.text = $"score: {currentScore}";
        }
    }

    void ClearHoverVisuals(Tile tileToClear)
    {
        if (tileToClear == null) return;

        GameObject tileGO = tileGameObjects[tileToClear.gridPosition.x, tileToClear.gridPosition.y];
        if (tileGO != null)
        {
            DOTween.Kill(tileGO.transform, true);
            currentHoverScaleTween = tileGO.transform.DOScale(originalScaleHoveredTile, hoverScaleDuration).SetEase(Ease.OutQuad);
        }
    }

    #endregion

    #region Animations

    private IEnumerator AnimateSwapCoroutine(
        GameObject tile1GO, Vector3 tile1FinalPos,
        GameObject tile2GO, Vector3 tile2FinalPos,
        GameObject playerVisualGO, Vector3 playerVisualTargetPos,
        Tile tileSwappedWithPlayer, Vector2Int playerNewGridPos_AttackOrigin)
    {
        swapAnimationActive = true;
        bool levelWasClearedThisTurn = false;
        float elapsedTime = 0f;

        ClearPlayerAttackAreaVisuals();

        Vector3 tile1StartPos = tile1GO.transform.position;
        Vector3 tile2StartPos = tile2GO.transform.position;
        Vector3 playerVisualStartPos = playerVisualGO != null ? playerVisualGO.transform.position : Vector3.zero;

        SpriteRenderer tile1Sr = tile1GO.GetComponent<SpriteRenderer>();
        SpriteRenderer tile2Sr = tile2GO.GetComponent<SpriteRenderer>();
        SpriteRenderer playerSr = playerVisualGO?.GetComponent<SpriteRenderer>();

        int tile1OriginalOrder = tile1Sr?.sortingOrder ?? 0;
        int tile2OriginalOrder = tile2Sr?.sortingOrder ?? 0;
        int playerOriginalOrder = playerSr?.sortingOrder ?? 0;

        if (playerSr != null) playerSr.sortingOrder = 102;
        if (tile1Sr != null) tile1Sr.sortingOrder = 101;
        if (tile2Sr != null) tile2Sr.sortingOrder = 100;

        List<GameObject> tempPreviewOutlines = new List<GameObject>();
        if (playerController != null && playerAttackOutlinePrefab != null)
        {
            List<Vector2Int> previewPattern = playerController.GetCurrentAttackPatternRelative(playerFacing);
            foreach (Vector2Int relativePos in previewPattern)
            {
                Vector2Int targetGridPos = playerNewGridPos_AttackOrigin + relativePos;
                if (InBounds(targetGridPos))
                {
                    Vector3 worldPos = GridToWorldPosition(targetGridPos);
                    GameObject outlineGO = Instantiate(playerAttackOutlinePrefab, worldPos, Quaternion.identity, transform);
                    SpriteRenderer olSr = outlineGO.GetComponent<SpriteRenderer>();
                    if (olSr != null) olSr.color = new Color(attackHighlightColor.r + 0.1f, attackHighlightColor.g + 0.1f, attackHighlightColor.b + 0.1f, 1f);
                    tempPreviewOutlines.Add(outlineGO);
                }
            }
        }

        while (elapsedTime < swapAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / swapAnimationDuration);

            t = t * t * (3f - t * 2f);

            tile1GO.transform.position = Vector3.Lerp(tile1StartPos, tile1FinalPos, t);
            tile2GO.transform.position = Vector3.Lerp(tile2StartPos, tile2FinalPos, t);

            if (playerVisualGO != null)
                playerVisualGO.transform.position = Vector3.Lerp(playerVisualStartPos, playerVisualTargetPos, t);
            yield return null;
        }

        tile1GO.transform.position = tile1FinalPos;
        tile2GO.transform.position = tile2FinalPos;
        if (playerVisualGO != null) playerVisualGO.transform.position = playerVisualTargetPos;

        if (tile1Sr != null) tile1Sr.sortingOrder = tile1OriginalOrder;
        if (tile2Sr != null) tile2Sr.sortingOrder = tile2OriginalOrder;
        if (playerSr != null) playerSr.sortingOrder = playerOriginalOrder;

        tile1GO.transform.rotation = Quaternion.identity;
        tile2GO.transform.rotation = Quaternion.identity;

        currentScore -= scorePenaltyPerTurn;

        List<Tile> tilesHitByPlayer = PerformPlayerAttackAndGetHitTiles(playerNewGridPos_AttackOrigin);
        StartCoroutine(FlashHitTiles(tilesHitByPlayer, enemyHitFlashColor, enemyHitFlashDuration));

        foreach (GameObject outline in tempPreviewOutlines) Destroy(outline);
        tempPreviewOutlines.Clear();

        if (isGameOver)
        {
            swapAnimationActive = false;
            UpdateAllHighlightDisplays();
            yield break;
        }

        bool canInteract = true;
        if (tileSwappedWithPlayer is EnemyTile swappedEnemy && swappedEnemy.IsDefeated())
        {
            canInteract = false;
        }

        if (canInteract && tileSwappedWithPlayer != null && !isGameOver)
        {
            int floorBeforeInteraction = currentFloor;
            tileSwappedWithPlayer.OnPlayerSwap(playerController);
            if (currentFloor > floorBeforeInteraction || (isGameOver && currentFloor > finalFloor))
            {
                levelWasClearedThisTurn = true;
            }
        }

        swapAnimationActive = false;

        if (levelWasClearedThisTurn)
        {
            yield break;
        }

        if (!isGameOver)
        {
            ProcessEnemyActions();
        }

        if (!swapAnimationActive)
        {
            UpdateAllHighlightDisplays();
        }
        yield return null;
    }

    private IEnumerator AnimateSimpleTilePairSwap(
        GameObject tile1GO, Vector3 tile1TargetPos,
        GameObject tile2GO, Vector3 tile2TargetPos)
    {
        swapAnimationActive = true;
        float elapsedTime = 0f;

        Vector3 tile1StartPos = tile1GO.transform.position;
        Vector3 tile2StartPos = tile2GO.transform.position;

        SpriteRenderer sr1 = tile1GO.GetComponent<SpriteRenderer>();
        SpriteRenderer sr2 = tile2GO.GetComponent<SpriteRenderer>();
        int order1Original = sr1?.sortingOrder ?? 0;
        int order2Original = sr2?.sortingOrder ?? 0;

        if (sr1 != null) sr1.sortingOrder = 90;
        if (sr2 != null) sr2.sortingOrder = 89;

        while (elapsedTime < enemySwapAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / enemySwapAnimationDuration);
            t = t * t * (3f - 2f * t);

            tile1GO.transform.position = Vector3.Lerp(tile1StartPos, tile1TargetPos, t);
            tile2GO.transform.position = Vector3.Lerp(tile2StartPos, tile2TargetPos, t);
            yield return null;
        }

        tile1GO.transform.position = tile1TargetPos;
        tile2GO.transform.position = tile2TargetPos;

        if (sr1 != null) sr1.sortingOrder = order1Original;
        if (sr2 != null) sr2.sortingOrder = order2Original;

        swapAnimationActive = false;
        UpdateAllHighlightDisplays();
        yield return null;
    }

    private IEnumerator AnimateSnapBackCoroutine(GameObject tileGO, Vector3 targetPos, int originalSortingOrderToRestore)
    {
        swapAnimationActive = true;
        tileGO.transform.DORotate(Vector3.zero, swapAnimationDuration * 0.5f).SetEase(Ease.OutBack);


        float elapsedTime = 0f;
        Vector3 startPos = tileGO.transform.position;
        SpriteRenderer sr = tileGO.GetComponent<SpriteRenderer>();

        while (elapsedTime < swapAnimationDuration * 0.75f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / (swapAnimationDuration * 0.75f));
            t = t * t * (3f - 2f * t);
            tileGO.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        tileGO.transform.position = targetPos;
        if (sr != null) sr.sortingOrder = originalSortingOrderToRestore;

        swapAnimationActive = false;
        UpdateAllHighlightDisplays();
        yield return null;
    }

    private IEnumerator FlashHitTiles(List<Tile> tilesToFlash, Color flashColor, float duration)
    {
        if (tilesToFlash == null || tilesToFlash.Count == 0) yield break;

        Dictionary<SpriteRenderer, Color> originalTileColors = new();

        foreach (Tile tile in tilesToFlash)
        {
            if (tile != null)
            {
                if (!tile.TryGetComponent<SpriteRenderer>(out var sr)) continue;

                if (tile is EnemyTile enemy && !enemy.IsDefeated()) continue;

                if (originalTileColors.TryAdd(sr, sr.color))
                    sr.color = flashColor;
            }
        }

        if (originalTileColors.Count > 0)
        {
            yield return new WaitForSeconds(duration / 2);
            foreach (KeyValuePair<SpriteRenderer, Color> entry in originalTileColors)
            {
                if (entry.Key != null)
                {
                    entry.Key.color = entry.Value;
                }
            }
        }
    }

    #endregion

    #region Helpers

    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float worldX = (gridPos.x - (currentDynamicGridSize - 1) / 2.0f) * tileSize;
        float worldY = (gridPos.y - (currentDynamicGridSize - 1) / 2.0f) * tileSize;
        return new Vector3(worldX, worldY, 0);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float centerX_grid = (currentDynamicGridSize - 1) / 2.0f;
        float centerY_grid = (currentDynamicGridSize - 1) / 2.0f;

        int gridX = Mathf.RoundToInt(worldPos.x / tileSize + centerX_grid);
        int gridY = Mathf.RoundToInt(worldPos.y / tileSize + centerY_grid);
        return new Vector2Int(gridX, gridY);
    }

    public bool InBounds(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < currentDynamicGridSize &&
               gridPos.y >= 0 && gridPos.y < currentDynamicGridSize;
    }

    bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    bool IsValid(Vector2Int vec)
    {
        return (Mathf.Abs(vec.x) == 1 && vec.y == 0) || (Mathf.Abs(vec.y) == 1 && vec.x == 0);
    }

    bool IsDropValid(Vector3 mouseReleaseWorldPos, Vector2Int dragStartGridPos, Vector2Int currentGapGridPos)
    {
        Vector2Int intendedStepToGap = currentGapGridPos - dragStartGridPos;

        if (!IsValid(intendedStepToGap)) return false;

        bool isHorizontalDrag = Mathf.Abs(intendedStepToGap.x) > 0;

        Vector3 gapTileWorldCenter = GridToWorldPosition(currentGapGridPos);

        if (isHorizontalDrag)
        {
            float thresholdX = gapTileWorldCenter.x;
            if (intendedStepToGap.x > 0)
            {
                if (mouseReleaseWorldPos.x >= thresholdX) return true;
            }
            else
            {
                if (mouseReleaseWorldPos.x <= thresholdX) return true;
            }
        }
        else
        {
            float thresholdY = gapTileWorldCenter.y;
            if (intendedStepToGap.y > 0)
            {
                if (mouseReleaseWorldPos.y >= thresholdY) return true;
            }
            else
            {
                if (mouseReleaseWorldPos.y <= thresholdY) return true;
            }
        }
        return false;
    }

    GameObject GetPrefabForType(TileType type)
    {
        return type switch
        {
            TileType.Empty => emptyTilePrefab,
            TileType.Trap => trapTilePrefab,
            TileType.HealthPickup => healthPickupPrefab,
            TileType.Player => playerTilePrefab,
            TileType.Goal => goalTilePrefab,
            TileType.Key => keyTilePrefab,
            TileType.Environment => Random.value > 0.5f ? rockTilePrefab : boulderTilePrefab, //! fix later
            _ => emptyTilePrefab,
        };
    }

    private IEnumerator FlashScreen(Color flashColor)
    {
        if (fullScreenFlashPanel == null) yield break;

        Image panelImage = fullScreenFlashPanel.GetComponent<Image>();
        if (panelImage == null) yield break;

        fullScreenFlashPanel.SetActive(true);
        panelImage.color = flashColor;

        float elapsedTime = 0f;
        Color transparentColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            panelImage.color = Color.Lerp(flashColor, transparentColor, elapsedTime / flashDuration);
            yield return null;
        }
        
        panelImage.color = transparentColor;
        fullScreenFlashPanel.SetActive(false);
    }

    #endregion
}