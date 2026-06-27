using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central game manager - thin orchestrator that assembles systems in Start
/// and delegates state/step/suitcase logic to GameFlowController, board
/// presentation to BoardPresenter, and target counting to
/// TargetPresentation. Visual/audio assets are sourced from VisualTheme /
/// AudioCatalog ScriptableObject containers (programmatic defaults work with
/// zero imported art).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level / Theme (leave null for programmatic defaults)")]
    public LevelConfig levelConfig;
    public VisualTheme visualTheme;
    public AudioCatalog audioCatalog;

    [Header("Systems")]
    public BoardController boardController;
    public MatchDetector matchDetector;
    public SwapHandler swapHandler;
    public GravitySystem gravitySystem;
    public CascadeManager cascadeManager;
    public SpecialFactory specialFactory;
    public SuitcaseManager suitcaseManager;
    public BoardInput boardInput;
    public GameUI gameUI;

    /// <summary>State machine + step/suitcase authority. Delegated from here.</summary>
    public GameFlowController Flow { get; private set; }

    /// <summary>Animation/cascade wrapper + dead-board shuffle hook.</summary>
    public BoardPresenter boardPresenter { get; private set; }

    /// <summary>Suitcase-clear routing stub (Phase C will animate flyers).</summary>
    public TargetPresentation targetPresentation { get; private set; }

    /// <summary>Dead-board detection + shuffle.</summary>
    public DeadBoardDetector deadBoardDetector { get; private set; }

    /// <summary>Special×special combos (rocket×rocket/bomb/propeller).</summary>
    public SpecialComboHandler comboHandler { get; private set; }

    // Kept for callers that read state via GameManager; delegates to Flow.
    public GameState State => Flow != null ? Flow.State : GameState.Idle;
    public int RemainingSteps => Flow != null ? Flow.RemainingSteps : 0;
    public int RemainingSuitcases => Flow != null ? Flow.RemainingSuitcases : 0;

    private VisualTheme _theme;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Constructed early so the State/Remaining accessors above never
        // null-ref before Start runs. Init (config + UI) happens in Start.
        Flow = new GameFlowController();
    }

    private void Start()
    {
        // Resolve config + theme (fall back to programmatic defaults).
        if (levelConfig == null) levelConfig = LevelConfig.Default;
        _theme = visualTheme != null ? visualTheme : VisualTheme.Default;
        // AudioCatalog is built now but not actually played until Phase E.
        if (audioCatalog == null) audioCatalog = AudioCatalog.Default;

        // Board: generate the deterministic layout, then instantiate it.
        boardController = FindOrCreateBoardController();
        boardController.gameManager = this;
        CellData[,] grid = BoardGenerator.Generate(levelConfig);
        boardController.InitializeBoard(grid);

        // Systems
        matchDetector = new MatchDetector();
        swapHandler = new SwapHandler();
        gravitySystem = new GravitySystem();
        cascadeManager = new CascadeManager();
        specialFactory = new SpecialFactory();
        suitcaseManager = new SuitcaseManager();
        boardInput = FindOrCreateBoardInput();
        gameUI = FindOrCreate<GameUI>("GameUI");

        // Presentation layer
        targetPresentation = new TargetPresentation();
        deadBoardDetector = new DeadBoardDetector(matchDetector, this);
        comboHandler = new SpecialComboHandler(boardController, this);
        boardPresenter = new BoardPresenter(
            this, swapHandler, cascadeManager, deadBoardDetector,
            boardController, Flow, levelConfig);

        // Wire dependencies
        matchDetector.Init(boardController);
        swapHandler.Init(boardController, this);
        gravitySystem.Init(boardController);
        cascadeManager.Init(boardController, matchDetector, swapHandler, gravitySystem, specialFactory, this);
        specialFactory.Init(boardController, this);
        suitcaseManager.Init(boardController);
        targetPresentation.Init(this, Flow);
        boardInput.Init(boardController, swapHandler, this);
        gameUI.Init(this);

        // Initialize flow (counters + UI). Layout already carries suitcases,
        // so SuitcaseManager.PlaceInitialSuitcases() is intentionally skipped.
        Flow.Init(levelConfig, gameUI);

        SetState(GameState.Idle);
    }

    public Sprite GetSpriteForType(ElementType type, GameConfig.SpecialType special = GameConfig.SpecialType.None, GameConfig.RocketDir rocketDir = GameConfig.RocketDir.Horizontal)
    {
        return _theme.GetSpriteForType(type, special, rocketDir);
    }

    // Delegated state/counter APIs (callers continue to use GameManager).
    public void SetState(GameState newState) => Flow.SetState(newState);
    public void DecreaseStep() => Flow.DecreaseStep();
    public void DecreaseSuitcase(int count = 1) => Flow.DecreaseSuitcase(count);

    private BoardController FindOrCreateBoardController()
    {
        var bc = FindObjectOfType<BoardController>();
        if (bc == null)
        {
            GameObject go = new GameObject("BoardController");
            bc = go.AddComponent<BoardController>();
            bc.gameManager = this;
        }
        return bc;
    }

    private T FindOrCreate<T>(string name) where T : MonoBehaviour
    {
        var comp = FindObjectOfType<T>();
        if (comp == null)
        {
            GameObject go = new GameObject(name);
            comp = go.AddComponent<T>();
        }
        return comp;
    }

    private BoardInput FindOrCreateBoardInput()
    {
        var comp = FindObjectOfType<BoardInput>();
        if (comp == null)
        {
            GameObject go = new GameObject("BoardInput");
            comp = go.AddComponent<BoardInput>();
        }
        return comp;
    }

    private void Update()
    {
        // Debug keys for special item testing
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Create a horizontal rocket at (3,4)
            StartCoroutine(specialFactory.CreateRocketAt(3, 4, GameConfig.RocketDir.Horizontal));
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Create a bomb at (4,4)
            StartCoroutine(specialFactory.CreateBombAt(4, 4));
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Create a propeller at (5,4)
            StartCoroutine(specialFactory.CreatePropellerAt(5, 4));
        }
    }
}
