using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StageUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text stageNameText;
    public Text objectivesText;
    public GameObject stageTransitionPanel;
    public List<GameObject> objectiveUIElements;

    private LevelManager levelManager;

    private void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        levelManager.onStageStart.AddListener(OnStageStart);
        levelManager.onStageComplete.AddListener(OnStageComplete);
        levelManager.onObjectiveProgress.AddListener(OnObjectiveProgress);
    }

    private void OnStageStart(StageDefinition stage)
    {
        stageNameText.text = $"Stage {levelManager.currentStageIndex + 1}: {stage.stageName}";
        UpdateObjectivesUI(stage);
    }

    private void UpdateObjectivesUI(StageDefinition stage)
    {
        string objectives = "";
        foreach (var objective in stage.objectives)
        {
            string progress = $"{objective.currentAmount}/{objective.targetAmount}";
            string status = objective.isCompleted ? "✓" : "";
            objectives += $"{objective.description} {progress} {status}\n";
        }
        objectivesText.text = objectives;
    }

    private void OnObjectiveProgress(StageObjective objective)
    {
        UpdateObjectivesUI(levelManager.currentLevel.stages[levelManager.currentStageIndex]);
    }

    private void OnStageComplete(StageDefinition stage)
    {
        stageTransitionPanel.SetActive(true);
        // Aşama tamamlama animasyonları ve efektleri
    }
}