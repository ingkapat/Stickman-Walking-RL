using UnityEngine;
using TMPro;

public class WalkerUI : MonoBehaviour
{
    public WalkerAgent agent;
    public TMP_Text distanceText;
    public TMP_Text speedText;
    public TMP_Text rewardText;
    public TMP_Text episodeText;
    public TMP_Text bestDistanceText;

    private float bestDistance = 0f;

    void Update()
    {
        if (agent == null) return;

        float dist = agent.DistanceTraveled;
        float speed = agent.CurrentSpeed;
        float reward = agent.GetCumulativeReward();
        int episode = agent.CompletedEpisodes;

        if (dist > bestDistance) bestDistance = dist;

        if (distanceText) distanceText.text = $"Distance: {dist:F2} m";
        if (speedText) speedText.text = $"Speed: {speed:F2} m/s";
        if (rewardText) rewardText.text = $"Reward: {reward:F2}";
        if (episodeText) episodeText.text = $"Episode: {episode}";
        if (bestDistanceText) bestDistanceText.text = $"Best: {bestDistance:F2} m";
    }
}
