using UnityEngine;
using UnityEngine.UI;

public class UITextLocale : MonoBehaviour
{
    [SerializeField]
    private string m_id;
    public string ID { get => m_id; set => m_id = value; }
    public string Text => INLocalization.Instance.GetText(m_id);

    private void Awake()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        Text component = GetComponent<Text>();
        if (!string.IsNullOrEmpty(m_id) && component != null)
        {
            component.text = Text;
        }
    }
}