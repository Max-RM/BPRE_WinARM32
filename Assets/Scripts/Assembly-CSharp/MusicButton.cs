using UnityEngine;

public class MusicButton : MonoBehaviour
{
    private GameObject musicOffButton;
    private GameObject musicOnButton;
    private void OnEnable()
    {
        MusicManager.onMusicMuted += HandleMusicManageronMusicMuted;
        musicOffButton = base.transform.Find("MusicOffButton").gameObject;
        musicOnButton = base.transform.Find("MusicOnButton").gameObject;
        musicOnButton.SetActive(value: false);
        musicOffButton.SetActive(value: false);
        RefreshMusicButtonState();
    }

    private void OnDisable()
    {
        MusicManager.onMusicMuted -= HandleMusicManageronMusicMuted;
    }

    private void HandleMusicManageronMusicMuted(bool muted)
    {
        RefreshMusicButtonState();
    }

    private void RefreshMusicButtonState()
    {
        if (Singleton<MusicManager>.IsInstantiated() && Singleton<MusicManager>.Instance.MusicMuted)
        {
            musicOnButton.SetActive(value: false);
            musicOffButton.SetActive(value: true);
        }
        else
        {
            musicOffButton.SetActive(value: false);
            musicOnButton.SetActive(value: true);
        }
    }

    public void ToggleMusic()
    {
        Singleton<MusicManager>.Instance.ToggleMute();
        RefreshMusicButtonState();
    }
}