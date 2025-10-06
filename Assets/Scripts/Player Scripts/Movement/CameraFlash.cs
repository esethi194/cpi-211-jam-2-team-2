using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraFlash : MonoBehaviour
{
    public Image flashImage;
    public float flashDuration = 0.2f;

    void Start()
    {
        // Ensure the flashImage is initially transparent.
        Color color = flashImage.color;
        color.a = 0;
        flashImage.color = color;
    }
    
    // Use this method for using the effect
    public void Flash()
    {
        // Stop any ongoing flashes to prevent overlap and start a new one.
        StopAllCoroutines();
        StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        float elapsedTime = 0f;
        Color color = flashImage.color;

        // Fade in
        while (elapsedTime < flashDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0, 1, elapsedTime / (flashDuration / 2));
            flashImage.color = color;
            yield return null;
        }

        elapsedTime = 0f;

        // Fade out
        while (elapsedTime < flashDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(1, 0, elapsedTime / (flashDuration / 2));
            flashImage.color = color;
            yield return null;
        }

        color.a = 0;
        flashImage.color = color;
    }
}