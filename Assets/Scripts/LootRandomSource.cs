// Dedicated random stream for reward generation. Production entropy is deliberately
// unrelated to run, world, encounter, enemy, or save identity.
using System;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;

public interface ILootRandomSource
{
    string SourceName { get; }
    long EventId { get; }
    float Value();
    int Range(int minimumInclusive,int maximumExclusive);
    float Range(float minimumInclusive,float maximumInclusive);
}

public static class LootRandomSourceFactory
{
    static Func<ILootRandomSource> factory=()=>new ProductionLootRandomSource();
    public static ILootRandomSource CreateProduction()=>factory();
#if UNITY_EDITOR
    public static void SetFactoryForTests(Func<ILootRandomSource> replacement)=>factory=replacement??(()=>new ProductionLootRandomSource());
    public static void ResetFactoryForTests()=>factory=()=>new ProductionLootRandomSource();
#endif
}

public sealed class ProductionLootRandomSource:ILootRandomSource
{
    static readonly RandomNumberGenerator entropy=RandomNumberGenerator.Create();
    static readonly object gate=new();
    static long nextEventId;
    readonly byte[] bytes=new byte[4];
    public string SourceName=>"System.Security.Cryptography.RandomNumberGenerator";
    public long EventId{get;}
    public ProductionLootRandomSource(){EventId=Interlocked.Increment(ref nextEventId);}
    public float Value()
    {
        lock(gate)entropy.GetBytes(bytes);
        uint value=BitConverter.ToUInt32(bytes,0)>>8;
        return value/16777216f;
    }
    public int Range(int minimumInclusive,int maximumExclusive)
    {
        if(maximumExclusive<=minimumInclusive)return minimumInclusive;
        return minimumInclusive+Mathf.FloorToInt(Value()*(maximumExclusive-minimumInclusive));
    }
    public float Range(float minimumInclusive,float maximumInclusive)
        =>minimumInclusive+(maximumInclusive-minimumInclusive)*Value();
}

// Compatibility adapter for encounter/build generation and legacy deterministic tests.
// Production death rewards never instantiate this adapter.
public sealed class UnityLootRandomSource:ILootRandomSource
{
    public static readonly UnityLootRandomSource Instance=new();
    public string SourceName=>"UnityEngine.Random";
    public long EventId=>0;
    UnityLootRandomSource(){}
    public float Value()=>UnityEngine.Random.value;
    public int Range(int minimumInclusive,int maximumExclusive)=>UnityEngine.Random.Range(minimumInclusive,maximumExclusive);
    public float Range(float minimumInclusive,float maximumInclusive)=>UnityEngine.Random.Range(minimumInclusive,maximumInclusive);
}

public sealed class SequenceLootRandomSource:ILootRandomSource
{
    readonly float[] values;int index;
    public string SourceName=>"Injected deterministic sequence";
    public long EventId{get;}
    public int ValuesConsumed=>index;
    public SequenceLootRandomSource(long eventId,params float[] sequence){EventId=eventId;values=sequence==null||sequence.Length==0?new[]{0f}:sequence;}
    public float Value(){float value=values[index%values.Length];index++;return Mathf.Clamp(value,0f,.99999994f);}
    public int Range(int minimumInclusive,int maximumExclusive)=>maximumExclusive<=minimumInclusive?minimumInclusive:minimumInclusive+Mathf.FloorToInt(Value()*(maximumExclusive-minimumInclusive));
    public float Range(float minimumInclusive,float maximumInclusive)=>minimumInclusive+(maximumInclusive-minimumInclusive)*Value();
}

public sealed class DelegateLootRandomSource:ILootRandomSource
{
    readonly Func<float> next;
    public string SourceName=>"Injected delegate";
    public long EventId=>0;
    public DelegateLootRandomSource(Func<float> value)=>next=value??(()=>0f);
    public float Value()=>Mathf.Clamp(next(),0f,.99999994f);
    public int Range(int minimumInclusive,int maximumExclusive)=>maximumExclusive<=minimumInclusive?minimumInclusive:minimumInclusive+Mathf.FloorToInt(Value()*(maximumExclusive-minimumInclusive));
    public float Range(float minimumInclusive,float maximumInclusive)=>minimumInclusive+(maximumInclusive-minimumInclusive)*Value();
}

// Reproducible stream for Editor simulations and deterministic parity tests.
// Gameplay continues to obtain ProductionLootRandomSource from the factory above.
public sealed class SeededSimulationRandomSource:ILootRandomSource
{
    ulong state;
    public string SourceName=>"Black-Cube seeded xorshift64* simulation";
    public long EventId{get;}
    public SeededSimulationRandomSource(long seed)
    {
        EventId=seed;
        state=unchecked((ulong)seed)+0x9E3779B97F4A7C15UL;
        if(state==0)state=0xA0761D6478BD642FUL;
    }
    ulong Next()
    {
        ulong x=state;x^=x>>12;x^=x<<25;x^=x>>27;state=x;
        return x*2685821657736338717UL;
    }
    public float Value()=>(Next()>>40)/16777216f;
    public int Range(int minimumInclusive,int maximumExclusive)=>maximumExclusive<=minimumInclusive?minimumInclusive:minimumInclusive+Mathf.FloorToInt(Value()*(maximumExclusive-minimumInclusive));
    public float Range(float minimumInclusive,float maximumInclusive)=>minimumInclusive+(maximumInclusive-minimumInclusive)*Value();
}
