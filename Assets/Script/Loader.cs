using UnityEngine.SceneManagement;
public static class Loader
{
    public enum Scene
    {
        MainMenu,
        HomeExplore,
        EditDesign,
        AssetCustom,
        Reserve,
    }

    public static void Load(Scene targetScene)
    {
        SceneManager.LoadScene(targetScene.ToString());
    }
}