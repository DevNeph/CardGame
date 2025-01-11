using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    public LevelDefinition currentLevel;
    private int currentStageIndex = 0;
    
    [Header("Events")]
    public UnityEvent<StageDefinition> onStageStart;
    public UnityEvent<StageDefinition> onStageComplete;
    public UnityEvent<LevelDefinition> onLevelComplete;
    public UnityEvent<StageObjective> onObjectiveProgress;

    private void Start()
    {
        if(currentLevel != null)
            StartStage(0);
    }

    public void StartStage(int stageIndex)
    {
        if (stageIndex >= currentLevel.stages.Count) return;

        currentStageIndex = stageIndex;
        var stage = currentLevel.stages[currentStageIndex];
        
        // Layout'u yükle
        LoadLayout(stage.layoutData);
        
        // Hedefleri sıfırla
        ResetObjectives(stage);
        
        onStageStart?.Invoke(stage);
    }

    private void ResetObjectives(StageDefinition stage)
    {
        foreach(var objective in stage.objectives)
        {
            objective.currentAmount = 0;
            objective.isCompleted = false;
        }
    }

    public void CheckStageProgress()
    {
        var currentStage = currentLevel.stages[currentStageIndex];
        bool allObjectivesComplete = true;

        foreach (var objective in currentStage.objectives)
        {
            if (!objective.isCompleted)
            {
                allObjectivesComplete = false;
                break;
            }
        }

        if (allObjectivesComplete)
        {
            CompleteStage();
        }
    }

    public void OnCardMatched(int cardID)
    {
        var stage = currentLevel.stages[currentStageIndex];
        foreach(var objective in stage.objectives)
        {
            switch(objective.type)
            {
                case ObjectiveType.CollectSpecificCards:
                    if(cardID == objective.specificCardID)
                    {
                        UpdateObjectiveProgress(objective, 1);
                    }
                    break;
                
                case ObjectiveType.CollectCardAmount:
                    UpdateObjectiveProgress(objective, 1);
                    break;

                case ObjectiveType.MatchPairs:
                    UpdateObjectiveProgress(objective, 1);
                    break;
            }
        }
        CheckStageProgress();
    }

    private void UpdateObjectiveProgress(StageObjective objective, int amount)
    {
        objective.currentAmount += amount;
        if(objective.currentAmount >= objective.targetAmount)
        {
            objective.isCompleted = true;
        }
        onObjectiveProgress?.Invoke(objective);
    }

    private void CompleteStage()
    {
        var currentStage = currentLevel.stages[currentStageIndex];
        currentStage.isCompleted = true;
        onStageComplete?.Invoke(currentStage);

        if (currentStageIndex >= currentLevel.stages.Count - 1)
        {
            CompleteLevel();
        }
        else
        {
            StartCoroutine(TransitionToNextStage());
        }
    }

    private IEnumerator TransitionToNextStage()
    {
        // Aşama geçiş animasyonu için bekleme
        yield return new WaitForSeconds(1.5f);
        StartStage(currentStageIndex + 1);
    }

    private void CompleteLevel()
    {
        onLevelComplete?.Invoke(currentLevel);
    }

    private void LoadLayout(LayoutData layout)
    {
        // Mevcut kart yerleştirme sisteminizi buraya entegre edin
    }
}