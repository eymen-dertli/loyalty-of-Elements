using UnityEngine;

public class OpenPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;

    private void Start()
    {
        if (panel != null
            && panel.name == "LevelPanel"
            && PlayerPrefs.GetInt(GameWinController.OpenLevelPanelKey, 0) == 1)
        {
            PlayerPrefs.SetInt(GameWinController.OpenLevelPanelKey, 0);
            PlayerPrefs.Save();
            panel.SetActive(true);
        }
    }

    public void OpenPanels()
    {
        panel.SetActive(true);
    }
}
