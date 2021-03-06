﻿using System;
using System.Collections.Generic;
using System.IO;


using API.Geo.Cuboid;
using API.Generic;

using Mogre;

using Game.Display;
using Game.World.Blocks;
using Game.World.Generator.Biomes;

namespace Game.World.Generator
{
    public abstract class VanillaIsland : Island {
        static readonly Block defaultBlock = new AirBlock();

        public VanillaIsland(SceneNode node, Vector2 size, API.Geo.World currentWorld) : base(node, size, currentWorld) {
            
        }

        public VanillaIsland(SceneNode node, API.Geo.World currentWorld) : base(node, currentWorld) {
        
        }
        

        public override int getSurfaceHeight(int x, int z, string restriction = "") {
            for(int y = (int)this.mIslandSize.y * Cst.CHUNK_SIDE; y != 0 ; y--) { 
                if(!(this.getBlock(x, y, z, false) is AirBlock)) {
                    if (restriction == "") { return y + 1; }
                    if(this.getBlock(x, y, z, false).getName() == restriction) { return y + 1; }
                }
            }
            return -1;
        }

        public override bool hasBlock(int x, int y, int z) { throw new NotImplementedException(); }


        public override void display() {

            LogManager.Singleton.DefaultLog.LogMessage("Now displaying");

            Chunk.Clean = true;
            foreach (KeyValuePair<Vector3, Chunk> pair in this.mChunkList) {
                pair.Value.generateVisualChunk();
            }
            this.displayed = true;

        }

        public bool hasVisiblefaceAt(Chunk c, int x, int y, int z, BlockFace i) { return c.hasVisibleFaceAt(x, y, z, i); }

        //For optimization purpose returns wether the block has visible faces
        public override bool setVisibleFaces(Vector3 blockCoord, Block curr)
        {

            if (curr is AirBlock) { return false; }

            if(curr is TransparentBlock) { return this.setVisibleFacesForPortals(blockCoord, curr); }

            bool hasVisiblefaces = false;
            Dictionary<BlockFace, Vector3> coordToCheck = new Dictionary<BlockFace,Vector3>();

            coordToCheck.Add(BlockFace.rightFace, new Vector3(blockCoord.x + 1, blockCoord.y,     blockCoord.z));
            coordToCheck.Add(BlockFace.leftFace,  new Vector3(blockCoord.x - 1, blockCoord.y,     blockCoord.z));
            coordToCheck.Add(BlockFace.upperFace, new Vector3(blockCoord.x,     blockCoord.y + 1, blockCoord.z));
            coordToCheck.Add(BlockFace.underFace, new Vector3(blockCoord.x,     blockCoord.y - 1, blockCoord.z));
            coordToCheck.Add(BlockFace.frontFace, new Vector3(blockCoord.x,     blockCoord.y,     blockCoord.z + 1));
            coordToCheck.Add(BlockFace.backFace,  new Vector3(blockCoord.x,     blockCoord.y,     blockCoord.z - 1));


            foreach (KeyValuePair<BlockFace, Vector3> keyVal in coordToCheck)
            {
                if((this.getBlock(keyVal.Value, false) is TransparentBlock) || this.getBlock(keyVal.Value, false) is Air) { 
                    this.setVisibleFaceAt(blockCoord, keyVal.Key, true); hasVisiblefaces = true; 
                }
            }
            return hasVisiblefaces;
        }

        private void setVisibleFaceAt(Vector3 loc, BlockFace face, bool val) {
            this.setVisibleFaceAt((int)loc.x, (int)loc.y, (int)loc.z, face, val); 
        }

        public bool setVisibleFacesForPortals(Vector3 blockCoord, Block curr) {
            bool hasVisiblefaces = false;
            Dictionary<BlockFace, Vector3> coordToCheck = new Dictionary<BlockFace, Vector3>();

            coordToCheck.Add(BlockFace.rightFace, new Vector3(blockCoord.x + 1, blockCoord.y, blockCoord.z));
            coordToCheck.Add(BlockFace.leftFace,  new Vector3(blockCoord.x - 1, blockCoord.y, blockCoord.z));
            coordToCheck.Add(BlockFace.upperFace, new Vector3(blockCoord.x, blockCoord.y + 1, blockCoord.z));
            coordToCheck.Add(BlockFace.underFace, new Vector3(blockCoord.x, blockCoord.y - 1, blockCoord.z));
            coordToCheck.Add(BlockFace.frontFace, new Vector3(blockCoord.x, blockCoord.y, blockCoord.z + 1));
            coordToCheck.Add(BlockFace.backFace,  new Vector3(blockCoord.x, blockCoord.y, blockCoord.z - 1));


            foreach(KeyValuePair<BlockFace, Vector3> keyVal in coordToCheck) {
                if(this.getBlock(keyVal.Value, false) is Air && !(this.getBlock(keyVal.Value, false) is TransparentBlock)) {
                    this.setVisibleFaceAt(blockCoord, keyVal.Key, true); hasVisiblefaces = true;
                }
            }
            return hasVisiblefaces;
        }

