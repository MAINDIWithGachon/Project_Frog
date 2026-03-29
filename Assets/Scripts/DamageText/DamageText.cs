using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    public float finalDamage;
    public bool iscri;//true면 치명타
    public Color baseColor;//일반
    public Color criColor;//치명타 붉은색 처리

    private TMP_Text tmpText;
    private TextMesh textMesh;

    private void Awake()
    {
        tmpText = GetComponentInChildren<TMP_Text>(true);
        textMesh = GetComponentInChildren<TextMesh>(true);
    }

    public void Init(float damage, bool isCri)
    {
        finalDamage = damage;
        iscri = isCri;

        string damageText = Mathf.RoundToInt(finalDamage).ToString();
        Color targetColor = iscri ? criColor : baseColor;

        if (tmpText == null)
            tmpText = GetComponentInChildren<TMP_Text>(true);

        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMesh>(true);

        if (tmpText != null)
        {
            tmpText.text = damageText;
            tmpText.color = targetColor;
        }

        if (textMesh != null)
        {
            textMesh.text = damageText;
            textMesh.color = targetColor;
        }
    }
}
