using HardcoreWater.ModBlock;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HardcoreWater.ModBlockEntity
{
    public class BlockEntityAqueduct : BlockEntity
	{
		private bool IsValidWaterSource(BlockPos blockPos)
        {
			Block nearBlock = this.Api.World.BlockAccessor.GetBlock(blockPos, BlockLayersAccess.Fluid);
			if (nearBlock != null)
			{
				if (nearBlock.IsLiquid())
				{
					return true;
				}
			}

			return false;
		}

		private bool IsValidFilledAqueduct(BlockPos blockPos)
        {
			BlockEntity nearBlockEntity = this.Api.World.BlockAccessor.GetBlockEntity(blockPos);
			if (nearBlockEntity is BlockEntityAqueduct)
			{
				BlockEntityAqueduct aqueduct = (nearBlockEntity as BlockEntityAqueduct);
				if (aqueduct.waterLevel > 0)
				{
					return true;
				}
			}

			return false;
		}

		private void onServerTick1s(float dt)
		{
			BlockPos[] blockPosFB = new BlockPos[2];

			// Scan blocks front and back of the aqueduct
			if (this.blockAqueduct.Orientation == "ns")
            {
				blockPosFB[0] = this.Pos.NorthCopy();
				blockPosFB[1] = this.Pos.SouthCopy();
			}
            else
            {
				blockPosFB[0] = this.Pos.WestCopy();
				blockPosFB[1] = this.Pos.EastCopy();
			}

			// Check validity of previous source location, if present
			if (this.waterSourcePos != null)
            {
				Block prevWater = this.Api.World.BlockAccessor.GetBlock(this.waterSourcePos, BlockLayersAccess.Fluid);
				if (prevWater != null)
				{
					if (!prevWater.IsLiquid())
					{
						this.waterSourcePos = null;
					}
				}
			}
			else
            {
				// If water source position is null, search for water
				// If block to either side is a water source or below a filled aqueduct, set water level of current aqueduct to 6 and distance from water to 1
				for (int i = 0; i < 2; i++)
				{
					if (IsValidWaterSource(blockPosFB[i]))
					{
						this.waterSourcePos = blockPosFB[i];
						break;
					}
				}

				// Check above as well
				if (IsValidFilledAqueduct(this.Pos.UpCopy()))
				{
					this.waterSourcePos = this.Pos.UpCopy();
				}
			}
			
			if (this.waterSourcePos != null)
            {
				this.waterLevel = 6;
				this.MarkDirty(true);
            }
			else
            {
				// Otherwise, if block to either side is an aqueduct, set this aqueducts distance to water to water source of surrounding aqueducts
				for (int i = 0; i < 2; i++)
				{
					BlockEntity nearBlockEntity = this.Api.World.BlockAccessor.GetBlockEntity(blockPosFB[i]);
					if (nearBlockEntity != null)
					{
						if (nearBlockEntity is BlockEntityAqueduct)
						{
							BlockEntityAqueduct aqueduct = (nearBlockEntity as BlockEntityAqueduct);
							waterSourcePos = aqueduct.waterSourcePos;
						}
					}
				}

				// If greater than max distance and connected to source, break the aqueduct
				if (waterSourcePos != null)
				{
					if (this.Pos.ManhattenDistance(waterSourcePos) > HardcoreWaterConfig.Loaded.AqueductMaxDistanceFromWaterSourceBlocks)
					{
						this.Api.World.BlockAccessor.BreakBlock(this.Pos, null);
						if (this.Api.World.BlockAccessor.GetBlockEntity(this.Pos) != null)
						{
							this.Api.World.BlockAccessor.RemoveBlockEntity(this.Pos);
						}
						this.MarkDirty(true);
						return;
					}
				}
			}

			// If still no water source, reduce water level
			if (waterSourcePos == null)
            {
				this.waterLevel = Math.Max(0, this.waterLevel - 1);
            }

			// Spawn a water non-source block on any empty side, if water still remains
			if (this.waterLevel > 0)
            {
				for (int i = 0; i < 2; i++)
				{
					Block nearBlockFluid = this.Api.World.BlockAccessor.GetBlock(blockPosFB[i], BlockLayersAccess.Fluid);
					Block nearBlock = this.Api.World.BlockAccessor.GetBlock(blockPosFB[i]);
					Block waterSourceBlock = this.Api.World.GetBlock(new AssetLocation("game:water-still-7"));
					Block waterBlock = this.Api.World.GetBlock(new AssetLocation("game:water-still-" + Math.Min(7, this.waterLevel)));
					bool notIced = !nearBlockFluid.Code.Path.Contains("ice");
					if (notIced && nearBlockFluid != waterSourceBlock && !nearBlock.DisplacesLiquids(this.Api.World.BlockAccessor, blockPosFB[i]))
					{
						this.Api.World.BlockAccessor.SetBlock(waterBlock.BlockId, blockPosFB[i], 2);
						this.Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(blockPosFB[i]);
					}
				}
			}
		}

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			this.blockAqueduct = (base.Block as BlockAqueduct);
			this.RegisterGameTickListener(new Action<float>(this.onServerTick1s), (int) Math.Round(HardcoreWaterConfig.Loaded.AqueductUpdateFrequencySeconds * 1000), 0);
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			tree.SetInt("waterLevel", this.waterLevel);
			tree.SetBlockPos("waterSourcePos", this.waterSourcePos);
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			base.FromTreeAttributes(tree, worldAccessForResolve);
			this.waterLevel = tree.GetInt("waterLevel");
			this.waterSourcePos = tree.GetBlockPos("waterSourcePos");
		}

		private BlockAqueduct blockAqueduct;

		private int waterLevel = 0;

		private BlockPos waterSourcePos = null;
	}
}
