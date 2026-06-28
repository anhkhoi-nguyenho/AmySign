using System.Collections;
using UnityEngine;

/// <summary>
/// Script JEUX_VIDEO basé STRICTEMENT sur la V1 d'origine.
/// Correction : Inversion de l'axe du pouce gauche pour qu'il tape vers le haut/bas
/// (sur le dessus de l'index) au lieu de rentrer à l'intérieur de la main.
/// </summary>
public class JEUX_VIDEO : MonoBehaviour
{
    public enum TargetPositionSpace
    {
        LocalToTargetParent,
        World,
        RelativeToAvatarRoot
    }

    [Header("Cibles IK droite")]
    public Transform rightHandTarget;
    public Transform rightElbowHint;

    [Header("Cibles IK gauche")]
    public Transform leftHandTarget;
    public Transform leftElbowHint;

    [Header("Tête / regard")]
    public Transform headTarget;

    [Header("Référence avatar")]
    public Transform avatarRoot;

    [Header("Espace des positions")]
    public TargetPositionSpace targetPositionSpace = TargetPositionSpace.RelativeToAvatarRoot;

    [Header("Lecture")]
    public bool playOnStart = true;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Positions fixes des mains (Version 1 Origine)")]
    public Vector3 rightHandPos = new Vector3(0.16f, 0.92f, 0.22f);
    public Vector3 rightElbowPos = new Vector3(0.35f, 0.75f, -0.05f);

    public Vector3 leftHandPos = new Vector3(-0.16f, 0.92f, 0.22f);
    public Vector3 leftElbowPos = new Vector3(-0.35f, 0.75f, -0.05f);

    [Header("Poignets — Version 1 Origine")]
    public bool applyWristRotation = true;
    public Vector3 rightWristEuler = new Vector3(0f, 120f, 90f);
    public Vector3 leftWristEuler = new Vector3(0f, -120f, -90f);
    public bool useLocalRotation = true;

    [Header("Paramètres d'animation des pouces")]
    public float clickSpeed = 25.0f;
    public float clickAmplitude = 30.0f;
    [Tooltip("Axe d'appui (Z fait descendre le droit, et sera inversé pour le gauche)")]
    public Vector3 thumbClickAxis = new Vector3(0f, 0f, 1f);

    [Header("Doigts — Version 1 Origine (Index à Auriculaire)")]
    public bool forceManualHandshape = true;

    private Vector3 fingersFoldedPh1 = new Vector3(80f, 0f, 0f);
    private Vector3 fingersFoldedPh2 = new Vector3(90f, 0f, 0f);
    private Vector3 fingersFoldedPh3 = new Vector3(70f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayJeuxVideo();
    }

