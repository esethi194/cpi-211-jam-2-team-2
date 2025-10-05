using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public int monstersCount = 0;
    public MonsterAI monster;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // delete this once anomoly system works 
    public void Update()
    {
        UpdateMonsterState();
    }

   
    
    //call this method to indicate when the player has to many anomalies present
    public void RegisterMonsterState()
    {
        monstersCount++;
        
        UpdateMonsterState();
    }

    private void UpdateMonsterState()
    {
        if (monster != null) return;

        if (monstersCount == 1)
        {
            monster.ChangeState(new RoamingState());
        }
        else if (monstersCount == 2)
        {
            monster.ChangeState(new StalkingState());
        }
        else if (monstersCount >= 3)
        {
            monster.ChangeState(new ChargeState());
        }
        
    }

    public void GameOver()
    {
        // game over code
    }
    
    
}
