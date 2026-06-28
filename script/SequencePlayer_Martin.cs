using System.Collections;
using UnityEngine;

/// <summary>
/// Séquence pour épeler MARTIN : M → A → R → T → I → N
///
/// UTILISATION :
/// 1. Créer un objet vide "MartinPlayer"
/// 2. Glisser ce script dessus
/// 3. Assigner les 6 players de lettres dans l'inspecteur
/// 4. Décocher Play On Start sur tous les players individuels
/// 5. Lancer le Play
/// </summary>
public class SequencePlayer_Martin : MonoBehaviour
{
    [Header("Players des lettres")]
    public LettreMUnityPlayer_1m60_V1 mPlayer;
    public LettreAUnityPlayer_1m60_V1 aPlayer;
    public LettreRUnityPlayer_1m60_V1 rPlayer;
    public LettreTUnityPlayer_1m60_V4 tPlayer;
    public LettreIUnityPlayer_1m60_V1 iPlayer;
    public LettreNUnityPlayer_1m60_V1 nPlayer;

    [Header("Durée d'affichage de chaque lettre (secondes)")]
    public float letterDuration = 0.8f;

    [Header("Pause entre les lettres (secondes)")]
    public float pauseBetweenLetters = 0.15f;

    [Header("Lecture")]
    public bool playOnStart = true;

    private void Start()
    {
        // Désactiver l'auto-lancement de toutes les lettres
        if (mPlayer != null) mPlayer.playOnStart = false;
        if (aPlayer != null) aPlayer.playOnStart = false;
        if (rPlayer != null) rPlayer.playOnStart = false;
        if (tPlayer != null) tPlayer.playOnStart = false;
        if (iPlayer != null) iPlayer.playOnStart = false;
        if (nPlayer != null) nPlayer.playOnStart = false;

        if (playOnStart) StartCoroutine(PlayMartin());
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlayMartin());
    }

    private IEnumerator PlayMartin()
    {
        Debug.Log("[MARTIN] Démarrage de l'épellation.");

        // M
        Debug.Log("[MARTIN] Lettre M");
        if (mPlayer != null) mPlayer.PlayLettreM();
        yield return new WaitForSeconds(letterDuration);
        if (mPlayer != null) mPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(pauseBetweenLetters);

        // A
        Debug.Log("[MARTIN] Lettre A");
        if (aPlayer != null) aPlayer.PlayLettreA();
        yield return new WaitForSeconds(letterDuration);
        if (aPlayer != null) aPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(pauseBetweenLetters);

        // R
        Debug.Log("[MARTIN] Lettre R");
        if (rPlayer != null) rPlayer.PlayLettreR();
        yield return new WaitForSeconds(letterDuration);
        if (rPlayer != null) rPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(pauseBetweenLetters);

        // T
        Debug.Log("[MARTIN] Lettre T");
        if (tPlayer != null) tPlayer.PlayLettreT();
        yield return new WaitForSeconds(letterDuration);
        if (tPlayer != null) tPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(pauseBetweenLetters);

        // I
        Debug.Log("[MARTIN] Lettre I");
        if (iPlayer != null) iPlayer.PlayLettreI();
        yield return new WaitForSeconds(letterDuration);
        if (iPlayer != null) iPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(pauseBetweenLetters);

        // N
        Debug.Log("[MARTIN] Lettre N");
        if (nPlayer != null) nPlayer.PlayLettreN();
        yield return new WaitForSeconds(letterDuration);

        Debug.Log("[MARTIN] Épellation terminée !");
    }
}
