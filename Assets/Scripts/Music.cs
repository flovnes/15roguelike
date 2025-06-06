using UnityEngine;

public class PersistentMusic : MonoBehaviour
{
    public static PersistentMusic Instance { get; private set; }
    private AudioSource audioSource;

    public static string VolumePlayerPrefsKey = "GameMasterVolume";
    public float defaultVolume = 0.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = PlayerPrefs.GetFloat(VolumePlayerPrefsKey, defaultVolume);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
}