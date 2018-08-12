using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour {

  private enum Screen {
    START,
    GAME,
    GAME_OVER
  }

  private static readonly int PIECE_VALUE = 10;
  private static readonly int CLEARED_BLOCK_VALUE = 100;

  private static readonly string HIGH_SCORE_PREF = "DefragHighScore";

  public SoundManager soundManager;

  public Block blockPrefab;
  public SpriteRenderer deadBlockPrefab;
  public CanvasGroup startScreen;
  public CanvasGroup gameOverScreen;
  public GameObject board;
  public Player player;
  public GhostPiece ghostPiece;
  public PreviewPiece previewPiece;
  public PowerupPiece powerupPiece;
  public RectTransform nextLevelLine;

  public Sprite redPlayer;
  public Sprite greenPlayer;
  
  public Text playerScoreText;
  public Text highScoreText;
  public Text newHighScoreText;
  public Text nextLevelText;

  private Block[] blocks;
  private SpriteRenderer[] deadBlocks;

  private int playerScore;
  private int highScore;

  private int linesInLevel = 4;
  private int dropFillMarker;
  private int playerIndex = 0;
  private List<Piece> pieces = new List<Piece>();
  private System.Random r = new System.Random();

  private float dropTimerLimit = 2;
  private float dropTimer;
  private float suckTimer;
  private float deleteTimerLimit = 16;
  private float deleteTimer;
  private float powerupTimerLimit = 12.5f;
  private float powerupTimerRandOffset = 5f;
  private float powerupTimer;

  private float debounceTimer = 0;

  private float freezeTimerAmount = 5;
  private float freezeTimer;

  private float powerupExpiryLimit = 3;
  private float powerupExpiry = 0;

  private Piece flaggedPiece;

  private bool droppingBoard;
  private bool powerupActive;

  private Screen screen;

  private System.Collections.IEnumerator startscreenGameplay;

  private int level;

  public void Awake() {
    highScore = PlayerPrefs.GetInt(HIGH_SCORE_PREF, 0);
    dropFillMarker = GameUtil.BOARD_WIDTH * linesInLevel;
    RefreshScoreText();

    blocks = new Block[GameUtil.BOARD_WIDTH * GameUtil.BOARD_HEIGHT];
    for (int i = 0; i < GameUtil.BOARD_WIDTH; i++) {
      for (int j = 0; j < GameUtil.BOARD_HEIGHT; j++) {
        blocks[ToIndex(i, j)] = Instantiate(
            blockPrefab, GameUtil.ToBoardSpace(i, j), Quaternion.identity, board.transform);
        SetBlockPiece(ToIndex(i, j), null);
      }
    }
    deadBlocks = new SpriteRenderer[GameUtil.BOARD_WIDTH * GameUtil.DEAD_BLOCK_ROWS];
    for (int i = 0; i < GameUtil.BOARD_WIDTH; i++) {
      for (int j = 0; j < GameUtil.DEAD_BLOCK_ROWS; j++) {
        deadBlocks[ToIndex(i, j)] = Instantiate(
            deadBlockPrefab, GameUtil.ToBoardSpace(GameUtil.BOARD_WIDTH - 1 - i, -j - 1), Quaternion.identity, board.transform);
        deadBlocks[ToIndex(i, j)].color = Color.black;
      }
    }

    powerupPiece.SetPowerup(GameUtil.Powerup.NONE, 0);
    playerIndex = 0;
  }

  public void Start() {
    LoadStartScreen();
  }

  private void LoadStartScreen() {
    screen = Screen.START;
    ShowStartScreen();
    HideGameOverScreen();
    startscreenGameplay = DoStartScreenGameplay();
    StartCoroutine(startscreenGameplay);
  }

  private System.Collections.IEnumerator DoStartScreenGameplay() {
    if (pieces.Count > 0) {
      yield break;
    }
    linesInLevel = 4;
    dropFillMarker = 4 * GameUtil.BOARD_WIDTH;
    while (true) {
      linesInLevel = 4;
      dropFillMarker = 4 * GameUtil.BOARD_WIDTH;
      playerIndex = 0;
      AddPiece(new Piece(0, 4, Color.magenta));
      yield return new WaitForSeconds(.5f);
      AddPiece(new Piece(4, 3, Color.green));
      yield return new WaitForSeconds(.1f);
      AddPiece(new Piece(7, 2, Color.blue));
      yield return new WaitForSeconds(.6f);
      AddPiece(new Piece(9, 4, Color.cyan));
      yield return new WaitForSeconds(.3f);
      AddPiece(new Piece(13, 3, Color.yellow));
      yield return new WaitForSeconds(.3f);
      AddPiece(new Piece(16, 3, Color.green));
      yield return new WaitForSeconds(.3f);
      AddPiece(new Piece(19, 2, Color.magenta));
      yield return new WaitForSeconds(.1f);
      AddPiece(new Piece(24, 3, Color.blue));
      yield return new WaitForSeconds(.1f);
      playerIndex = 0;
      yield return new WaitForSeconds(1f);
      soundManager.audioSource.PlayOneShot(soundManager.move);
      playerIndex = 6;
      yield return new WaitForSeconds(0.5f);
      soundManager.audioSource.PlayOneShot(soundManager.move);
      playerIndex = 12;
      yield return new WaitForSeconds(0.5f);
      soundManager.audioSource.PlayOneShot(soundManager.move);
      playerIndex = 18;
      yield return new WaitForSeconds(0.5f);
      soundManager.audioSource.PlayOneShot(soundManager.move);
      playerIndex = 24;
      yield return new WaitForSeconds(0.5f);
      player.gameObject.SetActive(false);
      PickUpOrDrop();
      yield return new WaitForSeconds(0.5f);
      soundManager.audioSource.PlayOneShot(soundManager.move);
      ghostPiece.Adjust(-1);
      playerIndex = 23;
      yield return new WaitForSeconds(0.5f);
      soundManager.audioSource.PlayOneShot(soundManager.move);
      ghostPiece.Adjust(-1);
      playerIndex = 22;
      yield return new WaitForSeconds(0.5f);
      soundManager.audioSource.PlayOneShot(soundManager.move);
      ghostPiece.Adjust(-1);
      playerIndex = 21;
      yield return new WaitForSeconds(1.0f);
      player.gameObject.SetActive(true);
      PickUpOrDrop();
      for (int i = 0; i < 24; i++) {
        blocks[i].GetPiece().Locked = true;
      }
      yield return new WaitForSeconds(0.25f);
      yield return AnimateDrop();
      linesInLevel = 4;
      dropFillMarker = 4 * GameUtil.BOARD_WIDTH;
      yield return new WaitForSeconds(1f);
    }
  }

  private void LoadGame() {
    if (startscreenGameplay != null) {
      StopCoroutine(startscreenGameplay);
      startscreenGameplay = null;
    }
    screen = Screen.GAME;
    playerScore = 0;
    pieces.Clear();
    for (int i = 0; i < blocks.Length; i++) {
      blocks[i].SetPiece(null);
    }
    ghostPiece.SetPiece(null);
    previewPiece.SetPiece(null);
    powerupPiece.SetPowerup(GameUtil.Powerup.NONE, 0);
    playerIndex = 0;
    dropTimer = dropTimerLimit;
    deleteTimer = deleteTimerLimit;
    powerupTimer = GetNewPowerUpTimer();
    player.gameObject.SetActive(true);
    player.image.sprite = greenPlayer;
    level = 0;
    SetNextLevel();

    HideStartScreen();
    HideGameOverScreen();
  }

  private float GetNewPowerUpTimer() {
    return powerupTimerLimit - (((float) r.NextDouble()) * powerupTimerRandOffset);
  }

  private void LoadGameOver() {
    screen = Screen.GAME_OVER;
    HideStartScreen();
    ShowGameOverScreen();
    StartCoroutine(GameOverScript());
  }

  public void Update() {
    if (screen == Screen.START) {
      RunStartScreen();
      return;
    } else if (screen == Screen.GAME) {
      RunGame();
    } else if (screen == Screen.GAME_OVER) {
      // Just kidding, this doesn't have any input right now.
    }
  }

  private void RunStartScreen() {
    if (Input.GetKeyDown(KeyCode.Space)) {
      LoadGame();
      return;
    }
    DrawBoard();
  }

  private System.Collections.IEnumerator GameOverScript() {
    float timePassed = 0;
    yield return null;
    while (timePassed < 5) {
      timePassed += Time.deltaTime;
      if (playerScore == highScore && (Mathf.FloorToInt(timePassed) % 2) == 0) {
        newHighScoreText.gameObject.SetActive(true);
      } else {
        newHighScoreText.gameObject.SetActive(false);
      }
      yield return null;
    }
    LoadStartScreen();
  }

  private void RunGame() {
    GameUtil.Powerup grabbedPowerup = GameUtil.Powerup.NONE;
    if (!DropsPaused()) {
      dropTimer = dropTimer - Time.deltaTime;
      deleteTimer = deleteTimer - Time.deltaTime;
    }
    suckTimer = suckTimer - Time.deltaTime;
    powerupTimer = powerupTimer - Time.deltaTime;
    powerupExpiry = powerupExpiry - Time.deltaTime;
    freezeTimer = freezeTimer - Time.deltaTime;
    debounceTimer = debounceTimer + Time.deltaTime;

    if (powerupTimer < 0) {
      powerupTimer = GetNewPowerUpTimer();
      powerupExpiry = powerupExpiryLimit;
      GameUtil.Powerup choice = (GameUtil.Powerup) (r.Next(GameUtil.NumPowerups()) + 1);
      int index = r.Next(GameUtil.BOARD_SIZE);
      powerupPiece.SetPowerup(choice, index);
    }

    if (powerupExpiry < 0 && powerupPiece.GetPowerup() != GameUtil.Powerup.NONE) {
      powerupPiece.SetPowerup(GameUtil.Powerup.NONE, 0);
    }

    if (dropTimer < 2 && previewPiece.GetCurrentPiece() == null) {
      previewPiece.SetPiece(CreateRandomPiece());
    }
    if (deleteTimer < 4 && flaggedPiece == null) {
      if (pieces.Count == 0) {
        deleteTimer = deleteTimerLimit;
      } else {
        Piece choice = pieces[r.Next(pieces.Count)];
        if (!choice.Locked) {
          soundManager.audioSource.PlayOneShot(soundManager.preDelete);
          flaggedPiece = choice;
          flaggedPiece.Flagged = true;
        }
      }
    }

    int presses = 0;
    while (debounceTimer - .05f > .25f) {
      presses++;
      debounceTimer -= .05f;
    }

    if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) {
      debounceTimer = 0;
      MovePlayer(-GameUtil.BOARD_WIDTH);
      ghostPiece.Adjust(-GameUtil.BOARD_WIDTH);
    } else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) {
      MovePlayer(-presses * GameUtil.BOARD_WIDTH);
      ghostPiece.Adjust(-presses * GameUtil.BOARD_WIDTH);
    }
    if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) {
      debounceTimer = 0;
      MovePlayer(GameUtil.BOARD_WIDTH);
      ghostPiece.Adjust(GameUtil.BOARD_WIDTH);
    } else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) {
      MovePlayer(presses * GameUtil.BOARD_WIDTH);
      ghostPiece.Adjust(presses * GameUtil.BOARD_WIDTH);
    }
    if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) {
      debounceTimer = 0;
      MovePlayer(1);
      ghostPiece.Adjust(1);
    } else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) {
      MovePlayer(presses);
      ghostPiece.Adjust(presses);
    }
    if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) {
      debounceTimer = 0;
      MovePlayer(-1);
      ghostPiece.Adjust(-1);
    } else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) {
      MovePlayer(-presses);
      ghostPiece.Adjust(-presses);
    }

    if (Input.GetKeyDown(KeyCode.Space)) {
      if (!TryGrabPowerup(out grabbedPowerup) && !powerupActive) {
        PickUpOrDrop();
        // Abandoned mechanic!
        // if (ghostPiece.GetCurrentPiece() == null) {
          // SuckInPieces(true);
        // }
      }
    } else if (Input.GetKey(KeyCode.Space) && ghostPiece.GetCurrentPiece() == null) {
      // SuckInPieces(false);
    }
    if (ghostPiece.GetCurrentPiece() != null) {
      player.gameObject.SetActive(false);
      ghostPiece.gameObject.SetActive(true);
    } else {
      player.gameObject.SetActive(true);
      ghostPiece.gameObject.SetActive(false);
    }
    DrawBoard();
    if (!droppingBoard) {
      MaybeDropBoard();
    }
    if (grabbedPowerup != GameUtil.Powerup.NONE) {
      RunPowerup(grabbedPowerup);
    }

    if (dropTimer < 0 && !DropsPaused()) {
      dropTimer = dropTimerLimit;
      if (!AddPiece(previewPiece.GetCurrentPiece())) {
        LoadGameOver();
      } else {
        previewPiece.SetPiece(null);
      }
    }

    if (deleteTimer < 0 && !DropsPaused()) {
      deleteTimer = deleteTimerLimit;
      if (flaggedPiece == null) {
        // Anything?
      } else if (flaggedPiece.Locked) {
        // Lucky you!
        flaggedPiece.Flagged = false;
        flaggedPiece = null;
      } else {
        soundManager.audioSource.PlayOneShot(soundManager.delete);
        DeletePiece(flaggedPiece);
        flaggedPiece = null;
      }
    }
  }

  private void RunPowerup(GameUtil.Powerup p) {
    StartPowerup();

    if (p == GameUtil.Powerup.FREEZE) {
      soundManager.audioSource.PlayOneShot(soundManager.freeze);
      freezeTimer = freezeTimerAmount;
      EndPowerup();
    } else if (p == GameUtil.Powerup.DELETE) {
      StartCoroutine(DeletePowerup());
    } else if (p == GameUtil.Powerup.COMPRESS) {
      soundManager.audioSource.PlayOneShot(soundManager.compress);
      StartCoroutine(CompressPowerup());
    } else {
      EndPowerup();
    }
  }

  private void StartPowerup() {
    powerupActive = true;
    ghostPiece.SetPiece(null);
    player.image.sprite = redPlayer;
  }

  private void EndPowerup() {
    powerupActive = false;
    player.image.sprite = greenPlayer;
  }

  private System.Collections.IEnumerator DeletePowerup() {
    HashSet<Piece> piecesSet = new HashSet<Piece>();
    for (int i = GameUtil.BOARD_SIZE - 1; i >= 0 && piecesSet.Count < 4; i--) {
      if (blocks[i].GetPiece() != null) {
        piecesSet.Add(blocks[i].GetPiece());
      }
    }
    List<Piece> pieces = new List<Piece>(piecesSet);
    float time = 0;
    while (time < 1.5) {
      time += Time.deltaTime;
      if (pieces.Count > 0 && time > 0f && !pieces[0].Flagged) {
        soundManager.audioSource.PlayOneShot(soundManager.preDelete);
        pieces[0].Flagged = true;
      }
      if (pieces.Count > 1 && time > 0.1f && !pieces[1].Flagged) {
        soundManager.audioSource.PlayOneShot(soundManager.preDelete);
        pieces[1].Flagged = true;
      }
      if (pieces.Count > 2 && time > 0.3f && !pieces[2].Flagged) {
        soundManager.audioSource.PlayOneShot(soundManager.preDelete);
        pieces[2].Flagged = true;
      }
      if (pieces.Count > 3 && time > 0.8f && !pieces[3].Flagged) {
        soundManager.audioSource.PlayOneShot(soundManager.preDelete);
        pieces[3].Flagged = true;
      }
      yield return null;
    }
    for (int i = 0; i < pieces.Count; i++) {
      soundManager.audioSource.PlayOneShot(soundManager.thud);
      ShakeCamera();
      DeletePiece(pieces[i]);
    }
    EndPowerup();
  }

  private System.Collections.IEnumerator CompressPowerup() {
    HashSet<Piece> piecesSet = new HashSet<Piece>();
    for (int i = GameUtil.BOARD_SIZE - 1; i >= 0 && piecesSet.Count < 6; i--) {
      Piece p = blocks[i].GetPiece();
      if (p != null && !piecesSet.Contains(p) && !p.Locked) {
        piecesSet.Add(p);
        float time = 0;
        int index = p.Start;
        bool done = false;
        while (index > 0 && !done) {
          time = time + Time.deltaTime;
          while (time > 0.05 && index > 0) {
            index = index - 1;
            if (blocks[index].GetPiece() != null) {
              done = true;
              i = index + 1;
              break;
            }
            time = time - 0.05f;
            SuckClosePieces(index);
          }
          yield return null;
        }
      }
    }
    EndPowerup();
  }

  private void DeletePiece(Piece piece) {
    if (piece.Locked) {
      return;
    }
    if (ghostPiece.GetCurrentPiece() == piece) {
      ghostPiece.SetPiece(null);
    }
    for (int i = 0; i < piece.Size; i++) {
      // This shouldn't be necessary, but it's a good exception safety net.
      // Leave me alone, it's Ludum Dare.
      if (blocks[piece.Start + i].GetPiece() == piece) {
        blocks[piece.Start + i].SetPiece(null);
      }
    }
    pieces.Remove(piece);
  }

  private void MaybeDropBoard() {
    for (int i = 0; i < dropFillMarker; i++) {
      if (blocks[i].GetPiece() == null) {
        return;
      }
    }

    droppingBoard = true;
    for (int i = 0; i < dropFillMarker; i++) {
      blocks[i].GetPiece().Locked = true;
    }

    StartCoroutine(AnimateDrop());
  }

  private bool TryGrabPowerup(out GameUtil.Powerup grabbedPowerup) {
    if (powerupPiece.GetPowerup() == GameUtil.Powerup.NONE || playerIndex != powerupPiece.GetIndex()) {
      grabbedPowerup = GameUtil.Powerup.NONE;
      return false;
    }
    // Party time!
    grabbedPowerup = powerupPiece.GetPowerup();
    powerupPiece.SetPowerup(GameUtil.Powerup.NONE, 0);
    return true;
  }

  private System.Collections.IEnumerator AnimateDrop() {
    float grayTimer = 0;
    int clearedBlocks = 0;
    Color[] colors = new Color[GameUtil.BOARD_WIDTH * GameUtil.DEAD_BLOCK_ROWS];
    while (clearedBlocks < dropFillMarker) {
      grayTimer += Time.deltaTime;
      while (grayTimer - 0.1f > 0 && clearedBlocks < dropFillMarker) {
        soundManager.audioSource.PlayOneShot(soundManager.clearing);
        grayTimer = grayTimer - 0.1f;
        blocks[clearedBlocks].GrayOut();
        UpdateScore(CLEARED_BLOCK_VALUE);
        if (dropFillMarker - clearedBlocks - 1 < colors.Length) {
          colors[dropFillMarker - clearedBlocks - 1] = blocks[clearedBlocks].GetPiece().Color;
        }
        clearedBlocks++;
      }
      yield return null;
    }
    while (powerupActive) {
      yield return null;
    }
    int step1 = dropFillMarker == GameUtil.BOARD_WIDTH ? 0 : GameUtil.BOARD_WIDTH;
    int step2 = dropFillMarker - step1;

    yield return new WaitForSeconds(0.2f);
    for (int i = 0; i < GameUtil.BOARD_SIZE; i++) {
      Piece p = blocks[i].GetPiece();
      if (p != null) {
        if (p.Start - step1 < 0) {
          TryRemovePiece(p);
        }
        MovePiece(p, p.Start - step1);
      }
    }
    soundManager.audioSource.PlayOneShot(soundManager.delete);
    yield return new WaitForSeconds(0.3f);
    for (int i = 0; i < GameUtil.BOARD_SIZE; i++) {
      Piece p = blocks[i].GetPiece();
      if (p != null) {
        if (p.Start - step2 < 0) {
          TryRemovePiece(p);
        }
        MovePiece(p, p.Start - step2);
      }
    }
    for (int i = 0; i < colors.Length; i++) {
      deadBlocks[i].color = colors[i];
    }
    soundManager.audioSource.PlayOneShot(soundManager.thud);
    ShakeCamera();
    SetNextLevel();
    droppingBoard = false;
  }

  private bool DropsPaused() {
    return droppingBoard || powerupActive || freezeTimer > 0;
  }

  private void TryRemovePiece(Piece p) {
    if (p.Start + p.Size >= 0) {
      p.Locked = true;
    } else {
      pieces.Remove(p);
    }
  }

  private void PickUpOrDrop() {
    Piece piece = blocks[playerIndex].GetPiece();
    if (ghostPiece.GetCurrentPiece() == null) {
      if (piece != null) {
        if (piece.Locked) {
          ghostPiece.SetPiece(null);
          soundManager.audioSource.PlayOneShot(soundManager.noMove);
        } else {
          soundManager.audioSource.PlayOneShot(soundManager.pickUp);
          ghostPiece.SetPiece(piece);
        }
      } else {
        ghostPiece.SetPiece(null);
      }
    } else if (ghostPiece.GetCurrentPiece() == piece || piece == null) {
      if (TryDropPiece()) {
        soundManager.audioSource.PlayOneShot(soundManager.pickUp);
      } else {
        soundManager.audioSource.PlayOneShot(soundManager.noMove);
      }
    } else {
      soundManager.audioSource.PlayOneShot(soundManager.noMove);
    }
  }

  private bool TryDropPiece() {
    if (ghostPiece.GetCurrentPiece() == null) {
      return false;
    }

    int index = ghostPiece.GetTargetIndex();
    for (int i = 0; i < ghostPiece.GetCurrentPiece().Size; i++) {
      if (!(blocks[index + i].GetPiece() == null || blocks[index + i].GetPiece() == ghostPiece.GetCurrentPiece())) {
        return false;
      }
    }

    MovePiece(ghostPiece.GetCurrentPiece(), index);

    ghostPiece.SetPiece(null);
    return true;
  }

  private void SetNextLevel() {
    if (screen != Screen.GAME) {
      return;
    }
    level++;
    nextLevelText.text = "NEXT LEVEL: " + level;
    linesInLevel = System.Math.Min(level, 7);
    dropFillMarker = linesInLevel * GameUtil.BOARD_WIDTH;
    if (level < 3) {
      dropTimerLimit = 3;
      deleteTimer = 16;
    } else if (level < 7) {
      dropTimerLimit = 2;
      deleteTimer = 16;
    } else {
      dropTimerLimit = 2.0f * 7.0f / level;
      deleteTimer = System.Math.Max(6, 16 - (level - 7));
    }
  }

  private void PositionPlayer() {
    Vector3 board = GameUtil.ToBoardSpace(playerIndex);
    player.rectTransform.anchoredPosition = new Vector2(board.x, board.y);
  }

  private void PositionLevelLine() {
    nextLevelLine.anchoredPosition= new Vector2(29, -86 + 12 * linesInLevel);
  }

  private void DrawBoard() {
    PositionPlayer();
    PositionLevelLine();
    foreach (Piece piece in pieces) {
      for (int i = 0; i < piece.Size; i++) {
        SetBlockPiece(piece.Start + i, piece);
      }
    }
  }

  private void MovePlayer(int value) {
    if (value != 0) {
      soundManager.audioSource.PlayOneShot(soundManager.move);
    }
    playerIndex = System.Math.Min(GameUtil.BOARD_SIZE - 1, System.Math.Max(0, playerIndex + value));
  }

  private void ShowStartScreen() {
    startScreen.alpha = 1;
  }

  private void HideStartScreen() {
    if (startscreenGameplay != null) {
      StopCoroutine(startscreenGameplay);
      startscreenGameplay = null;
    }
    startScreen.alpha = 0;
  }

  private void ShowGameOverScreen() {
    gameOverScreen.alpha = 1;
    newHighScoreText.gameObject.SetActive(highScore == playerScore);
    if (playerScore == highScore) {
      PlayerPrefs.SetInt(HIGH_SCORE_PREF, playerScore);
    }
  }

  private void HideGameOverScreen() {
    gameOverScreen.alpha = 0;
  }

  private void RefreshScoreText() {
    this.playerScoreText.text = "SCORE: " + playerScore;
    this.highScoreText.text = "HIGH: " + highScore;
  }

  private int ToIndex(Coords coords) {
    return GameUtil.BOARD_WIDTH * coords.y + coords.x;
  }

  private int ToIndex(int x, int y) {
    return GameUtil.BOARD_WIDTH * y + x;
  }

  private Piece CreateRandomPiece() {
    int index = r.Next(GameUtil.BOARD_WIDTH) + GameUtil.BOARD_SIZE;
    int size = r.Next(GameUtil.MIN_PIECE_SIZE, GameUtil.MAX_PIECE_SIZE + 1);
    Color color = GameUtil.COLORS[r.Next(GameUtil.COLORS.Length)];

    return new Piece(index, size, color);

  }

  private bool AddPiece(Piece piece) {
    int index = piece.Start;
    int size = piece.Size;

    while (index - GameUtil.BOARD_WIDTH >= 0 && PieceFits(index - GameUtil.BOARD_WIDTH, size)) {
      index = index - GameUtil.BOARD_WIDTH;
    }

    if (index + size > GameUtil.BOARD_SIZE) {
      // Game over!
      soundManager.audioSource.PlayOneShot(soundManager.gameOver);
      return false;
    }

    pieces.Add(piece);
    MovePiece(piece, index);
    UpdateScore(size * PIECE_VALUE);
    soundManager.audioSource.PlayOneShot(soundManager.drop);
    return true;
  }

  private bool PieceFits(int index, int size) {
    for (int i = 0; i < size; i++) {
      if (index + i < GameUtil.BOARD_SIZE && blocks[index + i].GetPiece() != null) {
        return false;
      }
    }
    return true;
  }

  private void SuckInPieces(bool force) {
    if (blocks[playerIndex].GetPiece() != null) {
      return;
    }
    if (force || suckTimer < 0) {
      suckTimer = 0.05f;
      SuckClosePieces(playerIndex);
    }
  }

  private void SuckClosePieces(int index) {
    for (int i = index + 1; i < GameUtil.BOARD_SIZE; i++) {
      if (blocks[i].GetPiece() != null) {
        int current = i;
        Piece nextPiece = blocks[i].GetPiece();
        while (current < GameUtil.BOARD_SIZE && nextPiece != null) {
          current = current + nextPiece.Size;
          MovePieceOneSpace(nextPiece, false);
          nextPiece = blocks[current].GetPiece();
        }
        return;
      }
    }
  }

  private void MovePieceOneSpace(Piece piece, bool positive) {
    if (positive) {
      SetBlockPiece(piece.Start, null);
      SetBlockPiece(piece.Start + piece.Size, piece);
      piece.Start = piece.Start + 1;
    } else {
      SetBlockPiece(piece.Start + piece.Size - 1, null);
      SetBlockPiece(piece.Start - 1, piece);
      piece.Start = piece.Start - 1;
    }
  }

  private void MovePiece(Piece piece, int newStart) {
    if (piece.Start == newStart) {
      return;
    }
    for (int i = 0; i < piece.Size; i++) {
      SetBlockPiece(piece.Start + i, null);
    }

    piece.Start = newStart;
    for (int i = 0; i < piece.Size; i++) {
      if (piece.Start + i < 0) {
        continue;
      }
      SetBlockPiece(piece.Start + i, piece);
    }
  }

  private void SetBlockPiece(int index, Piece p) {
    if (index < 0 || index >= GameUtil.BOARD_SIZE) {
      return;
    }
    blocks[index].SetPiece(p);
    int type;
    if (p == null) {
      type = 3;
    } else if (index == p.Start) {
      type = 0;
    } else if (index == p.Start + p.Size - 1) {
      type = 1;
    } else if (index > p.Start && index < p.Start + p.Size) {
      type = 2;
    } else {
      // This is the last code that I wrote for this.
      // I didn't have time to ensure this never
      // happens.  It might not.
      type = 3;
    }
    blocks[index].SetType(type);
  }

  private void UpdateScore(int toAdd) {
    if (screen != Screen.GAME) {
      return;
    }
    playerScore = playerScore + toAdd;
    if (playerScore > highScore) {
      highScore = playerScore;
    }
    RefreshScoreText();
  }

  private void ShakeCamera() {
    StartCoroutine(DoShakeRoutine());
  }

  private System.Collections.IEnumerator DoShakeRoutine() {
    int x = r.Next(-2, 2);
    int y = r.Next(-4, -2);
    Camera.main.transform.Translate(x, y, 0);
    yield return new WaitForSeconds(0.05f);
    Camera.main.transform.Translate(-x, -y, 0);
    yield return new WaitForSeconds(0.05f);
    x = r.Next(-2, 2);
    y = r.Next(0, 2);
    Camera.main.transform.Translate(x, y, 0);
    yield return new WaitForSeconds(0.05f);
    Camera.main.transform.Translate(-x, -y, 0);
  }
}