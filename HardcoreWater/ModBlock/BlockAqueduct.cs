using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HardcoreWater.ModBlock
{
	public class BlockAqueduct : Block
	{
		public string Orientation
		{
			get
			{
				return this.Variant["orientation"];
			}
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);

			if (world.BlockAccessor.GetBlockEntity(pos) != null)
            {
				world.BlockAccessor.RemoveBlockEntity(pos);
			}
		}
	}
}
