using UnityEngine;

public class PowerupPiece : MonoBehaviour {

  public SpriteRenderer spriteRenderer;

  public Sprite freezeSprite;
  public Sprite compressSprite;
  public Sprite deleteSprite;

  private GameUtil.Powerup powerup;
  private int index;

  public void SetPowerup(GameUtil.Powerup powerup, int index) {
    this.powerup = powerup;
    this.index = index;
    if (powerup == GameUtil.Powerup.NONE) {
      this.gameObject.SetActive(false);
      return;
    }

    this.gameObject.SetActive(true);
    this.gameObject.transform.SetPositionAndRotation(
        GameUtil.ToBoardSpace(index), Quaternion.identity);
    if (powerup == GameUtil.Powerup.COMPRESS) {
      spriteRenderer.sprite = compressSprite;
    } else if (powerup == GameUtil.Powerup.FREEZE) {
      spriteRenderer.sprite = freezeSprite;
    } else if (powerup == GameUtil.Powerup.DELETE) {
      spriteRenderer.sprite = deleteSprite;
    }
  }

  public GameUtil.Powerup GetPowerup() {
    return powerup;
  }

  public int GetIndex() {
    return index;
  }
}
