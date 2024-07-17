using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundsController : MonoBehaviour
{
    public List<AudioClip> listMusic;
    public AudioSource audioSrc;

    void Start()
    {
        // Commencez par jouer une musique aléatoire dès le départ
        PlayRandomMusic();
    }

    void Update()
    {
        // Si la musique actuelle est terminée, jouez une nouvelle musique aléatoire
        if (!audioSrc.isPlaying)
        {
            PlayRandomMusic();
        }
    }

    void PlayRandomMusic()
    {
        if (listMusic.Count > 0)
        {
            // Sélectionnez un indice aléatoire dans la liste de musiques
            int randomIndex = Random.Range(0, listMusic.Count);
            // Assignez la musique à l'AudioSource et jouez-la
            audioSrc.clip = listMusic[randomIndex];
            audioSrc.Play();
        }
    }
}
