using UnityEngine;

public class OpenPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;

    public void OpenPanels()
    {
        panel.SetActive(true);
    }
}
