using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public void OnPlayButtonPressed()
    {
        SceneController.Instance
            .NewTransition()
            .Unload(SceneDatabase.Slots.MainMenu)
            .Load(SceneDatabase.Slots.Session, SceneDatabase.Scenes.Game)
            .WithOverlay()
            .Perform();
    }
}
