using System;
using TMPro;
using UnityEngine;

public class DateDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private string format = "yyyy-MM-dd";

    private void Awake()
    {
        if (dateText == null)
            dateText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (dateText != null)
            dateText.text = DateTime.Now.ToString(format);
    }
}
