using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Defines a card frame (border overlay) used in CardUI and CardBoard.
    /// Frames can be element-specific (team != null) or universal (team == null).
    /// Unlockable via the maestrías system.
    /// </summary>
    [CreateAssetMenu(fileName = "frame", menuName = "TcgEngine/CardFrameData", order = 7)]
    public class CardFrameData : ScriptableObject
    {
        public string id;
        public string title;

        [Tooltip("Elemento al que pertenece este marco. Null = aplica a cualquier carta.")]
        public TeamData team;

        [Tooltip("Marco para CardUI (formato vertical card_preview).")]
        public Sprite frame_preview;

        [Tooltip("Marco para CardBoard (formato cuadrado de tablero).")]
        public Sprite frame_board;
    }
}
