using UnityEngine;
using TMPro;

public class TurnBannerUI : MonoBehaviour
{
    public TextMeshProUGUI text;
    public TextMeshProUGUI turnCountText;
    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        // NO lo apagues en Awake, lo apagamos justo después en Start
        // gameObject.SetActive(false);
    }

    private void Start()
    {
        gameObject.SetActive(false);  // Se apaga justo después de activarse correctamente
    }

    public void Show(string message, int turnCount)
    {
        text.text = message;
        turnCountText.text = "Turno " + turnCount;
        gameObject.SetActive(true);
        rect.localScale = Vector3.one;
        CancelInvoke();
        Invoke("Hide", 1.8f);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
