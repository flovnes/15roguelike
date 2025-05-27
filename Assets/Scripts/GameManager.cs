using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    #region Singleton & Core Unity Methods
    // =====================================================================
    // Singleton & Core Unity Methods
    // =====================================================================

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
    }

    void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        HandleKeyOverrides();

        HandleKeyboardInput();
        HandleMouseInput();

        ProcessMoveQueue();
    }

    #endregion

    #region Inspector Assigned Fields
    // =====================================================================
    // Inspector Assigned Fields
    // =====================================================================

    public int gridSize = 5;

    [Header("Grid Settings")]
    public float tileSize = 1.0f;

    [Header("Random Level Settings")]
    public int numberOfEnemies = 3;
    public int numberOfTraps = 2;
    public int numberOfHealthPickups = 1;

    [Header("Prefabs")]
    public GameObject trapTilePrefab;
    public GameObject enemyTilePrefab;
    public GameObject rockTilePrefab;
    public GameObject bushTilePrefab;
    public GameObject healthPickupPrefab;
    public GameObject playerPrefab;
    public GameObject playerTilePrefab;
    public GameObject emptyTilePrefab;
    public GameObject playerAttackOutlinePrefab;

    [Header("Scene UI References (Assign from Scene)")]
    public Text scenePlayerHealthText; 
    public Text messageText;

    [Header("Enemy Attack Highlighting")]
    public Color attackHighlightColor = new Color(1f, 0.5f, 0.5f, 0.75f); 
    public KeyCode showAllAttackAreasKey = KeyCode.LeftAlt; 

    [Header("Attack Highlighting")]
    public Color enemyHitFlashColor = Color.white;
    public float enemyHitFlashDuration = 0.15f;

    [Header("Player Attack Settings")]
    public Vector2Int playerAttackFacingDirection = Vector2Int.up;

    [Header("Animation Settings")]
    public float swapAnimationDuration = 0.25f;
    private bool swapAnimationActive = false;

    [Header("Input Queue")]
    public int maxQueuedMoves = 2;
    private Queue<Vector2Int> moveQueue = new Queue<Vector2Int>();

    #endregion

    #region Internal State Variables
    // =====================================================================
    // Internal State Variables
    // =====================================================================

    // Game Data
    private Tile[,] grid;
    private GameObject[,] tileGameObjects;

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
    private Vector3 mouseOffsetFromTileCenter;        // relative grab point
    private int draggedTileOriginalSortingOrder;
    private SpriteRenderer draggedTileSpriteRenderer;

    private bool isGameOver = false;
    private int activeEnemiesCount = 0;

    #endregion

    #region Queue & Input Handling 
    // =====================================================================
    // Input Handling & Queue
    // =====================================================================

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
        else if (Input.GetKey(showAllAttackAreasKey) && !showAllHighlightsOverride) {
            // This case is if GetKeyDown was missed but key is held
            showAllHighlightsOverride = true;
            needsHighlightUpdate = true;
        } else if (!Input.GetKey(showAllAttackAreasKey) && showAllHighlightsOverride) {
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
                            if(hoveredEnemy != null) {
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

    #region Level Generation & Tile Management
    // =====================================================================
    // Level Generation & Tile Management
    // =====================================================================

    void GenerateLevel()
    {
        grid = new Tile[gridSize, gridSize];
        tileGameObjects = new GameObject[gridSize, gridSize];
        activeEnemiesCount = 0;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                switch ((x + y) % 3)
                {
                    case 0: SpawnTile(TileType.Environment, new Vector2Int(x, y), rockTilePrefab); break;
                    case 1: SpawnTile(TileType.Environment, new Vector2Int(x, y), bushTilePrefab); break;
                    case 2: SpawnTile(TileType.Empty, new Vector2Int(x, y)); break;
                }
            }
        }

        playerGridPos = new Vector2Int(0, 0);
        ReplaceTileInGrid(playerGridPos, TileType.Player, playerTilePrefab);

        List<Vector2Int> occupiedCoordinates = new List<Vector2Int>();
        
        occupiedCoordinates.Add(playerGridPos);

        // Enemies
        PlaceRandomTiles(TileType.Enemy, numberOfEnemies, occupiedCoordinates, new List<TileType> { TileType.Player });

        // Traps
        PlaceRandomTiles(TileType.Trap, numberOfTraps, occupiedCoordinates, new List<TileType> { TileType.Player, TileType.Enemy });

        // Heal
        PlaceRandomTiles(TileType.HealthPickup, numberOfHealthPickups, occupiedCoordinates, new List<TileType> { TileType.Player, TileType.Enemy, TileType.Trap });

        Debug.Log($"level gen end");

        SpawnPlayerVisual();
        UpdateAllHighlightDisplays();
    }

    void PlaceRandomTiles(TileType typeToPlace, int count, List<Vector2Int> occupiedCoordsOutput, List<TileType> forbiddenToOverwriteTypes)
    {
        int placedCount = 0;
        List<Vector2Int> availableSpots = new List<Vector2Int>();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                if (!occupiedCoordsOutput.Contains(currentPos) && !forbiddenToOverwriteTypes.Contains(grid[x, y].type))
                {
                    availableSpots.Add(currentPos);
                }
            }
        }

        int attempts = 0;
        while (placedCount < count && availableSpots.Count > 0 && attempts < gridSize * gridSize)
        {
            attempts++;
            int randomIndex = Random.Range(0, availableSpots.Count);
            Vector2Int randomPos = availableSpots[randomIndex];

            GameObject specificPrefab = GetPrefabForType(typeToPlace); // This needs to be smarter for env.
            if (typeToPlace == TileType.Environment) specificPrefab = Random.value > 0.5f ? rockTilePrefab : bushTilePrefab; // Example

            ReplaceTileInGrid(randomPos, typeToPlace, specificPrefab);
            occupiedCoordsOutput.Add(randomPos);
            availableSpots.RemoveAt(randomIndex);

            if (typeToPlace == TileType.Enemy) activeEnemiesCount++;
            placedCount++;
        }
        if (placedCount < count)
            Debug.LogWarning($" ?! placed only {count} / {placedCount} of {typeToPlace}");
    }

    Tile SpawnTile(TileType type, Vector2Int gridPos, GameObject specificPrefab = null)
    {
        GameObject prefabToUse = specificPrefab != null ? specificPrefab : GetPrefabForType(type);
        if (prefabToUse == null)
            return null;

        Vector3 worldPos = GridToWorldPosition(gridPos);
        GameObject tileGO = Instantiate(prefabToUse, worldPos, Quaternion.identity, this.transform);

        Tile tileScript = tileGO.GetComponent<Tile>();

        //! fixed for now don't need this
        // if (tileScript == null)
        // {
        //     Debug.LogWarning($"Tile script missing on prefab {prefabToUse.name} for type {type}. Adding fallback.");
        //     if (type == TileType.Player) tileScript = tileGO.AddComponent<PlayerTile>();
        //     else if (type == TileType.Enemy) tileScript = tileGO.AddComponent<EnemyTile>();
        //     else if (type == TileType.Trap) tileScript = tileGO.AddComponent<TrapTile>();
        //     else if (type == TileType.HealthPickup) tileScript = tileGO.AddComponent<HealthPickupTile>();
        //     else if (type == TileType.Environment && specificPrefab == rockTilePrefab) tileScript = tileGO.AddComponent<RockTile>();
        //     else if (type == TileType.Environment && specificPrefab == bushTilePrefab) tileScript = tileGO.AddComponent<BushTile>();
        //     else tileScript = tileGO.AddComponent<EmptyTile>();
        // }

        //? panic if happens again for now
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

    void ReplaceTileInGrid(Vector2Int pos, TileType newType, GameObject specificPrefab = null)
    {
        if (tileGameObjects[pos.x, pos.y] != null)
            Destroy(tileGameObjects[pos.x, pos.y]);
        SpawnTile(newType, pos, specificPrefab);
    }

    #endregion

    #region Player Setup
    // =====================================================================
    // Player Setup
    // =====================================================================
    void SpawnPlayerVisual()
    {
        if (playerPrefab == null)
            return;

        Vector3 playerVisualWorldPos = GridToWorldPosition(playerGridPos);
        playerVisualWorldPos.z -= 0.1f;

        playerGameObject = Instantiate(playerPrefab, playerVisualWorldPos, Quaternion.identity);
        playerController = playerGameObject.GetComponent<Player>(); 

        if (playerController != null)
        {
            playerController.healthText = scenePlayerHealthText;
            playerController.currentHealth = playerController.maxHealth;
            playerController.ForceInitialHealthUIDisplayUpdate();
        }
    }

    #endregion

    #region Game State & Logic
    // =====================================================================
    // Core Game Logic & State
    // =====================================================================
    void AttemptSwap(Vector2Int worldDirectionPlayerWantsToMove)
    {
        if (swapAnimationActive) return;
        if (playerController == null)
        {
            Debug.LogError("player is null. again");
            return;
        }

        Vector2Int currentPlayerTilePos_Grid = playerGridPos;
        Vector2Int targetContentTilePos_Grid = currentPlayerTilePos_Grid + worldDirectionPlayerWantsToMove;

        if (!InBounds(targetContentTilePos_Grid)) return;

        // get scripts
        Tile playerTileInstance = grid[currentPlayerTilePos_Grid.x, currentPlayerTilePos_Grid.y];
        Tile contentTileToSwapWith = grid[targetContentTilePos_Grid.x, targetContentTilePos_Grid.y];

        // save game onjects
        GameObject goOfPlayerTile = tileGameObjects[currentPlayerTilePos_Grid.x, currentPlayerTilePos_Grid.y];
        GameObject goOfContentTile = tileGameObjects[targetContentTilePos_Grid.x, targetContentTilePos_Grid.y];

        // LOGICAL SWAP
        
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

        // ANIMATION

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

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
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
        UpdateAllHighlightDisplays();
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        if (isGameOver) return;

        if (activeEnemiesCount <= 0)
        {
            GameOver("You killed all enemies.");
        }
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

    #endregion

    #region Visuals & Highlighting
    // =====================================================================
    // Visuals & Highlighting
    // =====================================================================
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
            if (tile != null)
            {
                tile.SetHighlight(false, attackHighlightColor);
            }
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
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y] is EnemyTile enemy && !enemy.IsDefeated())
                {
                    HighlightSingleEnemyAttackArea(enemy);
                }
            }
        }
    }

    #endregion

    #region Animation Coroutines
    // =====================================================================
    // Animation Coroutines
    // =====================================================================
    private IEnumerator AnimateSwapCoroutine(
        GameObject tile1GO, Vector3 tile1FinalPos,
        GameObject tile2GO, Vector3 tile2FinalPos,
        GameObject playerVisualGO, Vector3 playerVisualTargetPos,
        Tile tileSwappedWithPlayer,
        Vector2Int playerNewGridPos_AttackOrigin
    )
    {
        swapAnimationActive = true;
        float elapsedTime = 0f;

        ClearPlayerAttackAreaVisuals();

        Vector3 tile1StartPos = tile1GO.transform.position;
        Vector3 tile2StartPos = tile2GO.transform.position;
        Vector3 playerVisualStartPos = playerVisualGO != null ? playerVisualGO.transform.position : Vector3.zero;

        SpriteRenderer tile1Sr = tile1GO.GetComponent<SpriteRenderer>();
        SpriteRenderer tile2Sr = tile2GO.GetComponent<SpriteRenderer>();
        SpriteRenderer playerSr = playerVisualGO?.GetComponent<SpriteRenderer>(); // Use ?. for safety if playerVisualGO could be null

        int tile1OriginalOrder = tile1Sr?.sortingOrder ?? 0;
        int tile2OriginalOrder = tile2Sr?.sortingOrder ?? 0;
        int playerOriginalOrder = playerSr?.sortingOrder ?? 0;

        
        // Bring to front during animation
        if (playerSr != null) playerSr.sortingOrder = 102; 
        if (tile1Sr != null) tile1Sr.sortingOrder = 101;
        if (tile2Sr != null) tile2Sr.sortingOrder = 100;

        //! goota remove this after animations
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
                    GameObject outlineGO = Instantiate(playerAttackOutlinePrefab, worldPos, Quaternion.identity, this.transform);
                    SpriteRenderer olSr = outlineGO.GetComponent<SpriteRenderer>();
                    if (olSr != null) olSr.color = new Color(attackHighlightColor.r, attackHighlightColor.g, attackHighlightColor.b, 0.5f); // More transparent
                    tempPreviewOutlines.Add(outlineGO);
                }
            }
        }

        // SWAP LOGIC
        while (elapsedTime < swapAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / swapAnimationDuration);

            // t = 1f - (1f - t) * (1f - t); // ease-out
            t = t * t * (3f - 2f * t);       // ease-in-out 

            tile1GO.transform.position = Vector3.Lerp(tile1StartPos, tile1FinalPos, t);
            tile2GO.transform.position = Vector3.Lerp(tile2StartPos, tile2FinalPos, t);

            if (playerVisualGO != null)
                playerVisualGO.transform.position = Vector3.Lerp(playerVisualStartPos, playerVisualTargetPos, t);
            yield return null;
        }

        tile1GO.transform.position = tile1FinalPos;
        tile2GO.transform.position = tile2FinalPos;
        if (playerVisualGO != null) playerVisualGO.transform.position = playerVisualTargetPos;

        // Restore original sorting orders
        if (tile1Sr != null) tile1Sr.sortingOrder = tile1OriginalOrder;
        if (tile2Sr != null) tile2Sr.sortingOrder = tile2OriginalOrder;
        if (playerSr != null) playerSr.sortingOrder = playerOriginalOrder;

        // 1. Player Performs Logical Attack from NEW position
        List<Tile> tilesHitByPlayer = PerformPlayerAttackAndGetHitTiles(playerNewGridPos_AttackOrigin);
        StartCoroutine(FlashHitTiles(tilesHitByPlayer, enemyHitFlashColor, enemyHitFlashDuration));

        //! remove this too 
        foreach (GameObject outline in tempPreviewOutlines) Destroy(outline);
        tempPreviewOutlines.Clear();

        // Check if game ended after attack
        if (isGameOver)
        {
            swapAnimationActive = false;
            UpdateAllHighlightDisplays();
            yield break; // exit
        }

        // 2. interaction with the swapped tile
        bool canInteract = true;
        if (tileSwappedWithPlayer is EnemyTile swappedEnemy)
        {
            if (swappedEnemy.IsDefeated())
            {
                canInteract = false; // Don't interact with a just-defeated enemy
                Debug.Log($"Player swapped with {swappedEnemy.gameObject.name}, but it was just defeated. No OnPlayerEnter interaction.");
            }
        }

        if (canInteract)
            tileSwappedWithPlayer.OnPlayerEnter(playerController);

        // 3. enemy turn
        if (!isGameOver)
            ProcessEnemyActions();

        // 4. exit
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

        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        List<Color> originalColors = new List<Color>();

        foreach (Tile tile in tilesToFlash)
        {
            if (tile != null && !(tile is EnemyTile))
            {
                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    srs.Add(sr);
                    originalColors.Add(sr.color);
                    sr.color = flashColor;
                }
            }
        }

        if (srs.Count > 0)
        {
            yield return new WaitForSeconds(duration);
            for (int i = 0; i < srs.Count; i++)
            {
                if (srs[i] != null) srs[i].color = originalColors[i];
            }
        }
    }

    #endregion

    #region Utility & Helper Methods
    // =====================================================================
    // Utility & Helper Methods
    // =====================================================================
    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float worldX = (gridPos.x - (gridSize - 1) / 2.0f) * tileSize;
        float worldY = (gridPos.y - (gridSize - 1) / 2.0f) * tileSize;
        return new Vector3(worldX, worldY, 0);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float centerX_grid = (gridSize - 1) / 2.0f;
        float centerY_grid = (gridSize - 1) / 2.0f;

        int gridX = Mathf.RoundToInt(worldPos.x / tileSize + centerX_grid);
        int gridY = Mathf.RoundToInt(worldPos.y / tileSize + centerY_grid);
        return new Vector2Int(gridX, gridY);
    }

    public bool InBounds(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridSize &&
               gridPos.y >= 0 && gridPos.y < gridSize;
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
            case TileType.Enemy: return enemyTilePrefab;
            case TileType.HealthPickup: return healthPickupPrefab;
            case TileType.Player: return playerTilePrefab;
            case TileType.Environment: return Random.value > 0.5f ? rockTilePrefab : bushTilePrefab; //! fix later
            default: return emptyTilePrefab;
        }
    }

    #endregion
}