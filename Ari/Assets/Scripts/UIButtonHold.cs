using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Estado atual do botão (true enquanto o dedo estiver pressionando).")]
    public bool IsHeld { get; private set; }

    public void OnPointerDown(PointerEventData eventData) => IsHeld = true;
    public void OnPointerUp(PointerEventData eventData)   => IsHeld = false;

    void OnDisable() => IsHeld = false; // segurança se botão esconder/desabilitar
}

