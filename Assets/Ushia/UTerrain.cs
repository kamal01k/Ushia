﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Terrain))]
public class UTerrain : MonoBehaviour {

    public static float terrainDepth = -4400;
    public static float terrainHeight = 8848;

    public bool generated = false;

    private Terrain terrain;
    private UTerrainLoader loader = null;
    public USlippyTile tile;
    private UPlayer player;

    private bool borderXFixed = false;
    private bool borderYFixed = false;

    private Terrain[] neightbors; /// left, top, right, bottom

    public void init(USlippyTile _tile, UPlayer pl)
    {
        terrain = GetComponent<Terrain>();
        loader = new UTerrainLoader();
        neightbors = new Terrain[4];
        tile = _tile;
        loader.tile = _tile;
        loader.Start();
        player = pl;
    }

    /// <summary>
    /// Set terrain neightbors that fixes the LOD
    /// </summary>
    private void updateNeightbors()
    {
        /// left, top, right, bottom
        GetComponent<Terrain>().SetNeighbors(neightbors[0], neightbors[1], neightbors[2], neightbors[3]);
    }

    public void setLeftTerrain(Terrain t)
    {
        neightbors[0] = t;
        updateNeightbors();
    }

    public void setTopTerrain(Terrain t)
    {
        neightbors[1] = t;
        updateNeightbors();
    }

    public void setRightTerrain(Terrain t)
    {
        neightbors[2] = t;
        updateNeightbors();
    }

    public void setBottomTerrain(Terrain t)
    {
        neightbors[3] = t;
        updateNeightbors();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t">Northern (Top) terrain</param>
    public void fixBorderX(Terrain t)
    {
        /// get this Terrain's height data
        TerrainData data  = GetComponent<Terrain>().terrainData;
        float[,] heights  = data.GetHeights(0, 0, data.alphamapWidth, data.alphamapWidth);

        /// get the other Terrain's height data
        TerrainData data2 = t.terrainData;
        float[,] heights2 = data2.GetHeights(0, 0, data.alphamapWidth, data.alphamapWidth);

        /// set the terrain neightbors
        setTopTerrain(t);
        t.GetComponent<UTerrain>().setBottomTerrain(GetComponent<Terrain>());

        for (int i = 0; i < data.alphamapWidth; i++)
            heights[data.alphamapWidth - 1, i] = heights2[0, i];

        /// Apply the border height changes to the terrain data and then update the terrain
        data.SetHeights(0, 0, heights);
        GetComponent<Terrain>().ApplyDelayedHeightmapModification();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t">Eastern (Right) terrain</param>
    public void fixBorderY(Terrain t)
    {
        /// get this Terrain's height data
        TerrainData data = GetComponent<Terrain>().terrainData;
        float[,] heights = data.GetHeights(0, 0, data.alphamapWidth, data.alphamapWidth);

        /// get the other Terrain's height data
        TerrainData data2 = t.terrainData;
        float[,] heights2 = data2.GetHeights(0, 0, data.alphamapWidth, data.alphamapWidth);

        /// set the terrain neightbors
        setRightTerrain(t);
        t.GetComponent<UTerrain>().setLeftTerrain(GetComponent<Terrain>());

        for (int i = 0; i < data.alphamapWidth; i++)
            heights[i, data.alphamapWidth - 1] = heights2[i, 0];

        /// Apply the border height changes to the terrain data and then update the terrain
        data.SetHeights(0, 0, heights);
        GetComponent<Terrain>().ApplyDelayedHeightmapModification();
    }

    public void destroy()
    {
        DestroyImmediate(transform.root.gameObject);
    }

    private void Update()
    {

        if (player == null || tile == null)
        {
            destroy();
        }

        if(loader != null)
        {
            if (loader.Update())
            {
                genHeight();
                generated = true;
                loader = null;

            }
        }

        if(generated && tile != null)
        {
            if (!borderXFixed)
            {
                Hashtable map = player.getMap();
                string topTerrainKey = UPlayer.genTerrainName(tile.x, tile.y-1);
                if (map.ContainsKey(topTerrainKey))
                {
                    GameObject terrain = (GameObject)map[topTerrainKey];
                    if (terrain.GetComponent<UTerrain>().generated)
                    {
                        fixBorderX(terrain.GetComponent<Terrain>());
                        borderXFixed = true;
                    }
                }
            }
            if (!borderYFixed)
            {
                Hashtable map = player.getMap();
                string rightTerrainKey = UPlayer.genTerrainName(tile.x+1, tile.y);
                if (map.ContainsKey(rightTerrainKey))
                {
                    GameObject terrain = (GameObject)map[rightTerrainKey];
                    if (terrain.GetComponent<UTerrain>().generated)
                    {
                        fixBorderY(terrain.GetComponent<Terrain>());
                        borderYFixed = true;
                    }
                }
            }
        }
    }

    private void genHeight()
    {
        terrain = GetComponent<Terrain>();

        Texture2D data = new Texture2D(256, 256);
        //data.LoadImage(HeightLoader.getByteHeight(tile));

        byte[] rawData = new byte[256 * 256];
        rawData = loader.heightData;
        data.LoadImage(rawData);
        data.Apply();

        GetComponent<Transform>().position = new Vector3(GetComponent<Transform>().position.x, terrainDepth, GetComponent<Transform>().position.z);
        TerrainData td = terrain.terrainData;
        td.alphamapResolution = 257;
        td.heightmapResolution = 257;

        td.size = new Vector3(td.size.x, terrainHeight, td.size.z);

        float[,] heights = new float[td.alphamapWidth, td.alphamapWidth];

        for (int i = 0; i < td.alphamapWidth; i++)
        {
            for (int j = 0; j < td.alphamapWidth; j++)
            {
                Color c = data.GetPixel(i, j);
                float r = c.r;
                float g = c.g;
                float b = c.b;
                heights[j, i] = (r * 256 + g + b / 256) / 256;

                /// bad border (?)
                if (i == td.alphamapWidth-1) heights[j, i] = heights[j, td.alphamapWidth-2];
            }
            /// bad border (?)
            heights[td.alphamapWidth-1, i] = heights[td.alphamapWidth-2, i];
        }

        td.SetHeights(0, 0, heights);
        terrain.ApplyDelayedHeightmapModification();
    }
}
