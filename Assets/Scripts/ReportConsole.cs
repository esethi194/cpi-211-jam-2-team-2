using UnityEngine;

namespace Contrast
{
    public class ReportConsole : MonoBehaviour
    {
        public AnomalyManager manager;

        // Example bindings from UI dropdowns/buttons:
        public void ReportRoomCategory(string roomId, int categoryIndex)
        {
            var cat = (AnomalyCategory)categoryIndex;
            bool correct = manager.CheckReport(roomId, cat, null);
            Debug.Log(correct ? "Report correct." : "Report false.");
        }

        public void ReportExact(string roomId, string anomalyId)
        {
            bool correct = manager.CheckReport(roomId, null, anomalyId);
            Debug.Log(correct ? "Report correct." : "Report false.");
        }
    }
}
