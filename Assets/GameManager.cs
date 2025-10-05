using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public int monstersCount = 0;
    public GameObject monster;
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
   

   
    
    //call this method to indicate when the player has to many anomalies present
    // The number is the state the monster will be in so 0 is inactive and 3 is game over
    public void RegisterMonsterState(int stateNumber)
    {
        monstersCount = stateNumber;
        
        UpdateMonsterState();
    }

    private void UpdateMonsterState()
    {
        if (monster != null) return;

        if (monstersCount == 1)
        {
            monster.gameObject.GetComponent<MeshRenderer>().enabled = true;
            monster.GetComponent<MonsterAI>().ChangeState(new RoamingState());
        }
        else if (monstersCount == 2)
        {
            monster.gameObject.GetComponent<MeshRenderer>().enabled = true;
            monster.GetComponent<MonsterAI>().ChangeState(new StalkingState());
        }
        else if (monstersCount >= 3)
        {
            monster.gameObject.GetComponent<MeshRenderer>().enabled = true;
            monster.GetComponent<MonsterAI>().ChangeState(new ChargeState());
        }
        
    }

    public void GameOver()
    {
        // game over code
        Debug.Log("Game Over");
    }
    
    
}
