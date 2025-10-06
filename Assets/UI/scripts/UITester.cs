using UnityEngine;

public class UITester : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) GameSignals.Reported?.Invoke(true);   // correct
        if (Input.GetKeyDown(KeyCode.B)) GameSignals.Reported?.Invoke(false);  // wrong
        if (Input.GetKeyDown(KeyCode.L)) GameSignals.Win?.Invoke();            // win
        if (Input.GetKeyDown(KeyCode.K)) GameSignals.Lose?.Invoke("dev");      // lose
    }
}