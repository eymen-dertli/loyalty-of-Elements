using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;
    public AudioSource musicSource;
    public Image musicOffImage;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }

        // Baþlangýçta görseli gizle (müzik çalýyor)
        if (musicOffImage != null)
        {
            musicOffImage.enabled = false;
        }
    }

    public void ToggleMusic()
    {
        if (musicSource != null)
        {
            if (musicSource.isPlaying)
            {
                musicSource.Pause();
                ShowMusicOffImage();
            }
            else
            {
                musicSource.Play();
                HideMusicOffImage();
            }
        }
    }

    public void EnableMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.Play();
            HideMusicOffImage();
        }
    }

    public void DisableMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
            ShowMusicOffImage();
        }
    }

    private void ShowMusicOffImage()
    {
        if (musicOffImage != null)
        {
            musicOffImage.enabled = true;
        }
    }

    private void HideMusicOffImage()
    {
        if (musicOffImage != null)
        {
            musicOffImage.enabled = false;
        }
    }
}
