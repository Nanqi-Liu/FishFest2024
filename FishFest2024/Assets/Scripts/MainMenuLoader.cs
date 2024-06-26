using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyTransition;

public class MainMenuLoader : MonoBehaviour
{
    [SerializeField]
    private TransitionSettings transitionSettings;

    private void Start()
    {
        AudioManager.instance.PlayMenuMusic();
    }

    public void LoadMainLevel()
    {
        TransitionManager.Instance().Transition("VerticalLevelSpawningDemo", transitionSettings, 0);
    }

    public void LoadOpenCinematic()
    {
        TransitionManager.Instance().Transition("OpenCinematic", transitionSettings, 0);
    }
}
