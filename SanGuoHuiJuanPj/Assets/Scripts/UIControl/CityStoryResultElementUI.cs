public class CityStoryResultElementUI : MiniWindowElementUI
{
    public ForceFlagUI flag;

    public void Init()
    {
        flag.Hide();
        image.gameObject.SetActive(true);
    }

    public void HideImage() => image.gameObject.SetActive(false);
}