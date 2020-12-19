using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameRateCounter : MonoBehaviour
{
    [SerializeField, Range(0.1f, 2f)]
    float sampleDuration = 1f;

    public enum DisplayMode { FPS, MS }

    [SerializeField]
    DisplayMode displayMode = DisplayMode.FPS;

    TextMeshProUGUI display;
    int framesAcc;
    float durationAcc = 0;
    float bestDuration = float.PositiveInfinity;
    float worstDuration = 0;

    void OnEnable()
    {
        display = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        float frameDuration = Time.unscaledDeltaTime;
        framesAcc += 1;
        durationAcc += frameDuration;
        bestDuration = Mathf.Min(bestDuration, frameDuration);
        worstDuration = Mathf.Max(worstDuration, frameDuration);

        if (durationAcc >= sampleDuration)
        {
            if (displayMode == DisplayMode.FPS)
            {
                display.SetText("FPS\n{0:1}\n{1:1}\n{2:1}",
                    1f / bestDuration,
                    framesAcc / durationAcc,
                    1f / worstDuration);
            }
            else
            {
                display.SetText(
                    "MS\n{0:1}\n{1:1}\n{2:1}",
                    1000f * bestDuration,
                    1000f * durationAcc / framesAcc,
                    1000f * worstDuration
                );
            }
            framesAcc = 0;
            durationAcc = 0;

            bestDuration = float.PositiveInfinity;
            worstDuration = 0;
        }
    }
}
