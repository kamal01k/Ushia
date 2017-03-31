﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OSMNode
{
    //<node id="1423405850" visible="true" version="1" changeset="9212407" timestamp="2011-09-04T20:47:26Z" user="cfaerber" uid="17085" lat="48.1405398" lon="11.5430526"/>
    //public long id; // Now id is in the hastable

    //      lat
    //       |
    // ------0------ lon
    //       |
    public double lon, lat;

    // +Y
    //  |      
    //  |  +Z
    //  |  /
    //  | /
    //  0 ------- +X
    //public double x, y, z;
    public Vector3 pos;

    /// Using a Dictionary because is more efficient
    /// http://cc.davelozinski.com/c-sharp/fastest-collection-for-string-lookups
    public Dictionary<string, string> tags = new Dictionary<string, string>();

    public OSMNode() { }

    public OSMNode(long _id, double _lon, double _lat)
    {
        //id = _id;
        lon = _lon;
        lat = _lat;
        setVirtualPos();
    }

    public OSMNode(double _lon, double _lat)
    {
        lon = _lon;
        lat = _lat;
        setVirtualPos();
    }

    private void setVirtualPos()
    {
        pos = Vector3.zero;
    }

    /// casts to float
    public float lonf { get { return (float)lon; } }
    public float latf { get { return (float)lat; } }

    public float xf { get { return pos.x; } }
    public float yf { get { return pos.y; } }
    public float zf { get { return pos.z; } }
}
