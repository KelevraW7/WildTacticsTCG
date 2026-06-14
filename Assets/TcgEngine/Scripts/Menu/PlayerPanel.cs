using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Player panel appears when you click on your avatar in the menu
    /// it shows all stats related to your account, and let you change avatar/cardback
    /// </summary>

    public class PlayerPanel : UIPanel
    {
        [Header("Player")]
        public Text player_name;
        public Text player_level;
        public AvatarUI avatar;
        public CardbackUI cardback;
        public Text elo;
        public Text winrate;        // WINRATE ONLINE  → "—"
        public Text cards_all;
        // Estadísticas por modo (formato "V / D")
        public Text online_vd;      // ONLINE           → "— / —"
        public Text solo_vd;        // SOLO             → "X / X"
        public Text competitive_vd; // COMPETITIVO      → "X / X"
        public Text total_matches;  // PARTIDAS TOTALES → "X"
        // Campos legacy (pueden quedar sin asignar si se usan los nuevos)
        public Text victories;
        public Text defeats;

        [Header("Bottom bar")]
        public GameObject buttons_area;
        public GameObject account_button;
        public GameObject sell_button;

        [Header("Avatars")]
        public UIPanel avatar_panel;
        public AvatarUI[] avatars;
        [Tooltip("Texto donde se muestra la condición de desbloqueo al pulsar un avatar bloqueado.")]
        public TMP_Text avatar_lock_hint;

        [Header("Cardbacks")]
        public UIPanel cardback_panel;
        public CardbackUI[] cardbacks;

        [Header("Edit Panel")]
        public UIPanel edit_panel;
        public InputField user_email;
        public InputField user_password_prev;
        public InputField user_password_new;
        public InputField user_password_confirm;
        public Button edit_change_email;
        public Button edit_change_password;
        public Button resend_button;
        public Button confirm_button;
        public Text edit_error;

        private string username;
        private UserData user_data;

        private static PlayerPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            foreach (AvatarUI icon in avatars)
                icon.onClick += OnClickAvatar;

            foreach (CardbackUI icon in cardbacks)
                icon.onClick += OnClickCardback;
        }

        protected override void Update()
        {
            base.Update();

        }

        protected override void Start()
        {
            base.Start();
            // Ocultarse automáticamente al pulsar cualquier tab del grupo "menu"
            TabButton.onClickAny += OnAnyTabClicked;
        }

        private void OnDestroy()
        {
            TabButton.onClickAny -= OnAnyTabClicked;
        }

        private void OnAnyTabClicked(TabButton btn)
        {
            if (btn.group == "menu" && IsVisible())
                Hide();
        }

        private async void LoadData()
        {
            if (IsYou())
                user_data = Authenticator.Get().UserData;
            else
                user_data = await ApiClient.Get().LoadUserData(username);

            RefreshPanel();
        }

        private void ClearPanel()
        {
            player_name.text = "";
            player_level.text = "";
            elo.text = "";
            winrate.text = "";
            if (online_vd      != null) online_vd.text      = "";
            if (solo_vd        != null) solo_vd.text        = "";
            if (competitive_vd != null) competitive_vd.text = "";
            if (total_matches  != null) total_matches.text  = "";
            if (victories      != null) victories.text      = "";
            if (defeats        != null) defeats.text        = "";
            avatar.Hide();
            cardback.Hide();
        }

        private void RefreshPanel()
        {
            avatar_panel.Hide();
            //cardback_panel.Hide();

            if (user_data != null)
            {
                UserData user = user_data;
                player_name.text = user.username;
                player_level.text = "—";   // Sistema de niveles pendiente

                AvatarData avatar = AvatarData.Get(user.avatar);
                this.avatar.SetAvatar(avatar);

                CardbackData cb = CardbackData.Get(user.cardback);
                this.cardback.SetCardback(cb);

                winrate.text = "—";   // Winrate online pendiente
                elo.text     = "—";   // ELO online pendiente

                // Estadísticas en formato "V / D"
                if (online_vd      != null) online_vd.text      = "—V / —D";
                if (solo_vd        != null) solo_vd.text        = user.victories + "V / " + user.defeats + "D";
                if (competitive_vd != null) competitive_vd.text = user.competitive_victories + "V / " + user.competitive_defeats + "D";
                if (total_matches  != null) total_matches.text  = user.matches.ToString();

                // Campos legacy (por compatibilidad si siguen asignados en el Inspector)
                if (victories != null) victories.text = user.victories.ToString();
                if (defeats   != null) defeats.text   = user.defeats.ToString();

                cards_all.text = user.CountUniqueCards() + " / " + CardData.GetAll().Count;

                buttons_area?.SetActive(IsYou());    //Buttons like logout only active if your account
                account_button?.SetActive(Authenticator.Get().IsApi());
                sell_button?.SetActive(Authenticator.Get().IsApi());
            }
        }

        private void RefreshAvatarList()
        {
            UserData udata = Authenticator.Get()?.UserData;

            foreach (AvatarUI icon in avatars)
                icon.SetDefaultAvatar();

            if (avatar_lock_hint != null)
                avatar_lock_hint.text = "";

            int index = 0;
            foreach (AvatarData adata in AvatarData.GetAll())
            {
                if (index >= avatars.Length) break;
                if (adata == null) continue;

                AvatarUI line = avatars[index];
                line.SetAvatar(adata);
                line.SetLocked(!adata.IsUnlocked(udata), disable_button: false);
                index++;
            }
        }

        private void RefreshCardBackList()
        {
            foreach (CardbackUI line in cardbacks)
                line.Hide();

            int index = 0;
            foreach (CardbackData cbdata in CardbackData.GetAll())
            {
                if (index < cardbacks.Length)
                {
                    CardbackUI line = cardbacks[index];
                    if (cbdata != null)
                    {
                        line.SetCardback(cbdata);
                        index++;
                    }
                }
            }
        }

        private void OnClickAvatar(AvatarData avatar)
        {
            user_data = Authenticator.Get().UserData;
            if (avatar == null || user_data == null || !IsYou()) return;

            // Avatar bloqueado: mostrar pista de desbloqueo
            if (!avatar.IsUnlocked(user_data))
            {
                if (avatar_lock_hint != null)
                    avatar_lock_hint.text = avatar.GetUnlockHint();
                return;
            }

            if (avatar_lock_hint != null)
                avatar_lock_hint.text = "";

            user_data.avatar = avatar.id;
            RefreshPanel();
            SaveUserAvatar(avatar);
            avatar_panel.Hide();
        }

        private void OnClickCardback(CardbackData cb)
        {
            user_data = Authenticator.Get().UserData;
            if (cb != null && user_data != null && IsYou())
            {
                user_data.cardback = cb.id;
                RefreshPanel();
                SaveUserCardback(cb);
                cardback_panel.Hide();
            }
        }

        private async void SaveUserAvatar(AvatarData avatar)
        {
            if (ApiClient.Get().IsConnected())
            {
                string url = ApiClient.ServerURL + "/users/edit/" + ApiClient.Get().UserID;
                EditUserRequest req = new EditUserRequest();
                req.avatar = avatar.id;
                string json_data = ApiTool.ToJson(req);
                await ApiClient.Get().SendRequest(url, "POST", json_data);
            }
            await Authenticator.Get().SaveUserData();
            MainMenu.Get().RefreshUserData();
            RefreshPanel();
        }

        private async void SaveUserCardback(CardbackData cardback)
        {
            if (ApiClient.Get().IsConnected())
            {
                string url = ApiClient.ServerURL + "/users/edit/" + ApiClient.Get().UserID;
                EditUserRequest req = new EditUserRequest();
                req.cardback = cardback.id;
                string json_data = ApiTool.ToJson(req);
                await ApiClient.Get().SendRequest(url, "POST", json_data);
            }
            await Authenticator.Get().SaveUserData();
            MainMenu.Get().RefreshUserData();
            RefreshPanel();
        }

        public void OnClickAvatar()
        {
            if (!IsYou())
                return;

            RefreshAvatarList();
            avatar_panel.Show();
        }

        public void OnClickCardBack()
        {
            if (!IsYou())
                return;

            RefreshCardBackList();
            cardback_panel.Show();
        }

        public void OnClickFriends()
        {
            FriendPanel.Get().Show();
        }

        public void OnClickDuplicates()
        {
            SellDuplicatePanel.Get().Show();
        }

        public void OnClickEdit()
        {
            user_email.readOnly = true;
            user_password_prev.readOnly = true;
            user_password_new.readOnly = true;
            user_password_confirm.readOnly = true;
            user_password_new.gameObject.SetActive(false);
            user_password_confirm.gameObject.SetActive(false);

            UserData udata = Authenticator.Get().UserData;
            user_email.text = udata.email;
            user_password_prev.text = "password";
            user_password_new.text = "password";
            user_password_confirm.text = "password";
            edit_change_email.gameObject.SetActive(true);
            edit_change_password.gameObject.SetActive(true);
            resend_button.gameObject.SetActive(udata.validation_level == 0);
            confirm_button.gameObject.SetActive(false);
            edit_error.text = "";
            edit_panel.Show();
        }

        public void OnClickChangePass()
        {
            OnClickEdit();
            user_password_prev.readOnly = false;
            user_password_new.readOnly = false;
            user_password_confirm.readOnly = false;
            user_password_prev.text = "";
            user_password_new.text = "";
            user_password_confirm.text = "";
            user_password_new.gameObject.SetActive(true);
            user_password_confirm.gameObject.SetActive(true);
            edit_change_email.gameObject.SetActive(false);
            edit_change_password.gameObject.SetActive(false);
            resend_button.gameObject.SetActive(false);
            confirm_button.gameObject.SetActive(true);
            user_password_prev.Select();
        }

        public void OnClickChangeEmail()
        {
            OnClickEdit();
            user_email.readOnly = false;
            edit_change_email.gameObject.SetActive(false);
            edit_change_password.gameObject.SetActive(false);
            resend_button.gameObject.SetActive(false);
            confirm_button.gameObject.SetActive(true);
            user_email.Select();
        }

        public async void OnClickResendConfirm()
        {
            edit_error.text = "";
            string url = ApiClient.ServerURL + "/users/email/resend";
            WebResponse res = await ApiClient.Get().SendPostRequest(url, "");
            if (res.success)
            {
                edit_panel.Hide();
            }
            else
            {
                edit_error.text = res.error;
            }
        }

        public async void OnClickEditConfirm()
        {
            edit_error.text = "";

            if (!user_email.readOnly && user_email.text.Length > 0)
            {
                EditEmailRequest req = new EditEmailRequest();
                req.email = user_email.text;
                string url = ApiClient.ServerURL + "/users/email/edit/";
                string json = ApiTool.ToJson(req);
                WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
                if (res.success)
                {
                    edit_panel.Hide();
                    MainMenu.Get().RefreshUserData();
                }
                else
                {
                    edit_error.text = res.error;
                }
            }
            else if (!user_password_new.readOnly && user_password_new.text.Length > 0)
            {
                if (user_password_new.text == user_password_confirm.text)
                {
                    EditPasswordRequest req = new EditPasswordRequest();
                    req.password_previous = user_password_prev.text;
                    req.password_new = user_password_new.text;
                    string url = ApiClient.ServerURL + "/users/password/edit/";
                    string json = ApiTool.ToJson(req);
                    WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
                    if (res.success)
                    {
                        edit_panel.Hide();
                    }
                    else
                    {
                        edit_error.text = res.error;
                    }
                }
            }
        }

        public bool IsYou()
        {
            return username == ApiClient.Get().Username;
        }

        public void ShowPlayer()
        {
            string user = ApiClient.Get().Username;
            ShowPlayer(user);
        }

        public void ShowPlayer(string user)
        {
            if (username != user)
                ClearPanel();
            username = user;
            LoadData();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            ShowPlayer();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            edit_panel.Hide();
        }

        public static PlayerPanel Get()
        {
            return instance;
        }
    }
}
