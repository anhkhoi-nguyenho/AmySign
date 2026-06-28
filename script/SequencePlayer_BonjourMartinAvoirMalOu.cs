using System.Collections;
using UnityEngine;

/// <summary>
/// Séquence complète : BONJOUR → M → A → R → T → I → N → AVOIR-MAL → OÙ
/// Avec transitions fluides entre chaque signe.
/// </summary>
public class SequencePlayer_BonjourMartinAvoirMalOu : MonoBehaviour
{
    [Header("Players des signes — assigner tous")]
    public BonjourUnityPlayer_1m60_Corrige_V7 bonjourPlayer;
    public LettreMUnityPlayer_1m60_V1 mPlayer;
    public LettreAUnityPlayer_1m60_V1 aPlayer;
    public LettreRUnityPlayer_1m60_V1 rPlayer;
    public LettreTUnityPlayer_1m60_V4 tPlayer;
    public LettreIUnityPlayer_1m60_V1 iPlayer;
    public LettreNUnityPlayer_1m60_V1 nPlayer;
    public AvoirMalUnityPlayer_1m60_V7 avoirMalPlayer;
    public OuUnityPlayer_1m60_V8 ouPlayer;

    [Header("Cibles IK (pour les transitions)")]
    public Transform rightHandTarget;
    public Transform rightElbowHint;
    public Transform leftHandTarget;
    public Transform leftElbowHint;
    public Transform avatarRoot;

    [Header("Durées d'affichage de chaque signe (secondes)")]
    public float bonjourDuration = 1.5f;
    public float letterDuration = 0.8f;
    public float avoirMalDuration = 1f;
    public float ouDuration = 1f;

    [Header("Transitions")]
    [Tooltip("Durée de la transition entre deux signes (secondes)")]
    public float transitionDuration = 0.4f;

    [Header("Lecture")]
    public bool playOnStart = true;

    // Positions cibles connues de chaque signe (relative à l'avatar)
    private readonly Vector3 letterHandPos = new Vector3(0.12f, 1.05f, 0.16f);
    private readonly Vector3 letterElbowPos = new Vector3(0.32f, 0.82f, 0.10f);

    private readonly Vector3 avoirMalStartHandPos = new Vector3(0.04f, 0.75f, 0.10f);
    private readonly Vector3 avoirMalStartElbowPos = new Vector3(0.26f, 0.72f, 0.04f);

    private readonly Vector3 ouStartHandPos = new Vector3(0.12f, 0.95f, 0.22f);
    private readonly Vector3 ouStartElbowPos = new Vector3(0.22f, 0.82f, 0.05f);

    // Main gauche pour OÙ
    private readonly Vector3 ouLeftHandPos = new Vector3(-0.12f, 0.95f, 0.22f);
    private readonly Vector3 ouLeftElbowPos = new Vector3(-0.22f, 0.82f, 0.05f);

