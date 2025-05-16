using System.Collections.Generic;
using Exiled.API.Interfaces;
using UnityEngine;

namespace JailRaSystem;

public class Config : IConfig
{
    public bool IsEnabled { get; set; } = true;

    public bool Debug { get; set; }
    public List<RoomPosition> room_postion { get; set; } = new List<RoomPosition>()
    {
        new RoomPosition(new Vector3(40f, 314.080f, -32.600f))
    };
}

public class RoomPosition
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
    
    public RoomPosition()
    {
    }
    
    public RoomPosition(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }
}