using UnityEngine;
using UnityEngine.InputSystem;

public class TestAI : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction state1Action;
    private InputAction state2Action;
    private InputAction state3Action;

    private void Awake()
    {
        // Add a PlayerInput component to the GameObject this script is on
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("StateTester requires a PlayerInput component on the same GameObject.");
            return;
        }

        // --- Setup Actions from the Default Map (you might need to adjust map/action names) ---
        
        // Assuming you have '1', '2', '3' keys bound to some actions, 
        // or you can manually create the bindings in the PlayerInput component.
        
        // This attempts to get generic actions named 'State1', 'State2', etc.
        // If your actions are named differently, change the string.
        state1Action = playerInput.actions.FindAction("State1");
        state2Action = playerInput.actions.FindAction("State2");
        state3Action = playerInput.actions.FindAction("State3");
        
        // FALLBACK: If you don't have named actions, you can manually bind keys:
        if (state1Action == null)
        {
             // Creates a temporary action for the '1' key
             state1Action = new InputAction(binding: "<Keyboard>/1", type: InputActionType.Button);
             state1Action.Enable();
        }
        if (state2Action == null)
        {
             state2Action = new InputAction(binding: "<Keyboard>/2", type: InputActionType.Button);
             state2Action.Enable();
        }
        if (state3Action == null)
        {
             state3Action = new InputAction(binding: "<Keyboard>/3", type: InputActionType.Button);
             state3Action.Enable();
        }
    }

    private void OnEnable()
    {
        if (state1Action != null) state1Action.performed += ctx => OnState1Pressed();
        if (state2Action != null) state2Action.performed += ctx => OnState2Pressed();
        if (state3Action != null) state3Action.performed += ctx => OnState3Pressed();
    }

    private void OnDisable()
    {
        if (state1Action != null) state1Action.performed -= ctx => OnState1Pressed();
        if (state2Action != null) state2Action.performed -= ctx => OnState2Pressed();
        if (state3Action != null) state3Action.performed -= ctx => OnState3Pressed();
    }

    private void OnState1Pressed()
    {
        Debug.Log("Forcing Monster to State 1 (Roaming)");
        GameManager.instance.RegisterMonsterState(1);
    }

    private void OnState2Pressed()
    {
        Debug.Log("Forcing Monster to State 2 (Stalking)");
        GameManager.instance.RegisterMonsterState(2);
    }

    private void OnState3Pressed()
    {
        Debug.Log("Forcing Monster to State 3 (Charge) - GAME OVER");
        GameManager.instance.RegisterMonsterState(3);
    }
}