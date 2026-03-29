using UnityEngine;

public class LoadingSceneCallBack : MonoBehaviour
{
    private bool isFirstUpdate = true;
    private void Update()
    {
        if (isFirstUpdate)
        {
            isFirstUpdate = false;
            Loader.LoaderCallBack();
        }
    }
}
