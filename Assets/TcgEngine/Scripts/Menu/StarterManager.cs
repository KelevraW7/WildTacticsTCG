using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Gestiona la colección inicial del jugador.
    /// Al crear cuenta nueva recibe 26 cartas de inicio (20 criaturas + 6 doradas).
    /// Usa el reward "starter_given" como flag para no repetir.
    /// Parches de expansión del pack usan flags adicionales (ej. "starter_patch_v2").
    /// </summary>
    public static class StarterManager
    {
        private const string STARTER_REWARD_ID   = "starter_given";
        private const string STARTER_PATCH_V2_ID = "starter_patch_v2"; // +Águila-agua(4) y +Pulpo-fuego(48)

        // ── IDs de las 26 cartas iniciales ────────────────────────────────────
        // VOLAR:      Cuervo-fuego=2, Águila-agua=4, Búho-agua=6, Águila-planta=7
        // DESTROZAR:  León-fuego=10, Lobo-agua=15, Tigre-planta=17
        // GOLPEAR:    Canguro-fuego=21, Gorila-agua=23, Oso-planta=25
        // EMBESTIR:   Toro-fuego=30, Elefante-agua=31, Ciervo-planta=35
        // SUMERGIR:   Ballena-fuego=38, Tiburón-agua=40, Cocodrilo-planta=45
        // INTOXICAR:  Serpiente-fuego=46, Pulpo-fuego=48, Pulpo-agua=51, Rata-planta=53
        // DORADAS:    Phoenix=55, Cerbero=56, Dragón=57, Unicornio=58, Megalodón=59, Cthulhu=60
        private static readonly string[] STARTER_IDS = new string[]
        {
            "2",  "4",  "6",  "7",   // VOLAR
            "10", "15", "17",         // DESTROZAR
            "21", "23", "25",         // GOLPEAR
            "30", "31", "35",         // EMBESTIR
            "38", "40", "45",         // SUMERGIR
            "46", "48", "51", "53",   // INTOXICAR
            "55", "56", "57", "58", "59", "60" // DORADAS
        };

        // ── Las 2 cartas añadidas en v2 del pack ─────────────────────────────
        private static readonly string[] PATCH_V2_IDS = new string[] { "4", "48" };

        // ── Colección completa (60 criaturas) para admin ──────────────────────
        private static readonly string[] ALL_CREATURE_IDS = new string[]
        {
             "1",  "2",  "3",  "4",  "5",  "6",  "7",  "8",  "9",
            "10", "11", "12", "13", "14", "15", "16", "17", "18",
            "19", "20", "21", "22", "23", "24", "25", "26", "27",
            "28", "29", "30", "31", "32", "33", "34", "35", "36",
            "37", "38", "39", "40", "41", "42", "43", "44", "45",
            "46", "47", "48", "49", "50", "51", "52", "53", "54",
            "55", "56", "57", "58", "59", "60"
        };

        /// <summary>
        /// Llama desde MainMenu.RefreshUserData() tras cargar los datos del usuario.
        /// — Primera vez: concede las 26 cartas del pack inicial.
        /// — Cuentas existentes: aplica parches de expansión del pack si faltan.
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> TryGiveStarter(UserData udata)
        {
            if (udata == null) return false;

            bool saved = false;

            // ── Pack inicial (primera vez) ────────────────────────────────────
            if (!udata.HasReward(STARTER_REWARD_ID))
            {
                AddStarterCards(udata);
                udata.AddReward(STARTER_REWARD_ID);
                saved = true;
                Debug.Log("[StarterManager] Colección inicial concedida (26 cartas).");
            }

            // ── Parche v2: Águila-agua(4) y Pulpo-fuego(48) ──────────────────
            // Se aplica a cuentas que ya tenían el pack inicial sin estas 2 cartas.
            if (!udata.HasReward(STARTER_PATCH_V2_ID))
            {
                string variant = VariantData.GetDefault()?.id ?? "";
                foreach (string id in PATCH_V2_IDS)
                {
                    if (udata.GetCardQuantity(id, variant, true) == 0)
                        udata.AddCard(id, variant, 1);
                }
                udata.AddReward(STARTER_PATCH_V2_ID);
                saved = true;
                Debug.Log("[StarterManager] Parche v2 aplicado: +Águila-agua(4) +Pulpo-fuego(48).");
            }

            if (saved)
                await Authenticator.Get().SaveUserData();

            return saved;
        }

        /// <summary>Añade las 24 cartas iniciales a udata (sin guardar).</summary>
        public static void AddStarterCards(UserData udata)
        {
            string variant = VariantData.GetDefault()?.id ?? "";
            foreach (string id in STARTER_IDS)
                udata.AddCard(id, variant, 1);
        }

        /// <summary>Admin: añade las 60 criaturas a udata y guarda.</summary>
        public static async System.Threading.Tasks.Task UnlockAll(UserData udata)
        {
            if (udata == null) return;
            string variant = VariantData.GetDefault()?.id ?? "";
            foreach (string id in ALL_CREATURE_IDS)
            {
                if (udata.GetCardQuantity(id, variant, true) == 0)
                    udata.AddCard(id, variant, 1);
            }
            await Authenticator.Get().SaveUserData();
            Debug.Log("[StarterManager] ¡Todas las cartas desbloqueadas!");
        }
    }
}
