using UnityEngine;

public class CandleItem : MonoBehaviour
{
    private Light candleLight;
    private bool isHeld = false;

    void Start()
    {
        candleLight = GetComponentInChildren<Light>();
    }

    public void SetHeld(bool held)
    {
        isHeld = held;

        // Turn off light when put in bag or switched away
        if (candleLight != null)
            candleLight.enabled = held;
    }
}