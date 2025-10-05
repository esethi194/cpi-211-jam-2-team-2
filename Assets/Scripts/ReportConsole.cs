using UnityEngine;

public class ReportConsole
{
    public class ReportConsole : MonoBehaviour
    {
        public AnomalyManager manager;

        public void ReportRoomCategory(string roomId, int categoryIndex)
        {
            var cat = (AnomalyCategory)categoryIndex;
            bool correct = manager.CheckReport(roomId, cat, null);
            Debug.Log(correct ? "Correct report!" : "Incorrect report.");

        }

        public void ReportExact(string roomId, string anomalyId)
        {
            bool correct = manager.CheckReport(roomId, null, anomalyId);
            Debug.Log(correct ? "Correct report!" : "Incorrect report.");
        }
    }
}
