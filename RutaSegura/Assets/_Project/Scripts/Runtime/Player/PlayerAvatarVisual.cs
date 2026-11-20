using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Runtime.Player
{
    /// <summary>
    /// Avatar procedural del protagonista (1.76 m) para vista en 3ra persona y sombras en 1ra persona.
    /// Construye extremidades articuladas sin colliders propios y las anima segun locomocion,
    /// postura cautelosa y consulta del telefono.
    /// </summary>
    [AddComponentMenu("Ruta Segura/Player/Player Avatar Visual")]
    public class PlayerAvatarVisual : MonoBehaviour
    {
        private PlayerController player;
        private Transform modelRoot;
        private Transform upperBodyPivot;
        private Transform headTransform;
        private Transform armPivotL;
        private Transform armPivotR;
        private Transform legPivotL;
        private Transform legPivotR;

        private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        private readonly List<Material> createdMaterials = new List<Material>();

        private float walkCycle;
        private bool currentVisible = true;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            BuildAvatarModel();
            SetVisibleInCamera(false);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < createdMaterials.Count; i++)
            {
                if (createdMaterials[i] != null)
                {
                    Destroy(createdMaterials[i]);
                }
            }
            createdMaterials.Clear();
        }

        private void BuildAvatarModel()
        {
            Transform existing = transform.Find("Protagonist_Model");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var rootGo = new GameObject("Protagonist_Model");
            modelRoot = rootGo.transform;
            modelRoot.SetParent(transform, false);
            modelRoot.localPosition = Vector3.zero;
            modelRoot.localRotation = Quaternion.identity;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material matJacket = CreateMat(shader, new Color(0.14f, 0.19f, 0.28f), 0.25f);
            Material matJeans = CreateMat(shader, new Color(0.14f, 0.17f, 0.24f), 0.20f);
            Material matPack = CreateMat(shader, new Color(0.09f, 0.10f, 0.12f), 0.30f);
            Material matSkin = CreateMat(shader, new Color(0.62f, 0.45f, 0.34f), 0.28f);
            Material matHair = CreateMat(shader, new Color(0.07f, 0.06f, 0.06f), 0.30f);
            Material matShoes = CreateMat(shader, new Color(0.10f, 0.10f, 0.11f), 0.35f);

            // Cadera base a -0.18m (piernas bajan hasta -1.00m = piso)
            var pelvis = CreateLimb("Pelvis", modelRoot, new Vector3(0f, -0.18f, 0f), new Vector3(0.32f, 0.14f, 0.20f), matJeans);

            // Pivote de torso superior (desde cadera hacia cuello +0.46m -> Y +0.28m..+0.42m)
            var upperGo = new GameObject("UpperBody_Pivot");
            upperBodyPivot = upperGo.transform;
            upperBodyPivot.SetParent(modelRoot, false);
            upperBodyPivot.localPosition = new Vector3(0f, -0.14f, 0f);

            CreateLimb("Torso_Jacket", upperBodyPivot, new Vector3(0f, 0.26f, 0f), new Vector3(0.38f, 0.50f, 0.22f), matJacket);
            CreateLimb("Backpack", upperBodyPivot, new Vector3(0f, 0.28f, -0.17f), new Vector3(0.28f, 0.38f, 0.14f), matPack);

            // Cabeza y cabello (cima a localY ~ +0.76m desde centro del jugador => altura total 1.76m)
            var head = CreateLimb("Head", upperBodyPivot, new Vector3(0f, 0.68f, 0f), new Vector3(0.21f, 0.24f, 0.22f), matSkin);
            headTransform = head.transform;
            CreateLimb("Hair", headTransform, new Vector3(0f, 0.06f, -0.01f), new Vector3(1.06f, 0.65f, 1.06f), matHair);

            // Brazos desde hombros
            armPivotL = CreatePivot("ArmPivot_L", upperBodyPivot, new Vector3(-0.24f, 0.48f, 0f));
            CreateLimb("Arm_L", armPivotL, new Vector3(0f, -0.24f, 0f), new Vector3(0.11f, 0.48f, 0.11f), matJacket);
            CreateLimb("Hand_L", armPivotL, new Vector3(0f, -0.51f, 0f), new Vector3(0.09f, 0.11f, 0.09f), matSkin);

            armPivotR = CreatePivot("ArmPivot_R", upperBodyPivot, new Vector3(0.24f, 0.48f, 0f));
            CreateLimb("Arm_R", armPivotR, new Vector3(0f, -0.24f, 0f), new Vector3(0.11f, 0.48f, 0.11f), matJacket);
            CreateLimb("Hand_R", armPivotR, new Vector3(0f, -0.51f, 0f), new Vector3(0.09f, 0.11f, 0.09f), matSkin);

            // Piernas desde cadera (-0.18m hasta -1.00m)
            legPivotL = CreatePivot("LegPivot_L", modelRoot, new Vector3(-0.10f, -0.20f, 0f));
            CreateLimb("Leg_L", legPivotL, new Vector3(0f, -0.35f, 0f), new Vector3(0.14f, 0.70f, 0.15f), matJeans);
            CreateLimb("Shoe_L", legPivotL, new Vector3(0f, -0.75f, 0.04f), new Vector3(0.14f, 0.10f, 0.24f), matShoes);

            legPivotR = CreatePivot("LegPivot_R", modelRoot, new Vector3(0.10f, -0.20f, 0f));
            CreateLimb("Leg_R", legPivotR, new Vector3(0f, -0.35f, 0f), new Vector3(0.14f, 0.70f, 0.15f), matJeans);
            CreateLimb("Shoe_R", legPivotR, new Vector3(0f, -0.75f, 0.04f), new Vector3(0.14f, 0.10f, 0.24f), matShoes);

            // Evitar advertencia de variable sin usar si pelvis solo sirve como ancla visual
            if (pelvis == null) return;
        }

        private Material CreateMat(Shader shader, Color col, float smoothness)
        {
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            else m.color = col;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            m.enableInstancing = true;
            createdMaterials.Add(m);
            return m;
        }

        private static Transform CreatePivot(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private GameObject CreateLimb(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                renderers.Add(mr);
            }
            return go;
        }

        public void SetVisibleInCamera(bool visible)
        {
            if (visible == currentVisible && renderers.Count > 0)
            {
                ShadowCastingMode target = visible ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
                if (renderers[0] != null && renderers[0].shadowCastingMode == target) return;
            }

            currentVisible = visible;
            ShadowCastingMode mode = visible ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].shadowCastingMode = mode;
                }
            }
        }

        private void Update()
        {
            if (modelRoot == null || player == null) return;

            float speed = player.CurrentSpeed;
            bool moving = player.IsGrounded && speed > 0.25f;
            bool crouching = player.IsCautious;
            bool sprinting = player.IsSprinting;
            bool phoneOpen = PhoneHUD.Instance != null && PhoneHUD.Instance.IsOpen;

            if (moving)
            {
                float freq = sprinting ? 11.5f : (crouching ? 5.5f : 7.8f);
                walkCycle += Time.deltaTime * freq;
            }
            else
            {
                walkCycle = Mathf.Lerp(walkCycle, Mathf.Round(walkCycle / Mathf.PI) * Mathf.PI, Time.deltaTime * 6f);
            }

            float swingAmp = moving ? (sprinting ? 36f : (crouching ? 16f : 24f)) : 0f;
            float legAngle = Mathf.Sin(walkCycle) * swingAmp;
            float armSwing = -Mathf.Sin(walkCycle) * (swingAmp * 0.78f);

            float crouchDrop = crouching ? -0.22f : 0f;
            float walkBob = moving ? Mathf.Abs(Mathf.Sin(walkCycle)) * (sprinting ? 0.045f : 0.025f) : Mathf.Sin(Time.time * 1.8f) * 0.008f;
            Vector3 targetModelPos = new Vector3(0f, crouchDrop + walkBob, 0f);
            modelRoot.localPosition = Vector3.Lerp(modelRoot.localPosition, targetModelPos, Time.deltaTime * 10f);

            float torsoForwardTilt = sprinting ? 10f : (crouching ? 14f : 0f);
            if (upperBodyPivot != null)
            {
                upperBodyPivot.localRotation = Quaternion.Lerp(
                    upperBodyPivot.localRotation,
                    Quaternion.Euler(torsoForwardTilt, 0f, 0f),
                    Time.deltaTime * 10f);
            }

            if (legPivotL != null)
            {
                legPivotL.localRotation = Quaternion.Lerp(legPivotL.localRotation, Quaternion.Euler(legAngle, 0f, 0f), Time.deltaTime * 14f);
            }
            if (legPivotR != null)
            {
                legPivotR.localRotation = Quaternion.Lerp(legPivotR.localRotation, Quaternion.Euler(-legAngle, 0f, 0f), Time.deltaTime * 14f);
            }
            if (armPivotL != null)
            {
                armPivotL.localRotation = Quaternion.Lerp(armPivotL.localRotation, Quaternion.Euler(armSwing, 0f, 0f), Time.deltaTime * 14f);
            }
            if (armPivotR != null)
            {
                Quaternion targetArmR = phoneOpen
                    ? Quaternion.Euler(-48f, -18f, 0f)
                    : Quaternion.Euler(-armSwing, 0f, 0f);
                armPivotR.localRotation = Quaternion.Lerp(armPivotR.localRotation, targetArmR, Time.deltaTime * 12f);
            }
        }
    }
}
