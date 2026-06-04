using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Defines all avatar data, including optional unlock conditions.
    ///
    /// unlock_type = Default   → disponible desde el inicio
    /// unlock_type = Shop      → se compra en la tienda (se añade vía AddAvatar)
    /// unlock_type = TotalVictories      → se desbloquea al alcanzar unlock_amount victorias totales
    /// unlock_type = CompetitiveVictories→ se desbloquea al alcanzar unlock_amount victorias Competitivas
    /// unlock_type = TotalMatches        → se desbloquea al jugar unlock_amount partidas
    /// </summary>

    public enum AvatarUnlockType
    {
        [InspectorName("Por defecto")]
        Default              = 0,
        [InspectorName("Tienda")]
        Shop                 = 1,
        [InspectorName("Victorias totales")]
        TotalVictories       = 2,
        [InspectorName("Victorias en Competitivo")]
        CompetitiveVictories = 3,
        [InspectorName("Total de partidas")]
        TotalMatches         = 4,
    }

    [CreateAssetMenu(fileName = "Avatar", menuName = "TcgEngine/Avatar", order = 10)]
    public class AvatarData : ScriptableObject
    {
        public string id;
        public Sprite avatar;
        public int sort_order;

        [Header("Desbloqueo")]
        public AvatarUnlockType unlock_type = AvatarUnlockType.Default;
        [Tooltip("Tienda → precio en WildCoins. Victorias/Partidas → cantidad necesaria para desbloquear automáticamente. Ignorado si unlock_type = Por defecto.")]
        public int unlock_amount = 0;

        // ── Static registry ──────────────────────────────────────────────────

        public static List<AvatarData> avatar_list = new List<AvatarData>();

        public static void Load(string folder = "")
        {
            if (avatar_list.Count == 0)
                avatar_list.AddRange(Resources.LoadAll<AvatarData>(folder));

            avatar_list.Sort((AvatarData a, AvatarData b) => {
                if (a.sort_order == b.sort_order)
                    return a.id.CompareTo(b.id);
                return a.sort_order.CompareTo(b.sort_order);
            });
        }

        public static AvatarData Get(string id)
        {
            foreach (AvatarData a in GetAll())
                if (a.id == id) return a;
            return null;
        }

        public static List<AvatarData> GetAll() => avatar_list;

        // ── Unlock helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Devuelve true si el usuario ya tiene este avatar disponible para usar.
        /// </summary>
        public bool IsUnlocked(UserData udata)
        {
            if (unlock_type == AvatarUnlockType.Default) return true;
            if (udata == null) return false;
            return udata.HasAvatar(id);
        }

        /// <summary>
        /// Comprueba si el avatar se debe desbloquear automáticamente según las
        /// estadísticas actuales del usuario (victorias, partidas...).
        /// Solo relevante para TotalVictories, CompetitiveVictories y TotalMatches.
        /// </summary>
        public bool ShouldAutoUnlock(UserData udata)
        {
            if (udata == null) return false;
            switch (unlock_type)
            {
                case AvatarUnlockType.TotalVictories:
                    return udata.victories >= unlock_amount;
                case AvatarUnlockType.CompetitiveVictories:
                    return udata.competitive_victories >= unlock_amount;
                case AvatarUnlockType.TotalMatches:
                    return udata.matches >= unlock_amount;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Texto descriptivo de la condición de desbloqueo (para mostrar en la UI).
        /// </summary>
        public string GetUnlockHint()
        {
            switch (unlock_type)
            {
                case AvatarUnlockType.Shop:
                    return unlock_amount > 0
                        ? $"Disponible en la tienda por {unlock_amount} WC"
                        : "Disponible en la tienda";
                case AvatarUnlockType.TotalVictories:
                    return $"Gana {unlock_amount} partidas";
                case AvatarUnlockType.CompetitiveVictories:
                    return $"Gana {unlock_amount} partidas en SOLO Competitivo";
                case AvatarUnlockType.TotalMatches:
                    return $"Juega {unlock_amount} partidas";
                default:
                    return "";
            }
        }
    }
}
