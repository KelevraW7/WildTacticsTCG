using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Rellena el panel RivalInfo en la escena Game:
    ///   - Avatar aleatorio (distinto al del jugador)
    ///   - Nivel de la IA en función de la dificultad
    ///   - Subtítulo "Fácil" / "Casual" / "Experto"
    /// </summary>
    public class RivalInfoUI : MonoBehaviour
    {
        [Header("Referencias UI")]
        public Image avatar_image;
        public TextMeshProUGUI level_text;
        public TextMeshProUGUI subtitle_text;

        void Start()
        {
            SetupRivalAvatar();
            GameClient.Get().onGameStart += OnGameStart;
        }

        private void OnGameStart()
        {
            SetupDifficultyInfo();
        }

        void OnDestroy()
        {
            GameClient.Get().onGameStart -= OnGameStart;
        }

        private void SetupRivalAvatar()
        {
            List<AvatarData> all = AvatarData.GetAll();
            if (all == null || all.Count == 0) return;

            string player_avatar_id = Authenticator.Get()?.UserData?.GetAvatar() ?? "";

            List<AvatarData> candidates = all.FindAll(a => a.id != player_avatar_id && a.avatar != null);
            if (candidates.Count == 0)
                candidates = all;

            AvatarData picked = candidates[Random.Range(0, candidates.Count)];
            if (avatar_image != null)
                avatar_image.sprite = picked.avatar;
        }

        private void SetupDifficultyInfo()
        {
            int ai_level = GameClient.ai_settings.ai_level;

            string subtitle;
            int badge_level;

            if (ai_level <= 0)
            {
                subtitle = "Fácil";
                badge_level = 1;
            }
            else if (ai_level < 10)
            {
                subtitle = "Casual";
                badge_level = Random.Range(3, 6);
            }
            else
            {
                subtitle = "Experto";
                badge_level = Random.Range(7, 16);
            }

            if (subtitle_text != null)
                subtitle_text.text = subtitle;

            if (level_text != null)
                level_text.text = badge_level.ToString();
        }
    }
}
