﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

[ExecuteInEditMode]
public class UPlayer : MonoBehaviour
{
    [Header("Generation:")]
    public bool genTerrain = true;
    public bool genOSM     = false;

    [Header("Chunk parameters:")]
    public float chunkSize         = 1;
    public int chunkAdjacentLayers = 1;
    public int maxResidualChunks   = 2;

    [Header("World Location:")]
    public double startLat = 0;
    public double startLon = 0;
    public int zoom        = 14;
    private USlippyTile startTile;

    [Header("Clear:")]
    public bool _clearHashMap = false;

    /// debug Gizmos
    [Header("Debug:")]
    public bool debugChunks    = true;
    public bool debugDistances = false;
    public bool debugPositions = false;
    public bool debugOSM       = true;

    private Hashtable map = new Hashtable();
    private int lastChunkX = 0;
    private int lastChunkY = 0;

    void Start ()
    {
        init();
        ServicePointManager.ServerCertificateValidationCallback = UUtils.MyRemoteCertificateValidationCallback;
    }

    public void init()
    {
        startTile = new USlippyTile(startLon, startLat, zoom);
        clearHashMap();
    }

    public void clearHashMap()
    {
        foreach (DictionaryEntry p in map)
        {
            DestroyImmediate((GameObject)p.Value);
        }
        map.Clear();
    }

    public Hashtable getMap()
    {
        return map;
    }

