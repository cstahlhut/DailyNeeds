﻿using Rek.FoodSystem;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;

namespace Stollie.DailyNeeds
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;

    /* BIG THANKS to DarkStar for sharing this with me! */

    internal class Spawn
    {
        private static readonly SerializableBlockOrientation EntityOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);

        private static readonly MyObjectBuilder_CubeGrid CubeGridBuilder = new MyObjectBuilder_CubeGrid()
        {
            EntityId = 0,
            GridSizeEnum = MyCubeSize.Large,
            IsStatic = true,
            Skeleton = new List<BoneInfo>(),
            LinearVelocity = Vector3.Zero,
            AngularVelocity = Vector3.Zero,
            ConveyorLines = new List<MyObjectBuilder_ConveyorLine>(),
            BlockGroups = new List<MyObjectBuilder_BlockGroup>(),
            Handbrake = false,
            XMirroxPlane = null,
            YMirroxPlane = null,
            ZMirroxPlane = null,
            PersistentFlags = MyPersistentEntityFlags2.InScene,
            Name = "ArtificialCubeGrid",
            DisplayName = "FieldEffect",
            CreatePhysics = false,
            DestructibleBlocks = true,
            PositionAndOrientation = new MyPositionAndOrientation(Vector3D.Zero, Vector3D.Forward, Vector3D.Up),

            CubeBlocks = new List<MyObjectBuilder_CubeBlock>()
                {
                    new MyObjectBuilder_CubeBlock()
                    {
                        EntityId = 0,
                        BlockOrientation = EntityOrientation,
                        SubtypeName = string.Empty,
                        Name = string.Empty,
                        Min = Vector3I.Zero,
                        Owner = 0,
                        ShareMode = MyOwnershipShareModeEnum.None,
                        DeformationRatio = 0,
                    }
                }
        };

        public static readonly MyObjectBuilder_Meteor GravityMissile = new MyObjectBuilder_Meteor()
        {
            EntityId = 0,
            LinearVelocity = Vector3.One * 10,
            AngularVelocity = Vector3.Zero,
            PersistentFlags = MyPersistentEntityFlags2.InScene,
            Name = "GravityMissile",
            PositionAndOrientation = new MyPositionAndOrientation(Vector3D.Zero, Vector3D.Forward, Vector3D.Up)
        };

        public static MyEntity EmptyEntity(string displayName, string model, MyEntity parent, bool parented = false)
        {
            try
            {
                //var myParent = parented ? parent : null;
                var ent = new MyEntity();
                ent.Init(null, model, null, null, null);
                //ent.Name = $"{parent.EntityId}";
                ent.Render.CastShadows = false;
                ent.IsPreview = true;
                ent.Render.Visible = true;
                ent.Save = false;
                ent.SyncFlag = false;
                ent.NeedsWorldMatrix = false;
                //ent.RemoveFromGamePruningStructure();
                ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
                MyEntities.Add(ent);
                return ent;
            }
            catch (Exception ex) { Logging.Instance.WriteLine($"Exception in EmptyEntity: {ex}"); return null; }
        }

        public static MyEntity SpawnBlock(IMyEntity spawnPosition, string subtypeId, string name, bool isVisible = false, bool hasPhysics = false, bool isStatic = false, bool toSave = false, bool destructible = false, long ownerId = 0)
        {
            try
            {
                CubeGridBuilder.Name = name;
                CubeGridBuilder.CubeBlocks[0].SubtypeName = subtypeId;
                CubeGridBuilder.CreatePhysics = hasPhysics;
                CubeGridBuilder.IsStatic = isStatic;
                CubeGridBuilder.DestructibleBlocks = destructible;
                var ent = (MyEntity)MyAPIGateway.Entities.CreateFromObjectBuilder(CubeGridBuilder);

                ent.Flags &= ~EntityFlags.Save;
                ent.Render.Visible = isVisible;
                ent.PositionComp.SetWorldMatrix(spawnPosition.WorldMatrix, null, true, true, true, true, false, false);
                
                MyAPIGateway.Entities.AddEntity(ent);
                
                return ent;
            }
            catch (Exception ex)
            {
                Logging.Instance.WriteLine("Exception in Spawn");
                Logging.Instance.WriteLine($"{ex}");
                return null;
            }
        }

        internal static MyEntity SpawnPrefab(string name, out IMyTextPanel lcd, bool isDisplay = false)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    lcd = null;
                    return null;
                }

                PrefabBuilder.CubeBlocks.Clear(); // need no leftovers from previous spawns

                if (isDisplay)
                {
                    PrefabTextPanel.SubtypeName = name;
                    PrefabTextPanel.FontColor = new Vector4(1, 1, 1, 1);
                    PrefabTextPanel.BackgroundColor = new Vector4(0, 0, 0, 0);
                    PrefabTextPanel.FontSize = DISPLAY_FONT_SIZE;
                    PrefabBuilder.CubeBlocks.Add(PrefabTextPanel);
                    var def = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_TextPanel), name)) as MyTextPanelDefinition;

                    if (def != null)
                        def.TextureResolution = 256;
                }
                else
                {
                    PrefabCubeBlock.SubtypeName = name;
                    PrefabBuilder.CubeBlocks.Add(PrefabCubeBlock);
                }

                MyEntities.RemapObjectBuilder(PrefabBuilder);
                var ent = MyEntities.CreateFromObjectBuilder(PrefabBuilder, true);
                ent.IsPreview = true; // don't sync on MP
                ent.SyncFlag = false; // don't sync on MP
                ent.Save = false; // don't save this entity

                MyEntities.Add(ent, true);
                var lcdSlim = ((IMyCubeGrid)ent).GetCubeBlock(Vector3I.Zero);
                lcd = lcdSlim.FatBlock as IMyTextPanel;
                //lcd.Render.CastShadows = false;
                //lcd.Render.Transparency = 0.5f;
                //lcd.Render.NeedsResolveCastShadow = false;
                //lcd.Render.EnableColorMaskHsv = false;
                //lcd.Render.DrawInAllCascades = false;

                //lcd.Render.UpdateRenderObject(false, true);
                //lcd.Render.UpdateRenderObject(true, true);
                //lcd.Render.RemoveRenderObjects();
                //lcd.Render.AddRenderObjects();
                lcd.SetEmissiveParts("ScreenArea", Color.White, 1f);
                lcd.SetEmissiveParts("Edges", Color.Teal, 0.5f);

                lcd.ContentType = ContentType.TEXT_AND_IMAGE;

                return ent;
            }
            catch (Exception ex) { Logging.Instance.WriteLine($"Exception in SpawnPrefab: {ex}"); }

            lcd = null;
            return null;
        }

        private static SerializableVector3 PrefabVector0 = new SerializableVector3(0, 0, 0);
        private static SerializableVector3I PrefabVectorI0 = new SerializableVector3I(0, 0, 0);
        private static SerializableBlockOrientation PrefabOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);
        private const float DISPLAY_FONT_SIZE = 1f;

        private static MyObjectBuilder_CubeBlock PrefabCubeBlock = new MyObjectBuilder_CubeBlock()
        {
            EntityId = 1,
            SubtypeName = "",
            Min = PrefabVectorI0,
            BlockOrientation = PrefabOrientation,
            DeformationRatio = 0,
            ShareMode = MyOwnershipShareModeEnum.None,
        };

        private static MyObjectBuilder_CubeGrid PrefabBuilder = new MyObjectBuilder_CubeGrid()
        {
            EntityId = 0,
            GridSizeEnum = MyCubeSize.Small,
            IsStatic = true,
            Skeleton = new List<BoneInfo>(),
            LinearVelocity = PrefabVector0,
            AngularVelocity = PrefabVector0,
            ConveyorLines = new List<MyObjectBuilder_ConveyorLine>(),
            BlockGroups = new List<MyObjectBuilder_BlockGroup>(),
            Handbrake = false,
            XMirroxPlane = null,
            YMirroxPlane = null,
            ZMirroxPlane = null,
            PersistentFlags = MyPersistentEntityFlags2.InScene,
            Name = "HelmetMod",
            DisplayName = "HelmetMod",
            CreatePhysics = false,
            PositionAndOrientation = new MyPositionAndOrientation(Vector3D.Zero, Vector3D.Forward, Vector3D.Up),
            CubeBlocks = new List<MyObjectBuilder_CubeBlock>(),
        };

        private static MyObjectBuilder_TextPanel PrefabTextPanel = new MyObjectBuilder_TextPanel()
        {
            EntityId = 1,
            Min = PrefabVectorI0,
            BlockOrientation = PrefabOrientation,
            ShareMode = MyOwnershipShareModeEnum.None,
            DeformationRatio = 0,
            ShowOnHUD = false,
            //ShowText = ShowTextOnScreenFlag.PUBLIC, // HACK not whitelisted anymore...
            FontSize = DISPLAY_FONT_SIZE,
        };
    }
}