using TcgEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Panel lateral que aparece al hacer clic en DESAFÍO.
    /// Reemplaza el WipPanel singleton para evitar conflictos con OnlinePanelUI.
    /// Conecta Btn_Cancel.OnClick → OnClickClose()
    /// </summary>
    public class DesafioPanelUI : UIPanel
    {
        private static DesafioPanelUI _instance;
        public static DesafioPanelUI Get() => _instance;

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
