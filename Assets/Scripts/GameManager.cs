using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

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

        if (Input.GetKeyDown(KeyCode.R)) {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        if (isGameOver) return;

        HandleKeyOverrides();
        HandleKeyboardInput();
        HandleMouseInput();

        ProcessMoveQueue();
    }

    #endregion

    #region Unity Fields

    [Header("Scene Names")]
    public string gameSceneName = "Game";
    public string mainMenuSceneName = "Menu";

    [Header("Grid Settings")]
    public float tileSize = 1.0f;
    public int minGridSize = 4;
    public int maxGridSize = 7;
    public int floorsPerSizeIncrease = 3;

    [Header("Random Level Settings")]
    public int numberOfEnemies = 3;
    public int numberOfTraps = 2;
    public int numberOfHealthPickups = 1;

    [Header("Prefabs")]
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

    [Header("Scene UI References")]
    public Text scenePlayerHealthText;
    public Text messageText;

    [Header("Enemy Attack Highlighting")]
    public Color attackHighlightColor = new Color(1f, 0.5f, 0.5f, 0.75f);
    public KeyCode showAllAttackAreasKey = KeyCode.LeftAlt;

    [Header("Attack Highlighting")]
    public Color enemyHitFlashColor = Color.white;
    public float enemyHitFlashDuration = 0.15f;

    [Header("Game Progression")]
    public int currentFloor = 1;
    public int targetFloorToWinGame = 3;
    public Text floorDisplayUIText;
    private bool hasKeyOnCurrentFloor = false;
    private Vector2Int currentLevelGoalPosition;

    [Header("Score System")]
    public int scoreKillEnemy = 100;
    public int scoreAdvanceLevel = 250;
    public int scoreCollectKey = 250;
    public int scorePenaltyPerTurn = 10;
    private int currentScore = 0;
    public Text scoreDisplayUIText;

    [Header("Player Attack Settings")]
    public Vector2Int playerAttackFacingDirection = Vector2Int.up;

    [Header("Animation Settings")]
    public float swapAnimationDuration = 0.25f;
    public float enemySwapAnimationDuration = 0.1f;
    private bool swapAnimationActive = false;

    [Header("Input Queue")]
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

    public bool IsAnimating() => swapAnimationActive;
    public bool HasKey() => hasKeyOnCurrentFloor;

    #endregion

    #region Input 

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
        // continuous update while Alt is held (in case enemies move/die while Alt is held):
        else if (Input.GetKey(showAllAttackAreasKey) && !showAllHighlightsOverride)
        {
            // This case is if GetKeyDown was missed but key is held
            showAllHighlightsOverride = true;
            needsHighlightUpdate = true;
        }
        else if (!Input.GetKey(showAllAttackAreasKey) && showAllHighlightsOverride)
        {
            // This case is if GetKeyUp was missed but key is no longer held
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

    void HandleMouseInput()
    {
        if (swapAnimationActive && moveQueue.Count >= maxQueuedMoves && tileBeingDragged == null)
            return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector2Int currentMouseGridPos = WorldToGridPosition(mouseWorldPos);

        // Hover
        if (tileBeingDragged == null && !Input.GetMouseButton(0) && !swapAnimationActive)
        {
            EnemyTile previouslyHovered = hoveredEnemy;
            hoveredEnemy = null;

            if (InBounds(currentMouseGridPos))
            {
                Tile tileUnderMouse = grid[currentMouseGridPos.x, currentMouseGridPos.y];
                if (tileUnderMouse is EnemyTile enemy && !enemy.IsDefeated())
                {
                    hoveredEnemy = enemy;
                }
            }

            if (previouslyHovered != hoveredEnemy)
                UpdateAllHighlightDisplays();
        }

        // Mouse Down. Start drag?
        if (Input.GetMouseButtonDown(0))
        {
            if (tileBeingDragged == null && (!swapAnimationActive || moveQueue.Count < maxQueuedMoves))
            {
                if (InBounds(currentMouseGridPos))
                {
                    if (IsAdjacent(currentMouseGridPos, playerGridPos) && grid[currentMouseGridPos.x, currentMouseGridPos.y].type != TileType.Player)
                    {
                        tileBeingDragged = grid[currentMouseGridPos.x, currentMouseGridPos.y];
                        dragTileOriginalGridPos = currentMouseGridPos;
                        visualTileBeingDragged = tileGameObjects[currentMouseGridPos.x, currentMouseGridPos.y];

                        if (visualTileBeingDragged != null)
                        {
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
                        }
                        else { tileBeingDragged = null; }
                    }
                }
            }
        }

        // keep dragging while down
        if (Input.GetMouseButton(0) && tileBeingDragged != null && visualTileBeingDragged != null)
        {
            visualTileBeingDragged.transform.position = mouseWorldPos + mouseOffsetFromTileCenter;
        }

        // Mouse Up: try swap
        if (Input.GetMouseButtonUp(0))
        {
            if (tileBeingDragged != null && visualTileBeingDragged != null)
            {
                bool canSwapFromDrop = IsDropValid(mouseWorldPos, dragTileOriginalGridPos, playerGridPos);

                if (canSwapFromDrop)
                {
                    Vector2Int worldDirectionPlayerMoves = dragTileOriginalGridPos - playerGridPos;

                    if (moveQueue.Count < maxQueuedMoves)
                        EnqueueMove(worldDirectionPlayerMoves);
                    else
                        StartCoroutine(AnimateSnapBackCoroutine(visualTileBeingDragged,
                            GridToWorldPosition(dragTileOriginalGridPos),
                            draggedTileOriginalSortingOrder
                        ));
                }
                // drop not valid
                else
                {
                    StartCoroutine(AnimateSnapBackCoroutine(visualTileBeingDragged,
                        GridToWorldPosition(dragTileOriginalGridPos),
                        draggedTileOriginalSortingOrder
                    ));
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
        hasKeyOnCurrentFloor = false;
        hoveredEnemy = null;
        tileBeingDragged = null;
        visualTileBeingDragged = null;

        playerGridPos = new Vector2Int(0, 0);
        currentLevelGoalPosition = new Vector2Int(currentDynamicGridSize - 1, currentDynamicGridSize - 1);

        for (int x = 0; x < currentDynamicGridSize; x++)
        {
            for (int y = 0; y < currentDynamicGridSize; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                if (currentPos == playerGridPos || currentPos == currentLevelGoalPosition)
                {
                    SpawnTile(TileType.Empty, currentPos);
                }
                else
                {
                    float randEnv = Random.value;
                    if (randEnv < 0.33f) SpawnTile(TileType.Environment, currentPos, rockTilePrefab);
                    else if (randEnv < 0.66f) SpawnTile(TileType.Environment, currentPos, boulderTilePrefab);
                    else SpawnTile(TileType.Empty, currentPos);
                }
            }
        }

        ReplaceTileInGrid(playerGridPos, TileType.Player, playerTilePrefab);
        if (playerGridPos != currentLevelGoalPosition)
        {
            ReplaceTileInGrid(currentLevelGoalPosition, TileType.Goal, goalTilePrefab);
        }
        Tile goalInstance = grid[currentLevelGoalPosition.x, currentLevelGoalPosition.y];
        if (goalInstance is GoalTile gt) gt.UpdateVisualState();

        List<Vector2Int> forbiddenPlacementSpots = new List<Vector2Int> { playerGridPos, currentLevelGoalPosition };

        bool isTrollFloor = currentFloor == targetFloorToWinGame;
        if (!isTrollFloor)
        {
            PlaceKeyTile(forbiddenPlacementSpots);
        }

        int enemiesThisFloor = numberOfEnemies + ((currentDynamicGridSize - minGridSize) * 1);
        if (isTrollFloor)
        {
            PlaceDynamicContent(TileType.Enemy, 1, forbiddenPlacementSpots);
        }
        else
        {
            PlaceDynamicContent(TileType.Enemy, enemiesThisFloor, forbiddenPlacementSpots);
        }

        int trapsThisFloor = numberOfTraps + ((currentDynamicGridSize - minGridSize) / 2);
        PlaceDynamicContent(TileType.Trap, trapsThisFloor, forbiddenPlacementSpots);

        int healthThisFloor = numberOfHealthPickups;
        PlaceDynamicContent(TileType.HealthPickup, healthThisFloor, forbiddenPlacementSpots);

        SpawnPlayerVisual();
        UpdateFloorDisplay();
        UpdateAllHighlightDisplays();
    }

    void PlaceKeyTile(List<Vector2Int> occupiedInitially)
    {
        List<Vector2Int> possibleKeySpots = new List<Vector2Int>();
        for (int x = 0; x < currentDynamicGridSize; x++)
        {
            for (int y = 0; y < currentDynamicGridSize; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!occupiedInitially.Contains(pos) && (grid[x, y] == null || grid[x, y].type == TileType.Empty || grid[x, y] is EnvironmentTile))
                {
                    possibleKeySpots.Add(pos);
                }
            }
        }

        if (possibleKeySpots.Count > 0)
        {
            Vector2Int keyPos = possibleKeySpots[Random.Range(0, possibleKeySpots.Count)];
            ReplaceTileInGrid(keyPos, TileType.Key, keyTilePrefab);
            occupiedInitially.Add(keyPos);
            Debug.Log($"Key placed at {keyPos}");
        }
        else
        {
            // ?? change later
        }
    }

    void PlaceDynamicContent(TileType typeToPlace, int count, List<Vector2Int> occupiedAndForbiddenSpots)
    {
        int placedSuccessfully = 0;
        int placementAttempts = 0;
        int maxPlacementAttempts = currentDynamicGridSize * currentDynamicGridSize * 2;

        List<Vector2Int> potentialSpots = new List<Vector2Int>();
        for (int x = 0; x < currentDynamicGridSize; x++)
        {
            for (int y = 0; y < currentDynamicGridSize; y++)
            {
                potentialSpots.Add(new Vector2Int(x, y));
            }
        }

        for (int i = 0; i < potentialSpots.Count; i++)
        {
            Vector2Int temp = potentialSpots[i];
            int randomIndex = Random.Range(i, potentialSpots.Count);
            potentialSpots[i] = potentialSpots[randomIndex];
            potentialSpots[randomIndex] = temp;
        }

        foreach (Vector2Int randomPos in potentialSpots)
        {
            if (placedSuccessfully >= count) break;
            if (placementAttempts >= maxPlacementAttempts) break;
            placementAttempts++;

            if (!occupiedAndForbiddenSpots.Contains(randomPos))
            {
                Tile existingTileAtSpot = grid[randomPos.x, randomPos.y];
                if (existingTileAtSpot != null &&
                    (existingTileAtSpot.type == TileType.Empty || existingTileAtSpot.type == TileType.Environment))
                {
                    GameObject prefabToSpawn = null;

                    if (typeToPlace == TileType.Enemy)
                    {
                        bool isTrollFloor = currentFloor == targetFloorToWinGame;
                        if (isTrollFloor)
                        {
                            prefabToSpawn = trollTilePrefab;
                        }
                        else
                        {
                            prefabToSpawn = GetRegularEnemyPrefabForFloor(currentFloor);
                        }
                    }
                    else
                    {
                        prefabToSpawn = GetPrefabForType(typeToPlace);
                    }


                    if (prefabToSpawn != null)
                    {
                        ReplaceTileInGrid(randomPos, typeToPlace, prefabToSpawn);
                        occupiedAndForbiddenSpots.Add(randomPos);
                        if (typeToPlace == TileType.Enemy) activeEnemiesCount++;
                        placedSuccessfully++;
                    }
                }
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
        ReplaceTileInGrid(pos, newType, specificPrefab); // This already handles destroying old and spawning new
    }

    int GetGridSizeForFloor(int floor)
    {
        int sizeIncreases = (floor - 1) / floorsPerSizeIncrease;
        int calculatedSize = minGridSize + sizeIncreases;
        return Mathf.Min(calculatedSize, maxGridSize);
    }

    float GetCameraOrthoSizeForGrid(int actualGridSize)
    {
        float desiredHeight = (actualGridSize * tileSize) + (tileSize * 1.0f); // Grid height + 1 tile padding
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

    GameObject GetRegularEnemyPrefabForFloor(int floor)
    {
        if (floor < 2) return lizardTilePrefab;
        if (floor < 4)
        {
            return Random.value < 0.6f ? lizardTilePrefab : rhinoTilePrefab;
        }
        if (floor < targetFloorToWinGame)
        {
            float rand = Random.value;
            if (rand < 0.4f) return lizardTilePrefab;
            if (rand < 0.75f) return rhinoTilePrefab;
            return deerTilePrefab;
        }
        return lizardTilePrefab;
    }

    #endregion

    #region Game Logic

    void AttemptSwap(Vector2Int worldDirectionPlayerWantsToMove)
    {
        if (swapAnimationActive) return;

        Vector2Int currentPlayerTilePos_Grid = playerGridPos;
        Vector2Int targetContentTilePos_Grid = currentPlayerTilePos_Grid + worldDirectionPlayerWantsToMove;

        if (!InBounds(targetContentTilePos_Grid)) return;

        Tile playerTileInstance = grid[currentPlayerTilePos_Grid.x, currentPlayerTilePos_Grid.y];
        Tile contentTileToSwapWith = grid[targetContentTilePos_Grid.x, targetContentTilePos_Grid.y];

        GameObject goOfPlayerTile = tileGameObjects[currentPlayerTilePos_Grid.x, currentPlayerTilePos_Grid.y];
        GameObject goOfContentTile = tileGameObjects[targetContentTilePos_Grid.x, targetContentTilePos_Grid.y];

        grid[currentPlayerTilePos_Grid.x, currentPlayerTilePos_Grid.y] = contentTileToSwapWith;
        grid[targetContentTilePos_Grid.x, targetContentTilePos_Grid.y] = playerTileInstance;

        playerTileInstance.gridPosition = targetContentTilePos_Grid;
        contentTileToSwapWith.gridPosition = currentPlayerTilePos_Grid;

        tileGameObjects[currentPlayerTilePos_Grid.x, currentPlayerTilePos_Grid.y] = goOfContentTile;
        tileGameObjects[targetContentTilePos_Grid.x, targetContentTilePos_Grid.y] = goOfPlayerTile;

        playerGridPos = targetContentTilePos_Grid;

        if (worldDirectionPlayerWantsToMove != Vector2Int.zero)
            playerAttackFacingDirection = worldDirectionPlayerWantsToMove;

        ClearPlayerAttackAreaVisuals();

        Vector3 targetPosForPlayerTile = GridToWorldPosition(targetContentTilePos_Grid);
        Vector3 targetPosForContentTile = GridToWorldPosition(currentPlayerTilePos_Grid);
        Vector3 targetPosForPlayerVisual = targetPosForPlayerTile;
        if (playerGameObject) targetPosForPlayerVisual.z = playerGameObject.transform.position.z;

        StartCoroutine(AnimateSwapCoroutine(
            goOfPlayerTile, targetPosForPlayerTile,
            goOfContentTile, targetPosForContentTile,
            playerGameObject, targetPosForPlayerVisual,
            contentTileToSwapWith, playerGridPos
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

        List<Vector2Int> relativeAttackPattern = playerController.GetCurrentAttackPatternRelative(playerAttackFacingDirection);

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

        if (currentFloor > targetFloorToWinGame)
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
        hasKeyOnCurrentFloor = true;
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

        List<Vector2Int> relativePattern = playerController.GetCurrentAttackPatternRelative(playerAttackFacingDirection);
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
            List<Vector2Int> previewPattern = playerController.GetCurrentAttackPatternRelative(playerAttackFacingDirection);
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

            t = 1f - (1f - t) * (1f - t);
            // t = t * t * (3f - t * 2f);

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
            tileSwappedWithPlayer.OnPlayerEnter(playerController);
            if (currentFloor > floorBeforeInteraction || (isGameOver && currentFloor > targetFloorToWinGame) )
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

    private IEnumerator AnimateSnapBackCoroutine(GameObject tileGO, Vector3 targetPos, int originalOrder)
    {
        swapAnimationActive = true;
        float elapsedTime = 0f;
        Vector3 startPos = tileGO.transform.position;
        SpriteRenderer sr = tileGO.GetComponent<SpriteRenderer>();

        while (elapsedTime < swapAnimationDuration * 0.75f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / (swapAnimationDuration * 0.75f));
            tileGO.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        tileGO.transform.position = targetPos;
        if (sr != null) sr.sortingOrder = originalOrder;

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
            yield return new WaitForSeconds(duration/2);
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
        switch (type)
        {
            case TileType.Empty: return emptyTilePrefab;
            case TileType.Trap: return trapTilePrefab;
            case TileType.HealthPickup: return healthPickupPrefab;
            case TileType.Player: return playerTilePrefab;
            case TileType.Goal: return goalTilePrefab;
            case TileType.Key: return keyTilePrefab;
            case TileType.Environment: return Random.value > 0.5f ? rockTilePrefab : boulderTilePrefab; //! fix later
            default: return emptyTilePrefab;
        }
    }

    #endregion
}