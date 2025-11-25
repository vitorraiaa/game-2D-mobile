using UnityEngine;

public class MobileInputBridge : MonoBehaviour
{
    // --- Singleton esperado por MobileButton.cs (I) ---
    public static MobileInputBridge I { get; private set; }

    [Header("Geral")]
    public bool useMobile = true;

    [Header("Held (direcional)")]
    public bool leftHeld, rightHeld, upHeld, downHeld;

    [Header("Taps (ações)")]
    public bool attackTap, jumpTap, specialTap;

    // Saídas que os scripts do player podem ler
    public static float AxisX { get; private set; }
    public static float AxisY { get; private set; }
    public static bool  AttackTap { get; private set; }
    public static bool  JumpTap   { get; private set; }
    public static bool  SpecialTap{ get; private set; }

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Update()
    {
        if (!useMobile)
        {
            AxisX = AxisY = 0f;
            AttackTap = JumpTap = SpecialTap = false;
            return;
        }

        int x = 0, y = 0;
        if (leftHeld)  x--;
        if (rightHeld) x++;
        if (downHeld)  y--;
        if (upHeld)    y++;

        AxisX = Mathf.Clamp(x, -1, 1);
        AxisY = Mathf.Clamp(y, -1, 1);

        // Pulsos de 1 frame
        AttackTap  = attackTap;
        JumpTap    = jumpTap;
        SpecialTap = specialTap;

        attackTap = jumpTap = specialTap = false; // reset
    }

    // ---------------- DIRECIONAIS ----------------
    public void LeftPressed(bool isDown)  { leftHeld  = isDown; }
    public void RightPressed(bool isDown) { rightHeld = isDown; }
    public void UpPressed(bool isDown)    { upHeld    = isDown; }
    public void DownPressed(bool isDown)  { downHeld  = isDown; }

    // --- Aliases esperados pelo MobileButton.cs ---
    public void SetLeft(bool v)  => LeftPressed(v);
    public void SetRight(bool v) => RightPressed(v);
    public void SetUp(bool v)    => UpPressed(v);
    public void SetDown(bool v)  => DownPressed(v);

    // ---------------- AÇÕES (tap) ----------------
    public void FirePressed()                 { attackTap = true; }
    public void FirePressed(bool isDown)      { if (isDown) attackTap = true; }

    public void JumpPressed()                 { jumpTap   = true; }
    public void JumpPressed(bool isDown)      { if (isDown) jumpTap = true; }

    public void SpecialPressed()              { specialTap = true; }
    public void SpecialPressed(bool isDown)   { if (isDown) specialTap = true; }

    // ---------------- Genéricos (opcional) ----------------
    public void SetHeld(string dir, bool val)
    {
        switch (dir)
        {
            case "Left":  leftHeld  = val; break;
            case "Right": rightHeld = val; break;
            case "Up":    upHeld    = val; break;
            case "Down":  downHeld  = val; break;
        }
    }

    public void Tap(string action)
    {
        switch (action)
        {
            case "Attack":  attackTap  = true; break;
            case "Jump":    jumpTap    = true; break;
            case "Special": specialTap = true; break;
        }
    }
}