    public bool playerIsInNewChunk()
    {
        int x = UMaths.scaledFloor(chunkSize, transform.position.x);
        int y = UMaths.scaledFloor(chunkSize, transform.position.z);
        if (lastChunkX != x || lastChunkY != y)
        {
            lastChunkX = x;
            lastChunkY = y;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the total quadrants number given the desired 
    /// (Chebyshov distance) adjacent layers.
    /// </summary>
    public int nQuadrants()
    {
        return (int)System.Math.Pow(((2 * chunkAdjacentLayers) + 1), 2);
    }

    /// <summary>
    /// Class for store the (Chebyshov) distance from the player 
    /// to each chunk and then remove the farest chunk/s.
    /// </summary>
    private class distChunk
    {
        public int dist;
        public string chunk;
        public distChunk(int _dist = 0, string _chunk = "")
        {
            dist = _dist;
            chunk = _chunk;
        }
    }

    private void removeResidualChunks()
    {
        int totalAvailableChunks = nQuadrants() + maxResidualChunks;
        List<distChunk> distances = new List<distChunk>();

        foreach (DictionaryEntry p in map)
        {
            GameObject gameObj = (GameObject)p.Value;

            GameObject t = (GameObject)p.Value;
            Int3 chunk = Int3.findChunk(t.transform.position + new Vector3(chunkSize*0.5f, chunkSize * 0.5f, chunkSize * 0.5f), chunkSize);
            Int3 player = Int3.findChunk(transform.position, chunkSize);

            /// distancia de Chebyshov
            /// https://es.wikipedia.org/wiki/Distancia_de_Chebyshov
            Int3 dist = chunk - player;
            dist = dist.abs();

            distances.Add(new distChunk(Mathf.Max(dist.x, dist.z), (string)p.Key));
        }

        List<distChunk> sortedDistances = distances.OrderByDescending(o => o.dist).ToList();

        int i = map.Count;
        foreach (distChunk d in sortedDistances)
        {
            if (i <= totalAvailableChunks) return;
            if (map.ContainsKey(d.chunk))
            {
                DestroyImmediate((GameObject)map[d.chunk]);
                map.Remove(d.chunk);
            }
            i--;
        }
    }

    public static string genTerrainName(int x, int y)
    {
        return "Terrain_(" + x + "," + y + ")";
    }

    public GameObject createTerrain(USlippyTile tile, string key)
    {
        GameObject t = new GameObject(key);

        if (genTerrain)
        {
            /// crete the TerrainData
            TerrainData tData = new TerrainData();
            tData.size = new Vector3(chunkSize / 8, 0, chunkSize / 8); // don't know why 8, but terrain its 8 times bigger than this numbers

            /// add the terrain Collider and the Terrain based in the TerrainData
            TerrainCollider tColliderComp = t.AddComponent<TerrainCollider>();
            Terrain tComp = t.AddComponent<Terrain>();
            tColliderComp.terrainData = tData;
            tComp.terrainData = tData;

            /// change the terrain material
            tComp.materialType = Terrain.MaterialType.BuiltInLegacySpecular;

            /// create and init the UTerrain that will load the height data
            UTerrain uTerrain = t.AddComponent<UTerrain>();
            uTerrain.init(tile, this);
        }

        if (genOSM)
        {
            /// create and init the UTerrain that will load the height data
            OSMChunk osmChunk = t.AddComponent<OSMChunk>();
            osmChunk.init(tile, this);
        }

        if (genOSM && debugOSM)
        {
            /// only for debug visualization
            OSMDebug visualization = t.AddComponent<OSMDebug>();
        }

        return t;
    }

    void Update()
    {
        if(_clearHashMap)
        {
            clearHashMap();
            _clearHashMap = false;
        }

        if(playerIsInNewChunk())
        {
            /// chunk Position
            int x = UMaths.scaledFloor(chunkSize, transform.position.x);
            int y = UMaths.scaledFloor(chunkSize, transform.position.z);

            /// chunk number in the world starting from world position 0,0
            int xNum = (int)(x / chunkSize);
            int yNum = (int)(y / chunkSize);

            /// starting tile
            // Only for debug
            startTile = new USlippyTile(startLon, startLat, zoom);

            /// sapwn all the new needed chunks
            for (int i = -chunkAdjacentLayers; i <= chunkAdjacentLayers; i++)
            {
                for (int j = -chunkAdjacentLayers; j <= chunkAdjacentLayers; j++)
                {
                    USlippyTile sTile = new USlippyTile(xNum + i + startTile.x, yNum + j + startTile.y , zoom);
                    
                    /// This line fix that world is flipped upside down
                    sTile.y = startTile.y + (startTile.y - sTile.y);

                    string key = genTerrainName(sTile.x, sTile.y);
                    if (!map.ContainsKey(key))
                    {
                        GameObject t = createTerrain(sTile, key);

                        Vector3 pos = new Vector3();
                        pos.Set(x + (i * chunkSize), 0, y + (j * chunkSize));
                        t.transform.position = pos;

                        map.Add(key, t);
                    }
                }
            }
            /// after spawning the new chunks, remove the olders that we dont need
            removeResidualChunks();
        }
    }

    /// GIZMOS ////////////////////////////////////////////////////////////////////////////
    void drawRect(Vector3 pos, float size)
    {
        Gizmos.DrawLine(pos, pos + new Vector3(size, 0, 0));
        Gizmos.DrawLine(pos, pos + new Vector3(0, 0, size));
        Gizmos.DrawLine(pos + new Vector3(size, 0, 0), pos + new Vector3(size, 0, size));
        Gizmos.DrawLine(pos + new Vector3(0, 0, size), pos + new Vector3(size, 0, size));
    }

    public void renderRects()
    {
        float xx = transform.position.x % chunkSize;
        float yy = transform.position.z % chunkSize;

        for (int i = -chunkAdjacentLayers; i <= chunkAdjacentLayers; i++)
        {
            for (int j = -chunkAdjacentLayers; j <= chunkAdjacentLayers; j++)
            {
                Vector3 chunkPos = new Vector3(xx + (chunkSize * i) + (transform.position.x < 0 ? chunkSize : 0), 0, yy + (chunkSize * j) + (transform.position.z < 0 ? chunkSize : 0));
                chunkPos = transform.position - chunkPos;
                drawRect(chunkPos, chunkSize);
            }
        }
    }

    public void drawPoints(float size = 1)
    {
        int x = UMaths.scaledFloor(chunkSize, transform.position.x);
        int y = UMaths.scaledFloor(chunkSize, transform.position.z);

        for (int i = -chunkAdjacentLayers; i <= chunkAdjacentLayers; i++)
        {
            for (int j = -chunkAdjacentLayers; j <= chunkAdjacentLayers; j++)
            {
                Vector3 chunkPos = new Vector3(x + (i * chunkSize), 0, y + (j * chunkSize));
                Gizmos.DrawSphere(chunkPos, size);
            }
        }
    }

    public void drawDistances()
    {
        float halfHypot = (float)(chunkSize/2.0f);
        foreach (DictionaryEntry p in map)
        {
            GameObject gameObj = (GameObject)p.Value;
            Gizmos.DrawLine(transform.position, gameObj.transform.position + new Vector3(halfHypot, 0, halfHypot));
        }
    }

    private void OnDrawGizmos()
    {
        if (debugChunks)
        {
            Gizmos.color = Color.green;
            renderRects();
        }
        if (debugDistances)
        {
            Gizmos.color = Color.cyan;
            drawDistances();
        }
        if (debugPositions)
        {
            Gizmos.color = Color.blue;
            drawPoints(chunkSize * 0.025f);
        }
        Gizmos.DrawIcon(transform.position, "UshiaLocation.png");
    }
}