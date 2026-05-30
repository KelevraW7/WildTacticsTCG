using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// AI player base class, other AI inherit from this
    /// </summary>

    public abstract class AIPlayer
    {
        public int player_id;
        public int ai_level;

        protected GameLogic gameplay;

        public virtual void Update()
        {
            //Script called by game server to update AI
            //Override this to let the AI play
        }

        public bool CanPlay()
        {
            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            bool can_play = game_data.IsPlayerTurn(player);
            return can_play && !gameplay.IsResolving();
        }

        public static AIPlayer Create(AIType type, GameLogic gameplay, int id, int level = 0)
        {
            if (type == AIType.Random)
                return new AIPlayerRandom(gameplay, id, level);
            if (type == AIType.Medium)
                return new AIPlayerMedium(gameplay, id, level);
            if (type == AIType.MiniMax)
                return new AIPlayerMM(gameplay, id, level);
            return null;
        }

        /// <summary>
        /// Derive AI type from ai_level so the server doesn't need a separate field.
        ///   level 0        → Random  (Fácil)
        ///   level 1 – 5   → Medium  (Intermedio)
        ///   level 6 – 10  → MiniMax (Difícil)
        /// </summary>
        public static AIType TypeFromLevel(int level)
        {
            if (level <= 0) return AIType.Random;
            if (level <= 5) return AIType.Medium;
            return AIType.MiniMax;
        }
    }

    public enum AIType
    {
        Random  = 0,   // IA aleatoria — Fácil
        Medium  = 5,   // IA con ventaja de tipo — Intermedio
        MiniMax = 10,  // IA con Minimax + alpha-beta — Difícil
    }
}
