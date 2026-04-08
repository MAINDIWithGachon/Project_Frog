using UnityEngine;
using LLMUnity;
using System;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance; 
    
    public LLMAgent llmAgent;
    
    public bool IsProcessing { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void AskAI(string userMessage, Action<string> onReply, Action onComplete)
    {
        if (llmAgent == null || string.IsNullOrEmpty(userMessage)) return;

        IsProcessing = true;
        
        _ = llmAgent.Chat(userMessage, 
            (reply) => onReply?.Invoke(reply), 
            () => {
                IsProcessing = false;
                onComplete?.Invoke();
            });
    }
}
