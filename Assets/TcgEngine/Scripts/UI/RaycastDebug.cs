using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Temporal: muestra qué elemento UI recibe el click en cada posición.
    /// Añadir como componente a cualquier objeto activo (ej. PackPanel).
    /// BORRAR cuando se solucione el problema.
    /// </summary>
    public class RaycastDebug : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var pointer = new PointerEventData(EventSystem.current);
                pointer.position = Input.mousePosition;

                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, results);

                if (results.Count == 0)
                {
                    Debug.Log("[RaycastDebug] No hit en " + Input.mousePosition);
                    return;
                }

                Debug.Log("[RaycastDebug] Click en " + Input.mousePosition + " — " + results.Count + " hits:");
                for (int i = 0; i < results.Count; i++)
                {
                    var r = results[i];
                    Debug.Log($"  [{i}] {r.gameObject.name}  (parent: {r.gameObject.transform.parent?.name})  depth={r.depth}  sortOrder={r.sortingOrder}");
                }
            }
        }
    }
}
