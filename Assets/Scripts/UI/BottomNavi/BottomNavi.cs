using UnityEngine;

public class BottomNavi : MonoBehaviour
{
    [SerializeField] private GameObject[] normals;
    [SerializeField] private GameObject[] focuses;


    private void Start()
    {
        
    }

    public void SelectTab(int index)
    {
        if (normals == null || focuses == null)
        {
            Debug.LogError("[BottomNavi] Tab arrays are not assigned.");
            return;
        }

        if (normals.Length != focuses.Length)
        {
            Debug.LogError("[BottomNavi] normals and focuses length must be the same.");
            return;
        }

        if (index < 0 || index >= normals.Length)
        {
            Debug.LogWarning($"[BottomNavi] Invalid tab index: {index}");
            return;
        }

        for (int i = 0; i < normals.Length; i++)
        {
            bool isSelected = (i == index);

            if (normals[i] != null)
            {
                normals[i].SetActive(!isSelected);
            }

            if (focuses[i] != null)
            {
                focuses[i].SetActive(isSelected);
            }
        }
    }
    public void AllClose()
{
    if (normals == null || focuses == null)
    {
        Debug.LogError("[BottomNavi] Tab arrays are not assigned.");
        return;
    }

    for (int i = 0; i < normals.Length; i++)
    {
        if (normals[i] != null)
        {
            normals[i].SetActive(true);
        }

        if (focuses[i] != null)
        {
            focuses[i].SetActive(false);
        }
    }
}
}