    private void Start()
    {
        if (bonjourPlayer != null) bonjourPlayer.playOnStart = false;
        if (mPlayer != null) mPlayer.playOnStart = false;
        if (aPlayer != null) aPlayer.playOnStart = false;
        if (rPlayer != null) rPlayer.playOnStart = false;
        if (tPlayer != null) tPlayer.playOnStart = false;
        if (iPlayer != null) iPlayer.playOnStart = false;
        if (nPlayer != null) nPlayer.playOnStart = false;
        if (avoirMalPlayer != null) avoirMalPlayer.playOnStart = false;
        if (ouPlayer != null) ouPlayer.playOnStart = false;

        if (playOnStart) StartCoroutine(PlaySequence());
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlaySequence());
    }

    private void StopAllLetterPlayers()
    {
        if (bonjourPlayer != null) bonjourPlayer.StopAllCoroutines();
        if (mPlayer != null) mPlayer.StopAllCoroutines();
        if (aPlayer != null) aPlayer.StopAllCoroutines();
        if (rPlayer != null) rPlayer.StopAllCoroutines();
        if (tPlayer != null) tPlayer.StopAllCoroutines();
        if (iPlayer != null) iPlayer.StopAllCoroutines();
        if (nPlayer != null) nPlayer.StopAllCoroutines();
    }

    /// <summary>
    /// Transition fluide à UNE main.
    /// </summary>
    private IEnumerator SmoothTransition(Vector3 targetHandLocal, Vector3 targetElbowLocal)
    {
        if (rightHandTarget == null || avatarRoot == null) yield break;

        Vector3 startHand = rightHandTarget.position;
        Vector3 startElbow = rightElbowHint != null ? rightElbowHint.position : Vector3.zero;

        Vector3 endHand = avatarRoot.TransformPoint(targetHandLocal);
        Vector3 endElbow = avatarRoot.TransformPoint(targetElbowLocal);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            rightHandTarget.position = Vector3.Lerp(startHand, endHand, smoothT);
            if (rightElbowHint != null)
                rightElbowHint.position = Vector3.Lerp(startElbow, endElbow, smoothT);

            yield return null;
        }

        rightHandTarget.position = endHand;
        if (rightElbowHint != null) rightElbowHint.position = endElbow;
    }

    /// <summary>
    /// Transition fluide à DEUX mains (pour OÙ).
    /// </summary>
    private IEnumerator SmoothTransitionTwoHands(
        Vector3 rightHandLocal, Vector3 rightElbowLocal,
        Vector3 leftHandLocal, Vector3 leftElbowLocal)
    {
        if (avatarRoot == null) yield break;

        Vector3 startRightHand = rightHandTarget != null ? rightHandTarget.position : Vector3.zero;
        Vector3 startRightElbow = rightElbowHint != null ? rightElbowHint.position : Vector3.zero;
        Vector3 startLeftHand = leftHandTarget != null ? leftHandTarget.position : Vector3.zero;
        Vector3 startLeftElbow = leftElbowHint != null ? leftElbowHint.position : Vector3.zero;

        Vector3 endRightHand = avatarRoot.TransformPoint(rightHandLocal);
        Vector3 endRightElbow = avatarRoot.TransformPoint(rightElbowLocal);
        Vector3 endLeftHand = avatarRoot.TransformPoint(leftHandLocal);
        Vector3 endLeftElbow = avatarRoot.TransformPoint(leftElbowLocal);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (rightHandTarget != null)
                rightHandTarget.position = Vector3.Lerp(startRightHand, endRightHand, smoothT);
            if (rightElbowHint != null)
                rightElbowHint.position = Vector3.Lerp(startRightElbow, endRightElbow, smoothT);
            if (leftHandTarget != null)
                leftHandTarget.position = Vector3.Lerp(startLeftHand, endLeftHand, smoothT);
            if (leftElbowHint != null)
                leftElbowHint.position = Vector3.Lerp(startLeftElbow, endLeftElbow, smoothT);

            yield return null;
        }

        if (rightHandTarget != null) rightHandTarget.position = endRightHand;
        if (rightElbowHint != null) rightElbowHint.position = endRightElbow;
        if (leftHandTarget != null) leftHandTarget.position = endLeftHand;
        if (leftElbowHint != null) leftElbowHint.position = endLeftElbow;
    }

    private IEnumerator PlaySequence()
    {
        Debug.Log("[SÉQUENCE] Démarrage : Bonjour Martin avoir mal où ?");

        // ============== BONJOUR ==============
        Debug.Log("[SÉQUENCE] BONJOUR");
        if (bonjourPlayer != null) bonjourPlayer.PlayBonjour();
        yield return new WaitForSeconds(bonjourDuration);
        if (bonjourPlayer != null) bonjourPlayer.StopBonjour();

        // Transition BONJOUR → M
        yield return SmoothTransition(letterHandPos, letterElbowPos);

        // ============== M ==============
        Debug.Log("[SÉQUENCE] Lettre M");
        if (mPlayer != null) mPlayer.PlayLettreM();
        yield return new WaitForSeconds(letterDuration);
        if (mPlayer != null) mPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(0.1f);

        // ============== A ==============
        Debug.Log("[SÉQUENCE] Lettre A");
        if (aPlayer != null) aPlayer.PlayLettreA();
        yield return new WaitForSeconds(letterDuration);
        if (aPlayer != null) aPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(0.1f);

        // ============== R ==============
        Debug.Log("[SÉQUENCE] Lettre R");
        if (rPlayer != null) rPlayer.PlayLettreR();
        yield return new WaitForSeconds(letterDuration);
        if (rPlayer != null) rPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(0.1f);

        // ============== T ==============
        Debug.Log("[SÉQUENCE] Lettre T");
        if (tPlayer != null) tPlayer.PlayLettreT();
        yield return new WaitForSeconds(letterDuration);
        if (tPlayer != null) tPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(0.1f);

        // ============== I ==============
        Debug.Log("[SÉQUENCE] Lettre I");
        if (iPlayer != null) iPlayer.PlayLettreI();
        yield return new WaitForSeconds(letterDuration);
        if (iPlayer != null) iPlayer.StopAllCoroutines();
        yield return new WaitForSeconds(0.1f);

        // ============== N ==============
        Debug.Log("[SÉQUENCE] Lettre N");
        if (nPlayer != null) nPlayer.PlayLettreN();
        yield return new WaitForSeconds(letterDuration);
        if (nPlayer != null) nPlayer.StopAllCoroutines();

        StopAllLetterPlayers();

        // Transition N → AVOIR-MAL
        yield return SmoothTransition(avoirMalStartHandPos, avoirMalStartElbowPos);

        // ============== AVOIR-MAL ==============
        Debug.Log("[SÉQUENCE] AVOIR-MAL");
        if (avoirMalPlayer != null) avoirMalPlayer.PlayAvoirMal();
        yield return new WaitForSeconds(avoirMalDuration);
        if (avoirMalPlayer != null) avoirMalPlayer.StopAvoirMal();

        // Transition AVOIR-MAL → OÙ (deux mains)
        yield return SmoothTransitionTwoHands(
            ouStartHandPos, ouStartElbowPos,
            ouLeftHandPos, ouLeftElbowPos);

        // ============== OÙ ==============
        Debug.Log("[SÉQUENCE] OÙ");
        if (ouPlayer != null) ouPlayer.PlaySign();
        yield return new WaitForSeconds(ouDuration);

        Debug.Log("[SÉQUENCE] Phrase terminée !");
    }
}