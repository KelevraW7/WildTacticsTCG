using UnityEngine;
using System.IO;
using TcgEngine.Client;

namespace TcgEngine
{
    /// <summary>
    /// Panel de debug para testing. Solo visible en el Editor y builds de desarrollo.
    /// Pulsa F12 para abrir/cerrar.
    /// Añade este script a cualquier GameObject en la escena del menú.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        private bool   visible      = false;
        private Rect   window_rect  = new Rect(20, 20, 260, 590);
        private string debug_card_id = "";

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
                visible = !visible;
        }

        void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!visible) return;
            window_rect = GUI.Window(9999, window_rect, DrawWindow, "🛠 DEBUG PANEL  [F12]");
#endif
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(8);

            // ── Info actual ───────────────────────────────────────────────────
            var udata = Authenticator.Get()?.UserData;
            string user  = Authenticator.Get()?.Username ?? "—";
            string coins = udata != null ? udata.coins.ToString() : "—";

            GUILayout.Label($"Usuario:  {user}");
            GUILayout.Label($"Wildcoins: {coins} WC");
            GUILayout.Space(10);

            // ── Wildcoins ─────────────────────────────────────────────────────
            GUILayout.Label("── Wildcoins ──────────────────");

            if (GUILayout.Button("Reset coins → 0"))
                SetCoins(0);

            if (GUILayout.Button("+100 wildcoins"))
                AddCoins(100);

            if (GUILayout.Button("+1.000 wildcoins"))
                AddCoins(1000);

            if (GUILayout.Button("+10.000 wildcoins"))
                AddCoins(10000);

            GUILayout.Space(10);

            // ── Colección ─────────────────────────────────────────────────────
            GUILayout.Label("── Colección ──────────────────");

            if (GUILayout.Button("Desbloquear TODAS las cartas"))
                UnlockAll();

            GUILayout.Space(6);

            // ── Carta individual ──────────────────────────────────────────────
            GUILayout.Label("── Carta individual ───────────");

            GUILayout.BeginHorizontal();
            GUILayout.Label("ID carta:", GUILayout.Width(62));
            debug_card_id = GUILayout.TextField(debug_card_id, 6, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            if (udata != null && !string.IsNullOrEmpty(debug_card_id))
            {
                string dv = VariantData.GetDefault()?.id ?? "";
                int dqty = udata.GetCardQuantity(debug_card_id, dv, true);
                GUILayout.Label($"Cantidad actual: {dqty}");
            }
            else
            {
                GUILayout.Label("Cantidad actual: —");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 copia"))  AddOneCard();
            if (GUILayout.Button("-1 copia"))  RemoveOneCard();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // ── Cuenta ────────────────────────────────────────────────────────
            GUILayout.Label("── Cuenta ─────────────────────");

            if (GUILayout.Button("Reset cuenta completa"))
                ResetAccount();

            GUILayout.Space(8);

            // ── Cerrar ────────────────────────────────────────────────────────
            if (GUILayout.Button("Cerrar"))
                visible = false;

            GUI.DragWindow();
        }

        // ── Acciones ──────────────────────────────────────────────────────────

        private async void SetCoins(int amount)
        {
            var udata = Authenticator.Get()?.UserData;
            if (udata == null) { Debug.LogWarning("[Debug] No hay UserData cargado."); return; }
            udata.coins = amount;
            await Authenticator.Get().SaveUserData();
            Debug.Log($"[Debug] Coins → {amount}");
        }

        private async void AddCoins(int amount)
        {
            var udata = Authenticator.Get()?.UserData;
            if (udata == null) { Debug.LogWarning("[Debug] No hay UserData cargado."); return; }
            udata.coins = Mathf.Max(0, udata.coins + amount);
            await Authenticator.Get().SaveUserData();
            Debug.Log($"[Debug] +{amount} coins → total {udata.coins}");
        }

        private async void UnlockAll()
        {
            var udata = Authenticator.Get()?.UserData;
            if (udata == null) { Debug.LogWarning("[Debug] No hay UserData cargado."); return; }
            await StarterManager.UnlockAll(udata);
            Debug.Log("[Debug] Todas las cartas desbloqueadas.");
        }

        private async void AddOneCard()
        {
            var udata = Authenticator.Get()?.UserData;
            if (udata == null || string.IsNullOrEmpty(debug_card_id))
            { Debug.LogWarning("[Debug] ID vacío o UserData no cargado."); return; }

            string variant = VariantData.GetDefault()?.id ?? "";
            udata.AddCard(debug_card_id, variant, 1);
            await Authenticator.Get().SaveUserData();
            int qty = udata.GetCardQuantity(debug_card_id, variant, true);
            Debug.Log($"[Debug] +1 copia carta '{debug_card_id}' → cantidad: {qty}");
        }

        private async void RemoveOneCard()
        {
            var udata = Authenticator.Get()?.UserData;
            if (udata == null || string.IsNullOrEmpty(debug_card_id))
            { Debug.LogWarning("[Debug] ID vacío o UserData no cargado."); return; }

            string variant = VariantData.GetDefault()?.id ?? "";
            udata.RemoveCard(debug_card_id, variant, 1);
            await Authenticator.Get().SaveUserData();
            int qty = udata.GetCardQuantity(debug_card_id, variant, true);
            Debug.Log($"[Debug] -1 copia carta '{debug_card_id}' → cantidad: {qty}");
        }

        private void ResetAccount()
        {
            string username = Authenticator.Get()?.Username;
            if (string.IsNullOrEmpty(username)) { Debug.LogWarning("[Debug] No hay usuario activo."); return; }

            string file     = username + ".user";
            string fullpath = Application.persistentDataPath + "/" + file;

            if (File.Exists(fullpath))
            {
                File.Delete(fullpath);
                Debug.Log($"[Debug] Cuenta '{username}' reseteada. Reinicia el juego para aplicar.");
            }
            else
            {
                Debug.Log($"[Debug] No se encontró archivo para '{username}'.");
            }
        }
    }
}
