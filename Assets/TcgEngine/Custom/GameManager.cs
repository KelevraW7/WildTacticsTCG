using UnityEngine;
using TcgEngine;
using TcgEngine.Client;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        instance = this;
    }
}
