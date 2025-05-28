using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    private byte buttonsPressed;

    public Vector2 Direction;
    public Vector2 LookDirection;
    public bool Shoot;

    public void AddInput(NetworkInputType inputType)
    {
        byte flag = (byte)(1 << (int)inputType);
        buttonsPressed |= flag;
    }

    public readonly bool IsInputDown(NetworkInputType inputType)
    {
        byte flag = (byte)(1 << (int)inputType);
        return (buttonsPressed & flag) != 0;
    }
}
