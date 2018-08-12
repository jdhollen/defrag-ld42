using UnityEngine;

public class GhostPiece : MonoBehaviour {

  public GhostBlock ghostBlockPrefab;

  private Piece piece;
  private Piece displayPiece;

  private GhostBlock[] blocks = new GhostBlock[GameUtil.MAX_PIECE_SIZE];

  public void Start() {
    for (int i = 0; i < GameUtil.MAX_PIECE_SIZE; i++) {
      blocks[i] = Instantiate(ghostBlockPrefab, this.transform);
      blocks[i].gameObject.SetActive(false);
    }
  }

  public void SetPiece(Piece piece) {
    this.piece = piece;
    this.displayPiece = piece == null ? null : new Piece(piece.Start, piece.Size, ToGhost(piece.Color));

    int size = piece == null ? 0 : piece.Size;
    for (int i = 0; i < size; i++) {
      blocks[i].gameObject.SetActive(true);
      blocks[i].rectTransform.anchoredPosition = GameUtil.ToBoardSpace(displayPiece.Start + i);
      if (i == 0) {
        blocks[i].SetType(0);
      } else if (i == size - 1) {
        blocks[i].SetType(1);
      } else {
        blocks[i].SetType(2);
      }
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
      blocks[i].rectTransform.anchoredPosition = GameUtil.ToBoardSpace(displayPiece.Start + i);
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
}