        public override void setVisibleFaceAt(int x, int y, int z, BlockFace face, bool val) {
            Vector3 chunkLocation = new Vector3(x / 16, y / 16, z / 16),
                    blockLocation = new Vector3(x % 16, y % 16, z % 16);

            if(blockLocation.x < 0 || blockLocation.y < 0 || blockLocation.z < 0) {
                if(blockLocation.x < 0) { blockLocation.x += 16; chunkLocation.x -= 1; }
                if(blockLocation.z < 0) { blockLocation.z += 16; chunkLocation.z -= 1; }
                if(blockLocation.y < 0) { blockLocation.y += 16; chunkLocation.y -= 1; }
            }
            if(this.hasChunk(chunkLocation)) { this.mChunkList[chunkLocation].setVisibleFaceAt((int)blockLocation.x, (int)blockLocation.y, (int)blockLocation.z, face, val); }
        }

        public override bool hasVisibleFaceAt(int x, int y, int z, BlockFace face) {
            Vector3 chunkLocation = new Vector3(x / 16, y / 16, z / 16),
                    blockLocation = new Vector3(x % 16, y % 16, z % 16);

            if(blockLocation.x < 0 || blockLocation.y < 0 || blockLocation.z < 0) {
                if(blockLocation.x < 0) { blockLocation.x += 16; chunkLocation.x -= 1; }
                if(blockLocation.z < 0) { blockLocation.z += 16; chunkLocation.z -= 1; }
                if(blockLocation.y < 0) { blockLocation.y += 16; chunkLocation.y -= 1; }
            }
            if(this.hasChunk(chunkLocation)) { return this.mChunkList[chunkLocation].hasVisibleFaceAt((int)blockLocation.x, (int)blockLocation.y, (int)blockLocation.z, face); }
            return false;
        }



        public override void save() {
            var blockfileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SkyLands\\" +
                this.mWorld.getName() + "-Island-" + this.mBiome.getId() + ".sav";


            using(TextWriter writer = new StreamWriter(blockfileName)) {

                writer.WriteLine((int)this.mIslandSize.x);
                writer.WriteLine((int)this.mIslandSize.y);
                writer.WriteLine((int)this.mIslandSize.z);

                foreach(KeyValuePair<Vector3, Chunk> pair in this.mChunkList) {
                    writer.WriteLine((int)pair.Key.x);
                    writer.WriteLine((int)pair.Key.y);
                    writer.WriteLine((int)pair.Key.z);

                    for(int x = 0; x < 16; x++) {
                        for(int y = 0; y < 16; y++) {
                            for(int z = 0; z < 16; z++) {
                                writer.WriteLine(pair.Value.mBlockList[x, y, z].getId());
                            }
                        }
                    }
                }
            }

        }

        public override void save(string name) {

            using(TextWriter writer = new StreamWriter(name)) {

                writer.WriteLine((int)this.mIslandSize.x);
                writer.WriteLine((int)this.mIslandSize.y);
                writer.WriteLine((int)this.mIslandSize.z);

                foreach(KeyValuePair<Vector3, Chunk> pair in this.mChunkList) {
                    writer.WriteLine((int)pair.Key.x);
                    writer.WriteLine((int)pair.Key.y);
                    writer.WriteLine((int)pair.Key.z);

                    for(int x = 0; x < 16; x++) {
                        for(int y = 0; y < 16; y++) {
                            for(int z = 0; z < 16; z++) {
                                writer.WriteLine(pair.Value.mBlockList[x, y, z].getId());
                            }
                        }
                    }
                }
            }

        }

        public byte[] linearize(Block[, ,] a) {
            byte[] b = new byte[a.GetLength(0) * a.GetLength(1) * a.GetLength(2)];
            int i = 0;
            for(int x = 0; x < 16; x++) {
                for(int y = 0; y < 16; y++) {
                    for(int z = 0; z < 16; z++) {
                        b[i] = a[x, y, z].getId();
                        i++;
                    }
                }
            }
            return b;
        }

