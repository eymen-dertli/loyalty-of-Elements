using UnityEngine;

public class ClosePanel : MonoBehaviour
{

    public GameObject panel;

    public void ClosePanels()
    {
       panel.SetActive(false); 
    }

}
