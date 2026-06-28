using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe COEUR en LSF.
/// Les deux mains forment un cœur devant le torse.
/// Doigts courbés vers le haut qui se touchent, pouces qui se touchent en bas.
/// </summary>
public class COEUR : MonoBehaviour
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
    public float duration = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Positions main droite")]
    public Vector3 rightHandFinal = new Vector3(0.05f, 1.10f, 0.25f);
    public Vector3 rightElbowPos = new Vector3(0.30f, 0.85f, 0.05f);

    [Header("Positions main gauche")]
    public Vector3 leftHandFinal = new Vector3(-0.05f, 1.10f, 0.25f);
    public Vector3 leftElbowPos = new Vector3(-0.30f, 0.85f, 0.05f);

    [Header("Poignets — inclinés pour former le coeur")]
    public bool applyWristRotation = true;
    [Tooltip("Main droite inclinée vers la gauche, pouce vers le bas")]
    public Vector3 rightWristEuler = new Vector3(-45f, -180f, 45f);
    [Tooltip("Main gauche inclinée vers la droite, pouce vers le bas")]
    public Vector3 leftWristEuler = new Vector3(-45f, 0f, -45f);
    public bool useLocalRotation = true;

    [Header("Doigts — courbés vers le haut")]
    public bool forceManualHandshape = true;

    [Header("Index — courbé")]
    public Vector3 indexPh1 = new Vector3(50f, 0f, 0f);
    public Vector3 indexPh2 = new Vector3(60f, 0f, 0f);
    public Vector3 indexPh3 = new Vector3(30f, 0f, 0f);

    [Header("Majeur — courbé")]
    public Vector3 middlePh1 = new Vector3(50f, 0f, 0f);
    public Vector3 middlePh2 = new Vector3(60f, 0f, 0f);
    public Vector3 middlePh3 = new Vector3(30f, 0f, 0f);

    [Header("Annulaire — courbé")]
    public Vector3 ringPh1 = new Vector3(50f, 0f, 0f);
    public Vector3 ringPh2 = new Vector3(60f, 0f, 0f);
    public Vector3 ringPh3 = new Vector3(30f, 0f, 0f);

    [Header("Auriculaire — courbé")]
    public Vector3 pinkyPh1 = new Vector3(50f, 0f, 0f);
    public Vector3 pinkyPh2 = new Vector3(60f, 0f, 0f);
    public Vector3 pinkyPh3 = new Vector3(30f, 0f, 0f);

    [Header("Pouce DROITE — vers le bas pour la pointe")]
    public Vector3 rightThumbPh1 = new Vector3(30f, 0f, 50f);
    public Vector3 rightThumbPh2 = new Vector3(20f, 0f, 20f);
    public Vector3 rightThumbPh3 = new Vector3(10f, 0f, 0f);

    [Header("Pouce GAUCHE — vers le bas pour la pointe")]
    public Vector3 leftThumbPh1 = new Vector3(30f, 0f, 50f);
    public Vector3 leftThumbPh2 = new Vector3(20f, 0f, 20f);
    public Vector3 leftThumbPh3 = new Vector3(10f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayCoeur();
    }

    public void PlayCoeur()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayCoeurCoroutine());
    }

    private IEnumerator PlayCoeurCoroutine()
    {
        if (rightHandTarget == null || leftHandTarget == null) { Debug.LogError("[COEUR] Targets non assignés."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[COEUR] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[COEUR] Démarrage.");

        ApplyGaze();
        ApplyArmPose();

        yield return null;

        ApplyFullPose();

        while (true)
        {
            ApplyArmPose();
            ApplyFullPose();
            yield return null;
        }
    }

    private void ApplyArmPose()
    {
        SetTargetPosition(rightHandTarget, CorrectPosition(rightHandFinal));
        SetTargetPosition(leftHandTarget, CorrectPosition(leftHandFinal));
        if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
        if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
    }

    private void ApplyFullPose()
    {
        ApplyWristRotations();
        if (forceManualHandshape) UpdateFingerRotations();
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

    private void UpdateFingerRotations()
    {
        if (avatarRoot == null) return;

        // Main droite
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
        ApplyBone("mixamorig:RightHandThumb1", Quaternion.Euler(rightThumbPh1));
        ApplyBone("mixamorig:RightHandThumb2", Quaternion.Euler(rightThumbPh2));
        ApplyBone("mixamorig:RightHandThumb3", Quaternion.Euler(rightThumbPh3));

        // Main gauche
        ApplyBone("mixamorig:LeftHandIndex1", Quaternion.Euler(indexPh1));
        ApplyBone("mixamorig:LeftHandIndex2", Quaternion.Euler(indexPh2));
        ApplyBone("mixamorig:LeftHandIndex3", Quaternion.Euler(indexPh3));
        ApplyBone("mixamorig:LeftHandMiddle1", Quaternion.Euler(middlePh1));
        ApplyBone("mixamorig:LeftHandMiddle2", Quaternion.Euler(middlePh2));
        ApplyBone("mixamorig:LeftHandMiddle3", Quaternion.Euler(middlePh3));
        ApplyBone("mixamorig:LeftHandRing1", Quaternion.Euler(ringPh1));
        ApplyBone("mixamorig:LeftHandRing2", Quaternion.Euler(ringPh2));
        ApplyBone("mixamorig:LeftHandRing3", Quaternion.Euler(ringPh3));
        ApplyBone("mixamorig:LeftHandPinky1", Quaternion.Euler(pinkyPh1));
        ApplyBone("mixamorig:LeftHandPinky2", Quaternion.Euler(pinkyPh2));
        ApplyBone("mixamorig:LeftHandPinky3", Quaternion.Euler(pinkyPh3));
        ApplyBone("mixamorig:LeftHandThumb1", Quaternion.Euler(leftThumbPh1));
        ApplyBone("mixamorig:LeftHandThumb2", Quaternion.Euler(leftThumbPh2));
        ApplyBone("mixamorig:LeftHandThumb3", Quaternion.Euler(leftThumbPh3));
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
