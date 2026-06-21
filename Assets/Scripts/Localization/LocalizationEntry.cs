using System;
using UnityEngine;

[Serializable]
public struct LocalizationEntry
{
    [SerializeField] private string tableKey;
    [SerializeField] private string key;

    public LocalizationEntry(string tableKey, string key)
    {
        this.tableKey = tableKey;
        this.key = key;
    }

    public string TableKey => tableKey;
    public string Key => key;
    public bool IsValid => !string.IsNullOrEmpty(tableKey) && !string.IsNullOrEmpty(key);
}