        public override void load() {
            var blockfileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SkyLands\\" +
            this.mWorld.getName() + "-Island-" + this.mBiome.getId() + ".sav";
            DirectoryInfo directoryInfo = new FileInfo(blockfileName).Directory;
            if(directoryInfo != null) {
                directoryInfo.Create();
            }
            StreamReader stream;

            try { stream = new StreamReader(blockfileName); }
            catch { throw new Exception("Could not read file : " + blockfileName); }


            this.mIslandSize = new Vector3(Convert.ToInt32(stream.ReadLine()), Convert.ToInt32(stream.ReadLine()), Convert.ToInt32(stream.ReadLine()));

            while(stream.BaseStream.Position != stream.BaseStream.Length) {
                Vector3 pos = new Vector3(Convert.ToInt32(stream.ReadLine()), Convert.ToInt32(stream.ReadLine()), Convert.ToInt32(stream.ReadLine()));
                Chunk c = new VanillaChunk(new Vector3(16, 16, 16), pos, this);
                this.mChunkList.Add(pos, c);
                for(int x = 0; x < 16; x++) { for(int y = 0; y < 16; y++) { for(int z = 0; z < 16; z++) { c.setBlock(x, y, z, Convert.ToByte(stream.ReadLine())); } } }
            }

            stream.Close();
        }

        public void setBlockAt(Vector3 v, byte b, bool force) {
            this.setBlockAt((int)v.x, (int)v.y, (int)v.z, b, force);
        }

        public override void removeBlock(Vector3 item) {
            this.setBlockAt((int)item.x, (int)item.y, (int)item.z, "Air", true);
        }

        public void loadStructures(string path) {
            string[] s = File.ReadAllLines(path + "structures.scenario");

            for(int i = 0; i < s.Length; i++) {
                string islandType = (s[i].Split('-'))[1];
                if((islandType == "Plains" && this.mBiome is Plains)    || (islandType == "Desert" && this.mBiome is Desert) ||
                   (islandType == "Mount"  && this.mBiome is Mountains) || (islandType == "Hills"  && this.mBiome is Hills)) {
                    Vector3 v = API.Generator.Decorator.FindRandomPoint(this, new Random());
                    string[] ss = File.ReadAllLines(path + s[i] + ".terrain");

                    int h = 0;
                    for(int x = 0; x < 16; x++) {
                        for(int y = 0; y < 16; y++) {
                            for(int z = 0; z < 16; z++) {
                                this.setBlockAt(v + new Vector3(x, y, z), Convert.ToByte(ss[h]), true);
                                h++;
                            }
                        }
                    }
                }
            }
        }

        public override void load(string name) {
            DirectoryInfo directoryInfo = new FileInfo(name).Directory;
            if(directoryInfo != null) {
                directoryInfo.Create();
            }
            StreamReader stream;

            try { stream = new StreamReader(name); } catch { throw new Exception("Could not read file : " + name); }


            this.mIslandSize = new Vector3(Convert.ToInt32(stream.ReadLine()), Convert.ToInt32(stream.ReadLine()), Convert.ToInt32(stream.ReadLine()));

            while(stream.BaseStream.Position != stream.BaseStream.Length) {
                Vector3 pos = new Vector3(Convert.ToInt32(stream.ReadLine()), Convert.ToInt32(stream.ReadLine()), Convert.ToInt32(stream.ReadLine()));
                Chunk c = new VanillaChunk(new Vector3(16, 16, 16), pos, this);
                this.mChunkList.Add(pos, c);
                for(int x = 0; x < 16; x++) { for(int y = 0; y < 16; y++) { for(int z = 0; z < 16; z++) { c.setBlock(x, y, z, Convert.ToByte(stream.ReadLine())); } } }
            }

            stream.Close();
        }

        public override string getMaterialFromName(string name) { return VanillaChunk.staticBlock[name].getMaterial(); }

