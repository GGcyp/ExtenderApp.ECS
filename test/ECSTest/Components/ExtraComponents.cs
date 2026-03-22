using System.Runtime.InteropServices;
using ExtenderApp.ECS;

namespace ECSTest.Components
{
    public struct Health 
    {
        public int Value;
    }

    public struct Mana 
    {
        public int Value;
    }

    public struct Rotation 
    {
        public float Value;
    }

    public struct Scale 
    {
        public float Value;
    }

    public struct Acceleration 
    {
        public float X;
        public float Y;
    }

    public struct Team 
    {
        public int Id;
    }

    public struct State 
    {
        public int Value;
    }

    public struct Lifetime 
    {
        public float Value;
    }

    [StructLayout(LayoutKind.Sequential, Size = 128)]
    public struct HugePayload 
    {
        public int Seed;
    }
}