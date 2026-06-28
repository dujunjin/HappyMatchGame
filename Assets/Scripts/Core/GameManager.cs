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

    /// <summary>Phase E: pooled particle effects (clears + specials).</summary>
    public VfxSystem Vfx { get; private set; }

    /// <summary>Phase E: christmas-night background + snow field.</summary>
    public ChristmasBackground winterBackground { get; private set; }
    public SnowField snowField { get; private set; }

    /// <summary>Phase E: procedural SFX via 4 mix groups.</summary>
    public AudioManager Audio { get; private set; }

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

        // Phase E: a dark semi-transparent backdrop behind the board so the
        // (busy, bright) Christmas scene doesn't obscure the pieces.
        CreateBoardBackdrop();

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

        // Phase E: environment + particles (MonoBehaviours; created via AddComponent).
        winterBackground = FindObjectOfType<ChristmasBackground>();
        if (winterBackground == null)
        {
            winterBackground = new GameObject("ChristmasBackground").AddComponent<ChristmasBackground>();
        }
        Vfx = FindObjectOfType<VfxSystem>();
        if (Vfx == null)
        {
            Vfx = new GameObject("VfxSystem").AddComponent<VfxSystem>();
        }
        Vfx.Init();
        snowField = FindObjectOfType<SnowField>();
        if (snowField == null)
        {
            snowField = new GameObject("SnowField").AddComponent<SnowField>();
        }
        snowField.Init();

        // Phase E: audio.
        Audio = FindObjectOfType<AudioManager>();
        if (Audio == null)
        {
            Audio = new GameObject("AudioManager").AddComponent<AudioManager>();
        }
        Audio.Init(audioCatalog);

        // Phase F: F12 screenshot helper.
        if (FindObjectOfType<ScreenshotCapture>() == null)
            new GameObject("ScreenshotCapture").AddComponent<ScreenshotCapture>();

        // Wire dependencies
        matchDetector.Init(boardController);
        swapHandler.Init(boardController, this);
        gravitySystem.Init(boardController);
        cascadeManager.Init(boardController, matchDetector, swapHandler, gravitySystem, specialFactory, this);
        specialFactory.Init(boardController, this);
        suitcaseManager.Init(boardController);
        boardInput.Init(boardController, swapHandler, this);
        gameUI.Init(this);

        // Order matters: Flow.Init sets the logical counters first, then
        // TargetPresentation.Init reads them to seed the display count and
        // refreshes the TopBar.
        Flow.Init(levelConfig, this);
        targetPresentation.Init(this, Flow);

        SetState(GameState.Idle);

        string[] commandLine = System.Environment.GetCommandLineArgs();
        if (AcceptanceDirector.ShouldRun(commandLine))
        {
            AcceptanceDirector director = gameObject.AddComponent<AcceptanceDirector>();
            director.Init(this);
        }
    }

    public Sprite GetSpriteForType(ElementType type, GameConfig.SpecialType special = GameConfig.SpecialType.None, GameConfig.RocketDir rocketDir = GameConfig.RocketDir.Horizontal)
    {
        return _theme.GetSpriteForType(type, special, rocketDir);
    }

    // Delegated state/counter APIs (callers continue to use GameManager).
    public void SetState(GameState newState) => Flow.SetState(newState);
    public void DecreaseStep() => Flow.DecreaseStep();
    public void DecreaseSuitcase(int count = 1) => Flow.DecreaseSuitcase(count);

    /// <summary>
    /// Refresh the TopBar from the DISPLAY suitcase count (lags logical during
    /// flyer flight) and the live step count. Single source of truth for the
    /// top bar so Flow and TargetPresentation never desync it.
    /// </summary>
    public void RefreshTopBar()
    {
        int disp = targetPresentation != null ? targetPresentation.displayCount : Flow.RemainingSuitcases;
        gameUI?.UpdateTopBar(disp, Flow.RemainingSteps);
    }

    public void ShowResult(bool won) => gameUI?.ShowResult(won);

    /// <summary>Play the Phase D win sequence (replaces the plain win dialog).</summary>
    public void PlayWinSequence() => gameUI?.PlayWinSequence(this);

    /// <summary>
    /// Called by BoardPresenter after a cascade with no in-flight flyers, and
    /// by TargetPresentation when the last flyer lands. Resolves the pending
    /// end-game (victory barrier) and, if play continues, runs the dead-board
    /// check (shuffle stays in Settling until done).
    /// </summary>
    public void OnSettleComplete()
    {
        Flow.CheckEndGame();
        if (Flow.State == GameState.GameOver) return;

        if (deadBoardDetector != null && !deadBoardDetector.HasLegalSwap(boardController))
        {
            Flow.SetState(GameState.Settling);
            StartCoroutine(ShuffleThenSettle());
        }
        else
        {
            Flow.SetState(GameState.Idle);
        }
    }

    private IEnumerator ShuffleThenSettle()
    {
        yield return deadBoardDetector.Shuffle(boardController, levelConfig, this);
        Flow.SetState(GameState.Idle);
    }

    /// <summary>
    /// A frosted-glass panel sized to the board, at sortingOrder -1
    /// (behind the cells at 1, in front of the Christmas scene at &lt;0).
    /// Uses CreateRectGlassPanel to generate the texture at the board's
    /// exact aspect ratio — this prevents rounded corners from being
    /// stretched into ovals by non-uniform scaling.
    /// Matches HTML: background rgba(255,255,255,0.12), border rgba(255,255,255,0.18),
    /// border-radius 20px, padding 8px.
    /// </summary>
    private void CreateBoardBackdrop()
    {
        if (boardController == null) return;
        float spacing = boardController.cellSize + boardController.cellGap;
        int rows = boardController.Rows, cols = boardController.Cols;
        Vector3 o = boardController.boardOrigin;
        float xMin = o.x - spacing * 0.5f;
        float xMax = o.x + (cols - 1) * spacing + spacing * 0.5f;
        float yMax = o.y + spacing * 0.5f;
        float yMin = o.y - (rows - 1) * spacing - spacing * 0.5f;
        Vector3 center = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
        float w = (xMax - xMin) + 0.5f;
        float h = (yMax - yMin) + 0.5f;

        GameObject go = new GameObject("BoardBackdrop");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

        // Generate the glass texture at the BOARD'S EXACT ASPECT RATIO.
        // This is the key fix: a square texture stretched non-uniformly
        // turns round corners into ovals. A rectangular texture at the
        // right proportions keeps corners perfectly round.
        const float ppu = 100f;
        int texW = Mathf.Max(128, Mathf.RoundToInt(w * ppu));
        int texH = Mathf.Max(128, Mathf.RoundToInt(h * ppu));
        // HTML border-radius: 20px on ~340px board → ~6% ratio.
        // Scale to texture: texH * 0.06, minimum 36px for visible rounding.
        float texCornerRadius = Mathf.Max(36f, texH * 0.06f);

        sr.sprite = GlassPanelTexture.CreateRectGlassPanel(
            texW, texH, texCornerRadius,
            new Color(0.055f, 0.19f, 0.42f, 0.58f),
            0.42f,
            0.22f,
            0.014f
        );
        sr.sortingOrder = -1;
        sr.color = Color.white;
        go.transform.SetParent(boardController.transform, false);
        go.transform.position = center;
        // Sliced draw mode + explicit size = proper 9-slice rendering
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(w, h);
        go.transform.localScale = Vector3.one;
    }

    private BoardController FindOrCreateBoardController()    {
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
