using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central game manager - state machine driving the match-3 game loop.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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

    public GameState State { get; private set; } = GameState.Idle;
    public int RemainingSuitcases { get; private set; }
    public int RemainingSteps { get; private set; }

    private Dictionary<Color, Sprite> _elementSprites = new Dictionary<Color, Sprite>();
    private Dictionary<(Color, string), Sprite> _specialSprites = new Dictionary<(Color, string), Sprite>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Generate sprites
        GenerateSprites();

        // Initialize systems
        boardController = FindOrCreateBoardController();
        boardController.gameManager = this;
        // Populate the board now (before placing suitcases) so cells exist.
        boardController.InitializeBoard();
        matchDetector = new MatchDetector();
        swapHandler = new SwapHandler();
        gravitySystem = new GravitySystem();
        cascadeManager = new CascadeManager();
        specialFactory = new SpecialFactory();
        suitcaseManager = new SuitcaseManager();
        boardInput = FindOrCreateBoardInput();
        gameUI = FindOrCreate<GameUI>("GameUI");

        // Wire dependencies
        matchDetector.Init(boardController);
        swapHandler.Init(boardController, this);
        gravitySystem.Init(boardController);
        cascadeManager.Init(boardController, matchDetector, swapHandler, gravitySystem, specialFactory, this);
        specialFactory.Init(boardController, this);
        suitcaseManager.Init(boardController);
        boardInput.Init(boardController, swapHandler, this);
        gameUI.Init(this);

        // Start game
        RemainingSuitcases = GameConfig.InitialSuitcaseCount;
        RemainingSteps = GameConfig.MaxSteps;
        gameUI.UpdateTopBar(RemainingSuitcases, RemainingSteps);

        // Place initial suitcases
        suitcaseManager.PlaceInitialSuitcases();

        SetState(GameState.Idle);
    }

    private void GenerateSprites()
    {
        _elementSprites[GameConfig.ElementColor_Red] = SpriteGenerator.CreateCircleSprite(GameConfig.ElementColor_Red);
        _elementSprites[GameConfig.ElementColor_Blue] = SpriteGenerator.CreateCircleSprite(GameConfig.ElementColor_Blue);
        _elementSprites[GameConfig.ElementColor_Yellow] = SpriteGenerator.CreateCircleSprite(GameConfig.ElementColor_Yellow);
        _elementSprites[GameConfig.ElementColor_Green] = SpriteGenerator.CreateCircleSprite(GameConfig.ElementColor_Green);
        _elementSprites[GameConfig.ElementColor_Suitcase] = SpriteGenerator.CreateSuitcaseSprite(GameConfig.ElementColor_Suitcase);

        _specialSprites[(GameConfig.ElementColor_Red, "rocket")] = SpriteGenerator.CreateRocketSprite(GameConfig.ElementColor_Red);
        _specialSprites[(GameConfig.ElementColor_Blue, "rocket")] = SpriteGenerator.CreateRocketSprite(GameConfig.ElementColor_Blue);
        _specialSprites[(GameConfig.ElementColor_Yellow, "rocket")] = SpriteGenerator.CreateRocketSprite(GameConfig.ElementColor_Yellow);
        _specialSprites[(GameConfig.ElementColor_Green, "rocket")] = SpriteGenerator.CreateRocketSprite(GameConfig.ElementColor_Green);
        _specialSprites[(GameConfig.ElementColor_Red, "bomb")] = SpriteGenerator.CreateBombSprite(GameConfig.ElementColor_Red);
        _specialSprites[(GameConfig.ElementColor_Blue, "bomb")] = SpriteGenerator.CreateBombSprite(GameConfig.ElementColor_Blue);
        _specialSprites[(GameConfig.ElementColor_Yellow, "bomb")] = SpriteGenerator.CreateBombSprite(GameConfig.ElementColor_Yellow);
        _specialSprites[(GameConfig.ElementColor_Green, "bomb")] = SpriteGenerator.CreateBombSprite(GameConfig.ElementColor_Green);
    }

    public Sprite GetSpriteForType(ElementType type, GameConfig.SpecialType special = GameConfig.SpecialType.None, GameConfig.RocketDir rocketDir = GameConfig.RocketDir.Horizontal)
    {
        if (type == ElementType.Suitcase)
            return _elementSprites[GameConfig.ElementColor_Suitcase];

        if (special == GameConfig.SpecialType.Rocket)
        {
            var key = (GetColorForType(type), "rocket");
            if (_specialSprites.ContainsKey(key))
                return _specialSprites[key];
        }

        if (special == GameConfig.SpecialType.Bomb)
        {
            var key = (GetColorForType(type), "bomb");
            if (_specialSprites.ContainsKey(key))
                return _specialSprites[key];
        }

        return _elementSprites[GetColorForType(type)];
    }

    private Color GetColorForType(ElementType type)
    {
        return type switch
        {
            ElementType.Red => GameConfig.ElementColor_Red,
            ElementType.Blue => GameConfig.ElementColor_Blue,
            ElementType.Yellow => GameConfig.ElementColor_Yellow,
            ElementType.Green => GameConfig.ElementColor_Green,
            _ => Color.white
        };
    }

    public void SetState(GameState newState)
    {
        State = newState;
    }

    public void DecreaseStep()
    {
        RemainingSteps--;
        gameUI.UpdateTopBar(RemainingSuitcases, RemainingSteps);

        if (RemainingSteps <= 0 && RemainingSuitcases > 0)
        {
            SetState(GameState.GameOver);
            gameUI.ShowResult(false);
        }
    }

    public void DecreaseSuitcase(int count = 1)
    {
        RemainingSuitcases = Mathf.Max(0, RemainingSuitcases - count);
        gameUI.UpdateTopBar(RemainingSuitcases, RemainingSteps);

        if (RemainingSuitcases <= 0)
        {
            SetState(GameState.GameOver);
            gameUI.ShowResult(true);
        }
    }

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
    }
}
