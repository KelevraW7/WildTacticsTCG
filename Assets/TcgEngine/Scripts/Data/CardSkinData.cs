using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Defines a visual skin (illustration) for a creature card.
    /// Multiple skins can be assigned to a CardData and unlocked via the maestrías system.
    /// </summary>
    [CreateAssetMenu(fileName = "skin", menuName = "TcgEngine/CardSkinData", order = 6)]
    public class CardSkinData : ScriptableObject
    {
        public string id;
        public string title;

        [Tooltip("Ilustración raw sin marco. Se combina con un CardFrameData en CardUI y CardBoard.")]
        public Sprite illustration;
    }
}
