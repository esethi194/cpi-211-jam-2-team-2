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

        monster.GetComponent<MonsterAI>().UpdateMonsterState();
    }

    

    public void GameOver()
    {
        // game over code
        Debug.Log("Game Over");
    }
    
    
}
