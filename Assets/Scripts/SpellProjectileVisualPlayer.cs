using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SpellProjectileVisualPlayer : MonoBehaviour
{
    private PlayableGraph graph;

    public void Play(AnimationClip clip)
    {
        if (clip == null)
        {
            return;
        }

        Animator animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        if (graph.IsValid())
        {
            graph.Destroy();
        }

        AnimationPlayableUtilities.PlayClip(animator, clip, out graph);
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
    }

    private void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