        public override Block getBlock(int x, int y, int z, bool force) {
            if(!force && (x < 0 || y < 0 || z < 0)) { return defaultBlock; }

            Vector3 chunkLocation = new Vector3(x / 16, y / 16, z / 16),
                    blockLocation = new Vector3(x % 16, y % 16, z % 16);

            if(force && (blockLocation.x < 0 || blockLocation.y < 0 || blockLocation.z < 0)) {
                if(blockLocation.x < 0) { blockLocation.x += 16; chunkLocation.x -= 1; }
                if(blockLocation.z < 0) { blockLocation.z += 16; chunkLocation.z -= 1; }
                if(blockLocation.y < 0) { blockLocation.y += 16; chunkLocation.y -= 1; }
            }

            if(this.hasChunk(chunkLocation)) { return this.mChunkList[chunkLocation].getBlock(blockLocation); }
            if(force) {
                this.mChunkList.Add(chunkLocation, new VanillaChunk(Cst.CHUNK_SIDE * Vector3.UNIT_SCALE, chunkLocation, this));
                this.mChunkList[chunkLocation].generateVisualChunk();

                if(y > this.mIslandSize.y * Cst.CHUNK_SIDE) { this.mIslandSize.y = (int)System.Math.Ceiling(y / 16f); }
                if(x > this.mIslandSize.x * Cst.CHUNK_SIDE) { this.mIslandSize.x = (int)System.Math.Ceiling(x / 16f); }
                if(z > this.mIslandSize.z * Cst.CHUNK_SIDE) { this.mIslandSize.z = (int)System.Math.Ceiling(z / 16f); }

                return this.mChunkList[chunkLocation].getBlock(blockLocation);
            }
            return defaultBlock;
        }

        public override Vector3 getBlockCoord(int x, int y, int z) {
            if(x < 0 || y < 0 || z < 0) { return -Vector3.UNIT_SCALE; }

            Vector3 chunkLocation = new Vector3(x / 16, y / 16, z / 16),
                    blockLocation = new Vector3(x % 16, y % 16, z % 16);

            if(this.hasChunk(chunkLocation)) { return blockLocation; }
            return -Vector3.UNIT_SCALE;
        }

        public override Chunk getChunkFromBlock(int x, int y, int z) {
            Vector3 chunkLocation = new Vector3(x / 16, y / 16, z / 16);

            if(this.hasChunk(chunkLocation)) { return this.mChunkList[chunkLocation]; }
            return null;
        }

// ReSharper disable UnusedMember.Local
        private Vector3 getBlockCoordFromRelative(int x, int y, int z) { return new Vector3(x % 16, y % 16, z % 16); }
        private Vector3 getChunkCoordFromRelative(int x, int y, int z) { return new Vector3(x / 16, y / 16, z / 16); }
// ReSharper restore UnusedMember.Local


        public override void setBlockAt(int x, int y, int z, string material, bool force) {
            if(!force && (x < 0 || y < 0 || z < 0)) { return; }

            Vector3 chunkLocation = new Vector3(x / 16, y / 16, z / 16),
                    blockLocation = new Vector3(x % 16, y % 16, z % 16);

            if(force && (blockLocation.x < 0 || blockLocation.y < 0 || blockLocation.z < 0)) {
                if(blockLocation.x < 0) { blockLocation.x += 16; chunkLocation.x -= 1; }
                if(blockLocation.z < 0) { blockLocation.z += 16; chunkLocation.z -= 1; }
                if(blockLocation.y < 0) { blockLocation.y += 16; chunkLocation.y -= 1; }
            }

            if (this.hasChunk(chunkLocation)) { this.mChunkList[chunkLocation].setBlock(blockLocation, material); }
            else if(force) {
                this.mChunkList.Add(chunkLocation, new VanillaChunk(Cst.CHUNK_SIDE * Vector3.UNIT_SCALE, chunkLocation, this));
                this.mChunkList[chunkLocation].setBlock(blockLocation, material);
                this.mChunkList[chunkLocation].generateVisualChunk();

                if(y > this.mIslandSize.y * Cst.CHUNK_SIDE) { this.mIslandSize.y = (int)System.Math.Ceiling(y / 16f); }
                if(x > this.mIslandSize.x * Cst.CHUNK_SIDE) { this.mIslandSize.x = (int)System.Math.Ceiling(x / 16f); }
                if(z > this.mIslandSize.z * Cst.CHUNK_SIDE) { this.mIslandSize.z = (int)System.Math.Ceiling(z / 16f); }
            } 
        }

        public override void setBlockAt(int x, int y, int z, byte id, bool force) { this.setBlockAt(x, y, z, VanillaChunk.byteToString[id], force); }
        public override void setBlockAt(Vector3 loc, string name, bool force)     { this.setBlockAt((int)loc.x, (int)loc.y, (int)loc.z, name, force); }
    }
}
