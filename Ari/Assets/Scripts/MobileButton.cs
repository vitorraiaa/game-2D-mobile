using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEndDragHandler
{
    public enum Kind { Left, Right, Up, Down, Attack, Jump, Special }
    public Kind kind;

    [Header("Feedback (opcional)")]
    public Image targetImage;
    public float pressedAlpha = 0.6f;
    float originalAlpha = 1f;

    void Awake()
    {
        if (targetImage) originalAlpha = targetImage.color.a;

        // Para botões “tap” (Attack, Jump, Special), também dispare no click do Button
        var btn = GetComponent<Button>();
        if (btn && (kind == Kind.Attack || kind == Kind.Jump || kind == Kind.Special))
            btn.onClick.AddListener(ClickOnce);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!MobileInputBridge.I) return;
        if (targetImage) SetAlpha(pressedAlpha);

        switch (kind)
        {
            // Direcionais: manter “held” enquanto pressionado
            case Kind.Left:   MobileInputBridge.I.LeftPressed(true);   break;
            case Kind.Right:  MobileInputBridge.I.RightPressed(true);  break;
            case Kind.Up:     MobileInputBridge.I.UpPressed(true);     break;
            case Kind.Down:   MobileInputBridge.I.DownPressed(true);   break;

            // Ações: “tap” (um pulso)
            case Kind.Attack: MobileInputBridge.I.FirePressed();       break;
            case Kind.Jump:   MobileInputBridge.I.JumpPressed();       break;
            case Kind.Special:MobileInputBridge.I.SpecialPressed();    break;
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!MobileInputBridge.I) return;
        if (targetImage) SetAlpha(originalAlpha);

        // solta apenas os direcionais; ações são tap (não mantêm)
        switch (kind)
        {
            case Kind.Left:   MobileInputBridge.I.LeftPressed(false);   break;
            case Kind.Right:  MobileInputBridge.I.RightPressed(false);  break;
            case Kind.Up:     MobileInputBridge.I.UpPressed(false);     break;
            case Kind.Down:   MobileInputBridge.I.DownPressed(false);   break;
        }
    }

    public void OnEndDrag(PointerEventData e) => OnPointerUp(e);

    void ClickOnce()
    {
        if (!MobileInputBridge.I) return;

        // Segurança: só dispara em botões de ação
        switch (kind)
        {
            case Kind.Attack: MobileInputBridge.I.FirePressed();    break;
            case Kind.Jump:   MobileInputBridge.I.JumpPressed();    break;
            case Kind.Special:MobileInputBridge.I.SpecialPressed(); break;
        }
    }

    void SetAlpha(float a)
    {
        if (!targetImage) return;
        var c = targetImage.color; c.a = a; targetImage.color = c;
    }
}
