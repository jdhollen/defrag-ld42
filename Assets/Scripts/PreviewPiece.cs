using UnityEngine;

public class PreviewPiece : MonoBehaviour {

  public GameObject board;
  public Block blockPrefab;

  private Piece piece;
  private Piece displayPiece;

  private Block[] blocks = new Block[GameUtil.MAX_PIECE_SIZE];

  public void Awake() {
    for (int i = 0; i < GameUtil.MAX_PIECE_SIZE; i++) {
      blocks[i] = Instantiate(blockPrefab, board.transform);
      blocks[i].SetGhostLayer();
      blocks[i].gameObject.SetActive(false);
    }
  }

  public void SetPiece(Piece piece) {
    this.piece = piece;
    this.displayPiece = piece == null ? null : new Piece(piece.Start, piece.Size, ToGhost(piece.Color));

    int size = piece == null ? 0 : piece.Size;
    for (int i = 0; i < size; i++) {
      blocks[i].gameObject.SetActive(true);
      blocks[i].SetPiece(displayPiece);
      blocks[i].gameObject.transform.SetPositionAndRotation(
          GameUtil.ToBoardSpace(displayPiece.Start + i),
          Quaternion.identity);
    }
    for (int i = size; i < blocks.Length; i++) {
      blocks[i].gameObject.SetActive(false);
    }
  }

  public Piece GetCurrentPiece() {
    return piece;
  }

  public void Adjust(int delta) {
    if (displayPiece == null) {
      return;
    }
    displayPiece.Start =
        System.Math.Min(
            GameUtil.BOARD_SIZE - 1,
            System.Math.Max(0, displayPiece.Start + delta));
    for (int i = 0; i < displayPiece.Size; i++) {
      blocks[i].gameObject.transform.SetPositionAndRotation(
          GameUtil.ToBoardSpace(displayPiece.Start + i),
          Quaternion.identity);
    }
  }

  public int GetTargetIndex() {
    if (displayPiece == null) {
      return -1;
    }
    return displayPiece.Start;
  }

  private Color ToGhost(Color color) {
    return new Color(color.r, color.g, color.b, 0.5f);
  }

  public void SquishGhost(Block[] boardBlocks) {
    if (piece == null) {
      return;
    }
    int index = piece.Start;
    int size = piece.Size;

    while (index - GameUtil.BOARD_WIDTH >= 0 && PieceFits(index - GameUtil.BOARD_WIDTH, size, boardBlocks)) {
      index = index - GameUtil.BOARD_WIDTH;
    }

    Adjust(index - displayPiece.Start);
  }

  private bool PieceFits(int index, int size, Block[] boardBlocks) {
    for (int i = 0; i < size; i++) {
      if (index + i < GameUtil.BOARD_SIZE && boardBlocks[index + i].GetPiece() != null) {
        return false;
      }
    }
    return true;
  }
}
