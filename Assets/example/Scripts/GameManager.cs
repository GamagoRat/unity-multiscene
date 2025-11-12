using UnityEngine;

public class GameManager : MonoBehaviour
{
    public void OnQuitButtonPressed()
    {
        SceneController.Instance.NewTransition()
            .Unload(SceneDatabase.Slots.Session)
            .Load(SceneDatabase.Slots.MainMenu, SceneDatabase.Scenes.MainMenu, true)
            .WithOverlay()
            .WithClearUnusedAssets()
            .Perform();
    }
}
