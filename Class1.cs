using System;
using System.Collections.Generic;
using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace SwordBeam
{
    public class SwordBeamModule : ItemModule
    {
        public Color BeamColor = new Color(0.0823529412f, 1, 1, 0.392156863f);
        public Color BeamEmission = new Color(0, 5.24313725f, 5.24313725f, 0);
        public Vector3 BeamSize = new Vector3(0.0375f, 1.65f, 0.0375f);
        public Vector3 BeamScaleIncrease = new Vector3(0, 0.2f, 0);
        public float BeamCooldown = 0.15f;
        public float DespawnTime = 1.5f;
        public float SwordSpeed = 7;
        public float BeamSpeed = 50;
        public string ProjectileID = "SwordBeam";
        public float BeamDamage = 5;
        public bool BeamDismember = true;
        public string ActivationButton = "Trigger";
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<SwordBeamComponent>().Setup(ProjectileID, ActivationButton, BeamDismember, BeamSpeed, SwordSpeed, DespawnTime, BeamDamage, BeamCooldown, BeamColor, BeamEmission, BeamSize, BeamScaleIncrease);
        }
    }
    public class SwordBeamComponent : MonoBehaviour
    {
        Item item;
        float cdH;
        float cooldown;
        float despawnTime;
        float swordSpeed;
        float beamSpeed;
        float beamDamage;
        bool dismember;
        string beamID;
        bool beam;
        public Color beamColor;
        public Color beamEmission;
        public Vector3 beamSize;
        public Vector3 beamScaleIncrease;
        Interactable.Action useStart;
        Interactable.Action useStop;
        public void Start()
        {
            item = GetComponent<Item>();
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
        }
        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == useStart) beam = true;
            else if (action == useStop) beam = false;
        }
        public void Setup(string projId, string activateMethod, bool beamDismember, float BeamSpeed, float SwordSpeed, float BeamDespawn, float BeamDamage, float BeamCooldown, Color color, Color emission, Vector3 size, Vector3 scale)
        {
            beamID = projId;
            dismember = beamDismember;
            beamSpeed = BeamSpeed;
            swordSpeed = SwordSpeed;
            despawnTime = BeamDespawn;
            beamDamage = BeamDamage;
            cooldown = BeamCooldown;
            if (activateMethod.ToLower().Contains("trigger") || activateMethod.ToLower() == "use")
            {
                useStart = Interactable.Action.UseStart;
                useStop = Interactable.Action.UseStop;
            }
            else if (activateMethod.ToLower().Contains("alt") || activateMethod.ToLower().Contains("spell"))
            {
                useStart = Interactable.Action.AlternateUseStart;
                useStop = Interactable.Action.AlternateUseStop;
            }
            beamColor = color;
            beamEmission = emission;
            beamSize = size;
            beamScaleIncrease = scale;
        }
        public void FixedUpdate()
        {
            if (Time.time - cdH <= cooldown || !beam || item.rb.velocity.magnitude - Player.local.locomotion.rb.velocity.magnitude < swordSpeed)
            {
                return;
            }
            else
            {
                cdH = Time.time;
                Catalog.GetData<ItemData>(beamID).SpawnAsync(ShootBeam, item.transform.position, Quaternion.LookRotation(Player.local.head.cam.transform.forward, item.rb.velocity));
            }
        }
        public void ShootBeam(Item spawnedItem)
        {
            spawnedItem.rb.useGravity = false;
            spawnedItem.rb.drag = 0;
            spawnedItem.rb.AddForce(Player.local.head.transform.forward * beamSpeed, ForceMode.Impulse);
            spawnedItem.IgnoreRagdollCollision(Player.local.creature.ragdoll);
            spawnedItem.IgnoreObjectCollision(item);
            spawnedItem.RefreshCollision(true);
            spawnedItem.gameObject.AddComponent<SwordBeam>().Setup(beamDamage, dismember, beamColor, beamEmission, beamSize, beamScaleIncrease, item);
            spawnedItem.Throw();
            spawnedItem.Despawn(despawnTime);
        }
    }
    public class SwordBeam : MonoBehaviour
    {
        Item item;
        Item origin;
        float damage;
        bool dismember;
        public Color BeamColor;
        public Color BeamEmission;
        public Vector3 BeamSize;
        public Vector3 BeamScaleIncrease;
        List<RagdollPart> parts = new List<RagdollPart>();
        Imbue imbue;
        public void Start()
        {
            item = GetComponent<Item>();
            item.renderers[0].material.SetColor("_BaseColor", BeamColor);
            item.renderers[0].material.SetColor("_EmissionColor", BeamEmission * 2f);
            item.renderers[0].gameObject.transform.localScale = BeamSize;
            imbue = item.colliderGroups[0].imbue;
            if (origin.colliderGroups[0].imbue is Imbue originImbue && originImbue.spellCastBase != null && originImbue.energy > 0)
                imbue.Transfer(originImbue.spellCastBase, origin.colliderGroups[0].imbue.maxEnergy);
        }
        public void Setup(float Damage, bool Dismember, Color color, Color emission, Vector3 size, Vector3 scale, Item original)
        {
            damage = Damage;
            dismember = Dismember;
            BeamColor = color;
            BeamEmission = emission;
            BeamSize = size;
            BeamScaleIncrease = scale;
            origin = original;
        }
        public void Update()
        {
            item.gameObject.transform.localScale += BeamScaleIncrease * (Time.deltaTime * 100);
            if (parts.Count > 0)
            {
                parts[0].gameObject.SetActive(true);
                parts[0].bone.animationJoint.gameObject.SetActive(true);
                parts[0].ragdoll.TrySlice(parts[0]);
                if (parts[0].data.sliceForceKill)
                    parts[0].ragdoll.creature.Kill();
                parts.RemoveAt(0);
            }
        }
        public void OnTriggerEnter(Collider c)
        {
            if (c.GetComponentInParent<ColliderGroup>() is ColliderGroup group && group.collisionHandler.isRagdollPart)
            {
                RagdollPart part = group.collisionHandler.ragdollPart;
                if (!part.ragdoll.creature.isPlayer && part.ragdoll.creature.gameObject.activeSelf == true && !part.isSliced)
                {
                    CollisionInstance instance = new CollisionInstance(new DamageStruct(DamageType.Slash, damage))
                    {
                        targetCollider = c,
                        targetColliderGroup = group,
                        sourceColliderGroup = item.colliderGroups[0],
                        sourceCollider = item.colliderGroups[0].colliders[0],
                        casterHand = origin.lastHandler.caster,
                        impactVelocity = item.rb.velocity,
                        contactPoint = c.transform.position,
                        contactNormal = -item.rb.velocity
                    };
                    instance.damageStruct.penetration = DamageStruct.Penetration.None;
                    instance.damageStruct.hitRagdollPart = part;
                    if (part.sliceAllowed && dismember)
                    {
                        Vector3 direction = part.GetSliceDirection();
                        float num1 = Vector3.Dot(direction, item.transform.up);
                        float num2 = 1f / 3f;
                        if (num1 < num2 && num1 > -num2 && !parts.Contains(part))
                        {
                            parts.Add(part);
                        }
                    }
                    if (imbue.spellCastBase?.GetType() == typeof(SpellCastLightning))
                    {
                        part.ragdoll.creature.TryElectrocute(1, 2, true, true, (imbue.spellCastBase as SpellCastLightning).imbueHitRagdollEffectData);
                        imbue.spellCastBase.OnImbueCollisionStart(instance);
                    }
                    if (imbue.spellCastBase?.GetType() == typeof(SpellCastProjectile))
                    {
                        instance.damageStruct.damage *= 2;
                        imbue.spellCastBase.OnImbueCollisionStart(instance);
                    }
                    if (imbue.spellCastBase?.GetType() == typeof(SpellCastGravity))
                    {
                        imbue.spellCastBase.OnImbueCollisionStart(instance);
                        part.ragdoll.creature.TryPush(Creature.PushType.Hit, item.rb.velocity, 3, part.type);
                        part.rb.AddForce(item.rb.velocity, ForceMode.VelocityChange);
                    }
                    else
                    {
                        if (imbue.spellCastBase != null && imbue.energy > 0)
                        {
                            imbue.spellCastBase.OnImbueCollisionStart(instance);
                        }
                        part.ragdoll.creature.TryPush(Creature.PushType.Hit, item.rb.velocity, 1, part.type);
                    }
                    part.ragdoll.creature.Damage(instance);
                }
            }
        }
    }
}
