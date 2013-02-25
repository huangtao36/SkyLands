﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using API.Generator;

using Game.World.Generator.Decorators;

namespace Game.World.Generator.Biomes
{
    class Desert : Biome
    {
        public Desert() : base((byte) 1, new CactusDecorator()) {
            this.mGroundCover.Add("Sand");
            this.mGroundCover.Add("Sand");
            this.mGroundCover.Add("Sand");

            this.setMinMax(63, 74);
        }


    }
}
