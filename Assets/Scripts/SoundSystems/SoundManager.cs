using System;
using UnityEngine;
using System.Collections; 

public class SoundManager : MonoBehaviour
{
    private AudioSource audioSource;
    
    // NEW: Interval settings (Set these in the Inspector!)
    [Header("Ambient Sound Intervals")]
    public float minDelay = 8f; // Minimum time between looped sounds
    public float maxDelay = 15f; // Maximum time between looped sounds
    private Coroutine soundLoopCoroutine; // Keeps track of the running loop
    
    // Assign these clips in the Unity Inspector
    public AudioClip roamingClip;
    public AudioClip stalkingClip;
    public AudioClip chargeClip;
    public AudioClip chargingClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        
        audioSource.loop = false;
    }

    public void PlaySoundForState(int soundID)
    {
        // Stop any existing loop coroutine immediately when the state changes
        if (soundLoopCoroutine != null)
        {
            StopCoroutine(soundLoopCoroutine);
            soundLoopCoroutine = null;
        }

        switch (soundID)
        {
            case 1:
                soundLoopCoroutine = StartCoroutine(SoundLoop(roamingClip));
                
                break;
            case 2:
                soundLoopCoroutine = StartCoroutine(SoundLoop(stalkingClip));
                break;
            case 3:
                soundLoopCoroutine = StartCoroutine(PlayChargeSequence());
                break;
            default:
                audioSource.Stop();
                break;
        }
    }
    /// <summary>
    /// Coroutine that plays an audio clip, waits for the clip duration, 
    /// and then waits for a random delay before playing again.
    /// </summary>
    IEnumerator SoundLoop(AudioClip clip)
    {
        while (true) // Loop indefinitely while the monster is in this state
        {
            // 1. Play the clip
            audioSource.PlayOneShot(clip);

            // 2. Wait for the clip duration to finish
            yield return new WaitForSeconds(clip.length);
            
            // 3. Wait for a random, defined interval
            float delay = UnityEngine.Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);
        }
    }
    /// <summary>
    /// NEW: Coroutine that plays the two charge clips one after the other.
    /// </summary>
    IEnumerator PlayChargeSequence()
    {
        
       
        // 1. Play the sustained sound of heavy footsteps/movement
        if (chargingClip != null)
        {
            
            // For a simpler one-shot approach:
            audioSource.PlayOneShot(chargingClip);
            yield return new WaitForSeconds(chargingClip.length);
        }
     if (chargeClip != null)
     {
        audioSource.PlayOneShot(chargeClip);
        // Wait for the duration of the roar before starting the footsteps
        
     }

     
    }
}
