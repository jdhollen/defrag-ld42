using UnityEngine;

public static class GameUtil {
  public enum Powerup {
    NONE,
    FREEZE,
    COMPRESS,
    DELETE
  }

  public static readonly int BOARD_WIDTH = 6;
  public static readonly int BOARD_HEIGHT = 12;
  public static readonly int BOARD_SIZE = BOARD_WIDTH * BOARD_HEIGHT;

  public static readonly int MIN_PIECE_SIZE = 2;
  public static readonly int MAX_PIECE_SIZE = 4;

  public static readonly int DEAD_BLOCK_ROWS = 4;

  public static Color[] COLORS =
      { Color.magenta, Color.blue, Color.yellow, Color.green, Color.cyan };

  public static Coords ToCoords(int index) {
    return new Coords((index) % GameUtil.BOARD_WIDTH, (index) / GameUtil.BOARD_WIDTH);
  }

  public static Vector3 ToBoardSpace(int index) {
    Coords coords = ToCoords(index);
    return ToBoardSpace(coords.x, coords.y);
  }

  public static Vector3 ToBoardSpace(int coordX, int coordY) {
    int x = 12 * (GameUtil.BOARD_WIDTH - 1 - coordX) - 6 * (GameUtil.BOARD_WIDTH - 1);
    int y = 12 * coordY - 80;

    return new Vector3 (x, y, 0);
  }

  public static int NumPowerups() {
    return 3;
  }
}