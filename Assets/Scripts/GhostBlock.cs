using UnityEngine;
using UnityEngine.UI;

public class GhostBlock : MonoBehaviour {

  public Image image;
  public RectTransform rectTransform;

  public Sprite left;
  public Sprite right;
  public Sprite center;

  public void SetType(int type) {
    if (type == 0) {
      image.sprite = right;
    } else if (type == 1) {
      image.sprite = left;
    } else {
      image.sprite = center;
    }
  }
}
