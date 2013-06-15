﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mogre;

namespace Game.World.Blocks
{
    class RedSandBlock : VanillaBlock {
        public RedSandBlock() {
            this.mName = "Red Sand";
            this.mMaterial = "cube/RedSand";
            this.mItemTexture = "sand3.png";
            this.mId = 10;
        }
    }
}