    public void PlayJeuxVideo()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayJeuxVideoCoroutine());
    }

    private IEnumerator PlayJeuxVideoCoroutine()
    {
        if (rightHandTarget == null || leftHandTarget == null) yield break;
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) yield break;

        if (showDebugLogs) Debug.Log("[JEUX_VIDEO] V1 avec correction d'inversion d'axe sur le pouce gauche.");

        ApplyGaze();

        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime * globalSpeedMultiplier;

            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandPos));
            SetTargetPosition(leftHandTarget, CorrectPosition(leftHandPos));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));

            ApplyWristRotations();

            if (forceManualHandshape)
            {
                UpdateStaticFingers();

                // Animation asymétrique (désynchronisée)
                float rightWave = (Mathf.Sin(timer * clickSpeed) + 1f) * 0.5f;
                float leftWave = (Mathf.Sin(timer * clickSpeed + Mathf.PI) + 1f) * 0.5f;

                // Calcul de la rotation de base pour le côté droit
                Vector3 currentRightThumbRot = thumbClickAxis * (rightWave * clickAmplitude);
                
                // Pour le côté gauche, on inverse la valeur de l'axe (-currentLeftThumbRot) 
                // pour compenser l'inversion du squelette Mixamo et éviter qu'il rentre dans la paume
                Vector3 currentLeftThumbRot = thumbClickAxis * (leftWave * clickAmplitude);
                Vector3 currentLeftThumbInverted = -currentLeftThumbRot;

                // Application Pouce Droit (Parfait)
                ApplyBone("mixamorig:RightHandThumb1", Quaternion.Euler(currentRightThumbRot));
                ApplyBone("mixamorig:RightHandThumb2", Quaternion.Euler(currentRightThumbRot * 0.8f));

                // Application Pouce Gauche (Corrigé et inversé pour taper sur le dessus)
                ApplyBone("mixamorig:LeftHandThumb1", Quaternion.Euler(currentLeftThumbInverted));
                ApplyBone("mixamorig:LeftHandThumb2", Quaternion.Euler(currentLeftThumbInverted * 0.8f));
            }

            yield return null;
        }
    }

    private void ApplyWristRotations()
    {
        if (!applyWristRotation) return;

        if (rightHandTarget != null)
        {
            Quaternion r = Quaternion.Euler(rightWristEuler);
            if (useLocalRotation) rightHandTarget.localRotation = r;
            else                  rightHandTarget.rotation = r;
        }

        if (leftHandTarget != null)
        {
            Quaternion l = Quaternion.Euler(leftWristEuler);
            if (useLocalRotation) leftHandTarget.localRotation = l;
            else                  leftHandTarget.rotation = l;
        }
    }

    private void UpdateStaticFingers()
    {
        if (avatarRoot == null) return;

        string[] sides = { "Right", "Left" };
        foreach (string side in sides)
        {
            ApplyBone($"mixamorig:{side}HandIndex1", Quaternion.Euler(fingersFoldedPh1));
            ApplyBone($"mixamorig:{side}HandIndex2", Quaternion.Euler(fingersFoldedPh2));
            ApplyBone($"mixamorig:{side}HandIndex3", Quaternion.Euler(fingersFoldedPh3));

            ApplyBone($"mixamorig:{side}HandMiddle1", Quaternion.Euler(fingersFoldedPh1));
            ApplyBone($"mixamorig:{side}HandMiddle2", Quaternion.Euler(fingersFoldedPh2));
            ApplyBone($"mixamorig:{side}HandMiddle3", Quaternion.Euler(fingersFoldedPh3));

            ApplyBone($"mixamorig:{side}HandRing1", Quaternion.Euler(fingersFoldedPh1));
            ApplyBone($"mixamorig:{side}HandRing2", Quaternion.Euler(fingersFoldedPh2));
            ApplyBone($"mixamorig:{side}HandRing3", Quaternion.Euler(fingersFoldedPh3));

            ApplyBone($"mixamorig:{side}HandPinky1", Quaternion.Euler(fingersFoldedPh1));
            ApplyBone($"mixamorig:{side}HandPinky2", Quaternion.Euler(fingersFoldedPh2));
            ApplyBone($"mixamorig:{side}HandPinky3", Quaternion.Euler(fingersFoldedPh3));
        }
    }

    private void ApplyBone(string boneName, Quaternion rotation)
    {
        Transform bone = FindDeepChild(avatarRoot, boneName);
        if (bone != null) bone.localRotation = rotation;
    }

    private void ApplyGaze()
    {
        if (headTarget == null) return;
        Vector3 gaze = CorrectPosition(gazeTarget);
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot != null)
            gaze = avatarRoot.TransformPoint(gaze);
        Vector3 direction = gaze - headTarget.position;
        if (direction.sqrMagnitude > 0.001f)
            headTarget.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private Vector3 CorrectPosition(Vector3 p) => p * positionScale + positionOffset;

    private void SetTargetPosition(Transform target, Vector3 position)
    {
        if (target == null) return;
        switch (targetPositionSpace)
        {
            case TargetPositionSpace.LocalToTargetParent:
                target.localPosition = position; break;
            case TargetPositionSpace.World:
                target.position = position; break;
            case TargetPositionSpace.RelativeToAvatarRoot:
                target.position = avatarRoot != null
                    ? avatarRoot.TransformPoint(position) : position;
                break;
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}