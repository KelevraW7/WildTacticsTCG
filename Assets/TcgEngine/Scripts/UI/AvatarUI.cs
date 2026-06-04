using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Displays an avatar
    /// </summary>

    public class AvatarUI : MonoBehaviour
    {
        public UnityAction<AvatarData> onClick;

        private Image avatar_img;
        private Button avatar_button;
        private Sprite default_icon;

        private AvatarData avatar;

        void Awake()
        {
            avatar_img = GetComponent<Image>();
            avatar_button = GetComponent<Button>();
            default_icon = avatar_img.sprite;

            if (avatar_button != null)
                avatar_button.onClick.AddListener(OnClick);
        }

        public void SetAvatar(AvatarData avatar)
        {
            this.avatar = avatar;
            avatar_img.enabled = true;
            avatar_img.sprite = default_icon;

            if (avatar != null)
            {
                avatar_img.sprite = avatar.avatar;
            }
        }

        public void SetDefaultAvatar()
        {
            this.avatar = null;
            avatar_img.enabled = true;
            avatar_img.sprite = default_icon;
            SetLocked(false);
        }

        /// <summary>
        /// Aplica el estado visual de bloqueado: oscurece la imagen.
        /// disable_button=true (defecto) también desactiva el botón;
        /// disable_button=false deja el botón activo para permitir clicks de información.
        /// </summary>
        public void SetLocked(bool locked, bool disable_button = true)
        {
            avatar_img.color = locked ? new Color(1f, 1f, 1f, 0.30f) : Color.white;
            if (avatar_button != null && disable_button)
                avatar_button.interactable = !locked;
        }

        public bool IsLocked()
        {
            if (avatar == null) return false;
            UserData udata = Authenticator.Get()?.UserData;
            return !avatar.IsUnlocked(udata);
        }

        public void SetImage(Sprite sprite)
        {
            avatar_img.sprite = sprite;
        }

        public void Hide()
        {
            this.avatar = null;
            avatar_img.enabled = false;
        }

        public AvatarData GetAvatar()
        {
            return avatar;
        }

        private void OnClick()
        {
            onClick?.Invoke(avatar);
        }
    }
}