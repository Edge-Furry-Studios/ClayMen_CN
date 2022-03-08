﻿using BepInEx;
using RoR2;
using UnityEngine;
using RoR2.Navigation;
using R2API;
using System.Collections.Generic;
using RoR2.CharacterAI;
using R2API.Utils;
using BepInEx.Configuration;
using System;
using RoR2.ContentManagement;
using System.Collections;
using EntityStates;

namespace ClayMen
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Moffein.ClayMen", "Clay Men", "1.4.0")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(DirectorAPI), nameof(LanguageAPI), nameof(PrefabAPI))]//, nameof(DamageAPI)
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class ClayMen : BaseUnityPlugin
    {
        public static Transform headTransform;

        public static bool titanic, roost, aqueduct, wetland, rallypoint, scorched, abyss, sirens, stadia, meadow, voidfields, artifact;
        public static bool aqueductBeetles, aqueductWisps, scorchedBeetles, scorchedImps, scorchedWisps, rallypointImps, rallypointWisps;

        public void ReadConfig()
        {
            titanic = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Titanic Plains"), false, new ConfigDescription("Enables Clay Men on this map.")).Value;
            roost = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Distant Roost"), false, new ConfigDescription("Enables Clay Men on this map.")).Value;
            aqueduct = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Abandoned Aqueduct"), true, new ConfigDescription("Enables Clay Men on this map.")).Value;
            wetland = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Wetland Aspect"), false, new ConfigDescription("Enables Clay Men on this map.")).Value;
            rallypoint = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Rallypoint Delta"), true, new ConfigDescription("Enables Clay Men on this map.")).Value;
            scorched = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Scorched Acres"), true, new ConfigDescription("Enables Clay Men on this map.")).Value;
            abyss = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Abyssal Depths"), false, new ConfigDescription("Enables Clay Men on this map.")).Value;
            sirens = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Sirens Call"), false, new ConfigDescription("Enables Clay Men on this map.")).Value;
            stadia = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Stadia Jungle"), true, new ConfigDescription("Enables Clay Men on this map.")).Value;
            meadow = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Sky Meadow"), false, new ConfigDescription("Enables Clay Men on this map.")).Value;
            voidfields = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Void Fields"), true, new ConfigDescription("Enables Clay Men on this map.")).Value;
            artifact = base.Config.Bind<bool>(new ConfigDefinition("1 - Stage Settings", "Bulwarks Ambry"), true, new ConfigDescription("Enables Clay Men on this map.")).Value;

            aqueductBeetles = base.Config.Bind<bool>(new ConfigDefinition("2 - Spawn Pool Settings", "Abandoned Aqueduct - Remove Beetles"), false, new ConfigDescription("Remove Beetles from this map if Clay Men are enabled.")).Value;
            aqueductWisps = base.Config.Bind<bool>(new ConfigDefinition("2 - Spawn Pool Settings", "Abandoned Aqueduct - Remove Wisps"), false, new ConfigDescription("Remove Wisps from this map if Clay Men are enabled.")).Value;

            scorchedBeetles = base.Config.Bind<bool>(new ConfigDefinition("2 - Spawn Pool Settings", "Scorched Acres - Remove Beetles"), false, new ConfigDescription("Remove Beetles from this map if Clay Men are enabled.")).Value;
            scorchedWisps = base.Config.Bind<bool>(new ConfigDefinition("2 - Spawn Pool Settings", "Scorched Acres - Remove Wisps"), false, new ConfigDescription("Remove Wisps from this map if Clay Men are enabled.")).Value;
            scorchedImps = base.Config.Bind<bool>(new ConfigDefinition("2 - Spawn Pool Settings", "Scorched Acres - Remove Imps"), true, new ConfigDescription("Remove Imps from this map if Clay Men are enabled.")).Value;

            rallypointWisps = base.Config.Bind<bool>(new ConfigDefinition("2 - Spawn Pool Settings", "Rallypoint Delta - Remove Wisps"), true, new ConfigDescription("Remove Wisps from this map if Clay Men are enabled.")).Value;
            rallypointImps = base.Config.Bind<bool>(new ConfigDefinition("2 - Spawn Pool Settings", "Rallypoint Delta - Remove Imps"), false, new ConfigDescription("Remove Imps from this map if Clay Men are enabled.")).Value;
        }

        public void Start()
        {
            ItemDisplays.DisplayRules(Content.ClayManObject);
        }

        private void SetEntityStateFieldValues()
        {
            SneedUtils.SneedUtils.SetEntityStateField("EntityStates.ClaymanMonster.Leap", "verticalJumpSpeed", "20");
            SneedUtils.SneedUtils.SetEntityStateField("EntityStates.ClaymanMonster.Leap", "horizontalJumpSpeedCoefficient", "2.3");

            SneedUtils.SneedUtils.SetEntityStateField("EntityStates.ClaymanMonster.SpawnState", "duration", "3.2");
        }

        public void Awake()
        {
            ReadConfig();

            Content.ClayManObject = PrefabAPI.InstantiateClone(LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/ClayBody"), "MoffeinClayManBody", true);
            Content.ClayManMaster = PrefabAPI.InstantiateClone(LegacyResourcesAPI.Load<GameObject>("prefabs/charactermasters/ClaymanMaster"), "MoffeinClayManMaster", true);

            SetEntityStateFieldValues();
            Prefab.Modify(Content.ClayManObject);
            ModifyAI();

            ItemDisplays.DisplayRules(Content.ClayManObject);
            Director.Setup();
            ModifySkills(Content.ClayManObject);
            //SetupDamageTypes();

            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        /*private void SetupDamageTypes()
        {
            Content.ClayGooClayMan = DamageAPI.ReserveDamageType();
            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                orig(self, damageInfo);
                if (self.alive && !damageInfo.rejected)
                {
                    if (damageInfo.HasModdedDamageType(Content.ClayGooClayMan))
                    {
                        self.body.AddTimedBuff(RoR2Content.Buffs.ClayGoo.buffIndex, 2f * damageInfo.procCoefficient);
                    }
                }
            };
        }*/

        private void ModifySkills(GameObject bodyObject)
        {
            SkillLocator sk = bodyObject.GetComponent<SkillLocator>();
            sk.primary.skillFamily.variants[0].skillDef.activationState = new SerializableEntityStateType(typeof(EntityStates.MoffeinClayMan.SwipeForwardTar));
            sk.primary.skillFamily.variants[0].skillDef.isCombatSkill = true;
            sk.primary.skillFamily.variants[0].skillDef.baseRechargeInterval = 1.4f;
        }

        private void ModifyAI()
        {
            AISkillDriver clayPrimary = Content.ClayManMaster.GetComponent<AISkillDriver>();
            clayPrimary.maxDistance = 12f;
            Content.ClayManMaster.GetComponent<CharacterMaster>().bodyPrefab = Content.ClayManObject;
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new Content());
        }
    }
}
