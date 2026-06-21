using System;
using UnityEngine;

[Serializable]
public struct LocalizedTextData
{
    [SerializeField] private LocalizationEntry title;
    [SerializeField] private LocalizationEntry description;

    public LocalizationEntry Title => title;
    public LocalizationEntry Description => description;
}
