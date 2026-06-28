using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour la lettre Z en LSF.
/// L'index dessine un Z dans l'air en 3 segments :
/// 1) Trait horizontal vers la droite
/// 2) Diagonale vers bas-gauche
/// 3) Trait horizontal vers la droite
/// </summary>
public class Z : MonoBehaviour
{
    public enum TargetPositionSpace
    {
        LocalToTargetParent,
        World,
        RelativeToAvatarRoot
    }

    [Header("Cibles IK")]
    public Transform rightHandTarget;
    public Transform rightElbowHint;
    public Transform headTarget;

    [Header("Référence avatar")]
    public Transform avatarRoot;

    [Header("Espace des positions")]
    public TargetPositionSpace targetPositionSpace = TargetPositionSpace.RelativeToAvatarRoot;

    [Header("Lecture")]
    public bool playOnStart = true;
    public float duration = 2.0f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Position lettre Z")]
    [Tooltip("4 points du Z : haut-gauche → haut-droite → bas-gauche → bas-droite")]
    public Vector3 pointHautGauche  = new Vector3(0.06f, 1.15f, 0.16f);
    public Vector3 pointHautDroite  = new Vector3(0.18f, 1.15f, 0.16f);
    public Vector3 pointBasGauche   = new Vector3(0.06f, 0.95f, 0.16f);
    public Vector3 pointBasDroite   = new Vector3(0.18f, 0.95f, 0.16f);
    public Vector3 elbowFinalPosition = new Vector3(0.32f, 0.82f, 0.10f);

    [Header("Poignet — paume vers la caméra")]
    public bool applyWristRotation = true;
    public Vector3 wristRotationEuler = new Vector3(0f, 0f, 0f);
    public bool useLocalRotation = true;

    [Header("Doigts")]
    public bool forceManualHandshape = true;

    [Header("Index — tendu")]
    public Vector3 indexPh1 = new Vector3(-20f, 0f, 0f);
    public Vector3 indexPh2 = new Vector3(-15f, 0f, 0f);
    public Vector3 indexPh3 = new Vector3(-10f, 0f, 0f);

    [Header("Majeur — replié")]
    public Vector3 middlePh1 = new Vector3(80f, 0f, 0f);
    public Vector3 middlePh2 = new Vector3(90f, 0f, 0f);
    public Vector3 middlePh3 = new Vector3(70f, 0f, 0f);

    [Header("Annulaire — replié")]
    public Vector3 ringPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 ringPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 ringPh3 = new Vector3(70f, 0f, 0f);

    [Header("Auriculaire — replié")]
    public Vector3 pinkyPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 pinkyPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 pinkyPh3 = new Vector3(70f, 0f, 0f);

    [Header("Pouce — posé sur le majeur")]
    public Vector3 thumbPh1 = new Vector3(40f, 20f, 50f);
    public Vector3 thumbPh2 = new Vector3(40f, 0f, 30f);
    public Vector3 thumbPh3 = new Vector3(20f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayZ();
    }

    public void PlayZ()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayZCoroutine());
    }

    private IEnumerator PlayZCoroutine()
    {
        if (rightHandTarget == null) { Debug.LogError("[Z] RightHandTarget non assigné."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[Z] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[Z] Démarrage : tracé du Z.");

        ApplyGaze();

        // Trajectoire du Z : 4 points pour 3 segments
        Vector3[] path = new Vector3[]
        {
            pointHautGauche,
            pointHautDroite,
            pointBasGauche,
            pointBasDroite
        };

        int segmentCount = path.Length - 1;
        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);
        float realDuration = duration / safeSpeed;
        float segmentDuration = realDuration / segmentCount;

        // Position initiale
        SetTargetPosition(rightHandTarget, CorrectPosition(path[0]));
        if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(elbowFinalPosition));
        if (applyWristRotation) ApplyWristRot();
        if (forceManualHandshape) UpdateFingerRotations();

        yield return new WaitForSeconds(0.2f);

        // Tracer les 3 segments du Z
        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 startPos = path[i];
            Vector3 endPos = path[i + 1];

            if (showDebugLogs)
            {
                string segName = i == 0 ? "Trait horizontal haut" : (i == 1 ? "Diagonale" : "Trait horizontal bas");
                Debug.Log("[Z] Segment " + (i + 1) + " : " + segName);
            }

            float elapsed = 0f;
            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / segmentDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothT);
                SetTargetPosition(rightHandTarget, CorrectPosition(currentPos));
                if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(elbowFinalPosition));
                if (applyWristRotation) ApplyWristRot();
                if (forceManualHandshape) UpdateFingerRotations();

                yield return null;
            }
        }

        // Pose maintenue à la fin
        while (true)
        {
            SetTargetPosition(rightHandTarget, CorrectPosition(pointBasDroite));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(elbowFinalPosition));
            if (applyWristRotation) ApplyWristRot();
            if (forceManualHandshape) UpdateFingerRotations();
            yield return null;
        }
    }

    private void UpdateFingerRotations()
    {
        if (avatarRoot == null) return;

        ApplyBone("mixamorig:RightHandIndex1", Quaternion.Euler(indexPh1));
        ApplyBone("mixamorig:RightHandIndex2", Quaternion.Euler(indexPh2));
        ApplyBone("mixamorig:RightHandIndex3", Quaternion.Euler(indexPh3));

        ApplyBone("mixamorig:RightHandMiddle1", Quaternion.Euler(middlePh1));
        ApplyBone("mixamorig:RightHandMiddle2", Quaternion.Euler(middlePh2));
        ApplyBone("mixamorig:RightHandMiddle3", Quaternion.Euler(middlePh3));

        ApplyBone("mixamorig:RightHandRing1", Quaternion.Euler(ringPh1));
        ApplyBone("mixamorig:RightHandRing2", Quaternion.Euler(ringPh2));
        ApplyBone("mixamorig:RightHandRing3", Quaternion.Euler(ringPh3));

        ApplyBone("mixamorig:RightHandPinky1", Quaternion.Euler(pinkyPh1));
        ApplyBone("mixamorig:RightHandPinky2", Quaternion.Euler(pinkyPh2));
        ApplyBone("mixamorig:RightHandPinky3", Quaternion.Euler(pinkyPh3));

        ApplyBone("mixamorig:RightHandThumb1", Quaternion.Euler(thumbPh1));
        ApplyBone("mixamorig:RightHandThumb2", Quaternion.Euler(thumbPh2));
        ApplyBone("mixamorig:RightHandThumb3", Quaternion.Euler(thumbPh3));
    }

    private void ApplyBone(string boneName, Quaternion rotation)
    {
        Transform bone = FindDeepChild(avatarRoot, boneName);
        if (bone != null) bone.localRotation = rotation;
    }

    private void ApplyWristRot()
    {
        if (rightHandTarget == null) return;
        Quaternion r = Quaternion.Euler(wristRotationEuler);
        if (useLocalRotation) rightHandTarget.localRotation = r;
        else                  rightHandTarget.rotation = r;
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
