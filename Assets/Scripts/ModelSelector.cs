using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.InferenceEngine;
using TMPro;

public class ModelSelector : MonoBehaviour
{
    [Header("Models (drag Walker9-14)")]
    public ModelAsset[] models;

    [Header("Display Names (same order)")]
    public string[] displayNames;

    [Header("References")]
    public Agent agent;
    public BehaviorParameters behaviorParameters;
    public TMP_Dropdown dropdown;
    public TMP_Text currentModelText;

    void Start()
    {
        if (dropdown != null)
        {
            dropdown.ClearOptions();
            var names = new System.Collections.Generic.List<string>();
            for (int i = 0; i < models.Length; i++)
            {
                string name = i < displayNames.Length ? displayNames[i] : $"Model {i}";
                names.Add(name);
            }
            dropdown.AddOptions(names);
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        if (models.Length > 0)
        {
            LoadModel(0);
        }
    }

    void OnDropdownChanged(int index)
    {
        LoadModel(index);
    }

    public void LoadModel(int index)
    {
        if (index < 0 || index >= models.Length) return;
        if (behaviorParameters == null) return;

        behaviorParameters.Model = models[index];
        behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;

        if (agent != null)
        {
            agent.EndEpisode();
        }

        string name = index < displayNames.Length ? displayNames[index] : $"Model {index}";
        if (currentModelText != null)
        {
            currentModelText.text = $"Model: {name}";
        }

        Debug.Log($"Loaded: {name}");
    }
}