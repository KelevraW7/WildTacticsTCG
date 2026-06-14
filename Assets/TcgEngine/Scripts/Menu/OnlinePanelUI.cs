using TcgEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Panel lateral que aparece al hacer clic en ONLINE.
    /// Reemplaza el WipPanel singleton para evitar conflictos con DesafioPanelUI.
    /// Conecta Btn_Cancel.OnClick → OnClickClose()
    /// </summary>
    public class OnlinePanelUI : UIPanel
    {
        private static OnlinePanelUI _instance;
        public static OnlinePanelUI Get() => _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        public void OnClickClose()
        {
            Hide();
        }
    }
}
