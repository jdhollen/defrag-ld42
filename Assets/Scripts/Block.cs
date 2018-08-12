using UnityEngine;

public class Block : MonoBehaviour {

  public SpriteRenderer spriteRenderer;

  public Sprite left;
  public Sprite right;
  public Sprite center;
  public Sprite single;

  private Piece piece;

  public void Update() {
    if (piece == null || piece.Locked) {
      return;
    }

    if (piece.Flagged) {
      spriteRenderer.color = Color.red;
    } else {
      spriteRenderer.color = piece.Color;
    }
  }

  public void SetPiece(Piece piece) {
    if (this.piece != null && this.piece == piece) {
      return;
    }
    this.piece = piece;
    if (piece == null) {
      this.spriteRenderer.sprite = single;
      this.spriteRenderer.color = Color.grey;
    } else {
      this.spriteRenderer.color = piece.Color;
    }
  }

  public void SetType(int type) {
    if (type == 0) {
      spriteRenderer.sprite = right;
    } else if (type == 1) {
      spriteRenderer.sprite = left;
    } else if (type == 2) {
      spriteRenderer.sprite = center;
    } else if (type == 3) {
      spriteRenderer.sprite = single;
    }
  }

  public Piece GetPiece() {
    return piece;
  }

  public void SetGhostLayer() {
    spriteRenderer.sortingLayerName = "Ghost";
  }

  public void GrayOut() {
    Color c = this.spriteRenderer.color;
    this.spriteRenderer.color = new Color(c.r * .75f, c.g * .75f, c.b * .75f, c.a);
  }
}
