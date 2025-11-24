using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonTap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Verdadeiro apenas 1 frame após o toque.")]
    public bool ConsumedTap { get; private set; }

    bool pressed;

    public void OnPointerDown(PointerEventData eventData) { pressed = true; }
    public void OnPointerUp(PointerEventData eventData)   { pressed = false; }

    void LateUpdate()
    {
        // gera um “pulso” (true por 1 frame) quando soltar o dedo
        if (!pressed && ConsumedTap == false) return;
        if (pressed) return;

        // se chegou aqui, soltou nesse frame -> emite pulso e reseta no final do frame
        ConsumedTap = true;
        StartCoroutine(ClearNextFrame());
    }

    System.Collections.IEnumerator ClearNextFrame()
    {
        yield return null;
        ConsumedTap = false;
    }
}
