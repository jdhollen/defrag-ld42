using UnityEngine;

public class Piece {
  
  public int Size { get; private set; }
  public int Start { get; set; }
  public Color Color { get; private set; }
  public bool Locked { get; set; }
  public bool Flagged {get; set; }

  public Piece(int start, int size, Color color) {
    this.Start = start;
    this.Size = size;
    this.Color = color;
  }
}