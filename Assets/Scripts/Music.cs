using UnityEngine;

public class PersistentMusic : MonoBehaviour
{
    public static PersistentMusic Instance { get; private set; }
    private AudioSource audioSource;

    public float unmutedVolume = 0.4f;
    private bool isMuted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.volume = unmutedVolume;
            isMuted = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Mute()
    {
        if (!isMuted)
        {
            audioSource.volume = 0f;
            isMuted = true;
        }
    }
}