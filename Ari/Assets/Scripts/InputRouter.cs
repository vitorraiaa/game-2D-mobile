using UnityEngine;

public static class InputRouter
{
    // Retorna -1, 0 ou 1
    public static float AxisX()
    {
        if (MobileInputBridge.I && MobileInputBridge.I.useMobile)
            return MobileInputBridge.AxisX;

        // Teclado (A/D, Setas) — mantém compatibilidade desktop
        float x = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        return Mathf.Clamp(x, -1f, 1f);
    }

    // -1, 0, 1
    public static float AxisY()
    {
        if (MobileInputBridge.I && MobileInputBridge.I.useMobile)
            return MobileInputBridge.AxisY;

        float y = 0f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    y += 1f;
        return Mathf.Clamp(y, -1f, 1f);
    }

    // Um pulso de ataque (Attack)
    public static bool AttackTap()
    {
        if (MobileInputBridge.I && MobileInputBridge.I.useMobile)
            return MobileInputBridge.AttackTap;

        // mouse/teclado (mantém compatibilidade)
        return Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0);
    }

    // Um pulso de pulo (Jump)
    public static bool JumpTap()
    {
        if (MobileInputBridge.I && MobileInputBridge.I.useMobile)
            return MobileInputBridge.JumpTap;

        return Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");
    }

    // Um pulso de especial (E)
    public static bool SpecialTap()
    {
        if (MobileInputBridge.I && MobileInputBridge.I.useMobile)
            return MobileInputBridge.SpecialTap;

        return Input.GetKeyDown(KeyCode.E);
    }

    // “Segurando” botão de ataque (para tiro contínuo, se quiser)
    public static bool AttackHeld()
    {
        if (MobileInputBridge.I && MobileInputBridge.I.useMobile)
            return MobileInputBridge.AttackTap; // no mobile mapeamos como tap; se quiser “hold”, crie um Held no bridge

        return Input.GetButton("Fire1") || Input.GetMouseButton(0);
    }
}
