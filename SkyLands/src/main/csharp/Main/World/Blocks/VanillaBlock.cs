﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using API.Geo;
using API.Geo.Cuboid;
using API.Ent;
using API.Generic;

using Mogre;

using Entity   = API.Ent.Entity;
using Material = API.Generic.Material; 



namespace Game.World.Blocks
{
    public abstract class VanillaBlock : Block
    {
        protected string mName;
        protected string mMaterial;
        protected byte   mId = 255;//Warning do not set 255 as a value

        protected static int CUBE_SIDE = MainWorld.CUBE_SIDE;

        public override string getName() { return this.mName; }
        public override string getMaterial() { return this.mMaterial; }
        public override byte getId() { return this.mId; }


        public override string getFace(int i) { return this.mMaterial; }

        public override void onCreation(Vector3 position) { }
        public override void onDeletion() { }

        public override void onRightClick() { }
        public override void onLeftClick() { }

    }
}