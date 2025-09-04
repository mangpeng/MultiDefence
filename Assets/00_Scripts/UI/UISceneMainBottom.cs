
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UISceneMainBottom : MonoBehaviour
{
    public GameObject[] m_panels;
    public Button[] m_buttons;

    public Color m_activatedColor;

    private int m_lastActivatedIdx = -1;

    private void Start()
    {
        for (int i = 0; i < m_buttons.Count(); i++)
        {
            int idx = i; 
            m_buttons[i].onClick.AddListener(() => ShowPanel(idx));
        }

        ShowPanel(2);
    }

    public void ShowPanel(int idx)
    {
        if(m_lastActivatedIdx != -1)
        {
            m_buttons[m_lastActivatedIdx].GetComponent<Animator>().Play("bottomUI_Off");
        }

        m_buttons[idx].GetComponent<Animator>().Rebind();
        m_buttons[idx].GetComponent<Animator>().Play("bottomUI_On");
        m_lastActivatedIdx = idx;

        for (int i = 0; i < m_panels.Count(); i++)
        {
            var isActivated = i == idx;
            m_panels[i].SetActive(isActivated);
            m_buttons[i].GetComponent<Image>().color = isActivated ? m_activatedColor : Color.white;
        }
    }
}
