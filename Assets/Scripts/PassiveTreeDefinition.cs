// Step 17 authoritative Passive Tree V2 data. Compact templates generate stable,
// designer-readable nodes and explicit Cartesian layout data.
using System;
using System.Collections.Generic;
using UnityEngine;

public enum PassiveBranch
{
    Defense,Life,Mana,Magic,Lightning,Fire,Poison,Projectile,Physical,Cold,
    IncreasedProjectileAmount,AttackSpeed,BleedChance,PoisonChance,ChillChance,
    IgniteChance,ShockChance,ChanceToHitTwice,LifeRegeneration,ManaRegeneration,
    CriticalChance,CriticalMultiplier,LifeOnHit,ManaOnHit,LifeOnKill,ManaOnKill,
    Strength,Dexterity,Intelligence,CastSpeed,ProjectileSpeed,PrecisionChance,
    PrecisionDamage,RageGeneration,RageEffect,RageRetention,EmptyTravel
}
public enum PassiveNodeSize{Small,Medium,Large}
public enum PassiveNodeKind{ClassStart,Travel,Small,Notable,Keystone}
public enum PassiveRegion
{
    Warrior,Ranger,Thief,Mage,Priest,Barbarian,
    WarriorRanger,RangerThief,ThiefMage,MagePriest,PriestBarbarian,BarbarianWarrior,Center
}
public enum PassiveKeystone
{
    None,BruteForce,InfernalConversion,VenomousTransmutation,ManaShield,LivingCurrent,RageFinisher,
    // Retired V1 identities remain source-compatible but are absent from V2 data.
    IronBastion,LivingFortress,ArcaneOverload,AbsoluteZero,BallisticBarrage,OpenWounds,Wildfire,
    DeepFreeze,Overcharged,ToxicSaturation,UndyingFlesh,EndlessCurrent,Frenzy,BulletHell,EchoingStrikes
}

[Serializable] public readonly struct PassiveEffect
{
    public readonly StatTypes Stat;public readonly float Amount;
    public PassiveEffect(StatTypes stat,float amount){Stat=stat;Amount=amount;}
}
public readonly struct PassiveNodeDefinition
{
    public readonly int Id,Position,PrerequisiteId;public readonly string StableId,DisplayName,Description,WeaponTypeRestriction;
    public readonly PassiveBranch Branch;public readonly PassiveNodeSize Size;public readonly PassiveNodeKind Kind;
    public readonly PassiveRegion Region;public readonly Vector2 LayoutPosition;public readonly float Magnitude;
    public readonly PassiveKeystone Keystone;public readonly PassiveEffect[] Effects;public readonly PassiveExtensionMetadata ExtensionMetadata;
    public PassiveNodeDefinition(int id,string stable,string name,PassiveBranch branch,int position,PassiveNodeSize size,
        PassiveNodeKind kind,PassiveRegion region,Vector2 layout,int prerequisite,PassiveEffect[] effects,string weapon=null,
        PassiveKeystone keystone=PassiveKeystone.None,PassiveExtensionMetadata metadata=null,string description=null)
    {Id=id;StableId=stable;DisplayName=name;Branch=branch;Position=position;Size=size;Kind=kind;Region=region;LayoutPosition=layout;
        PrerequisiteId=prerequisite;Effects=effects??Array.Empty<PassiveEffect>();WeaponTypeRestriction=weapon??string.Empty;
        Keystone=keystone;ExtensionMetadata=metadata??new PassiveExtensionMetadata();Description=description??string.Empty;
        Magnitude=Effects.Length>0?Effects[0].Amount:0f;}
}
public readonly struct PassiveTreeEdge{public readonly int A,B;public PassiveTreeEdge(int a,int b){A=a;B=b;}}

public static class PassiveTreeDefinition
{
    public const int ClassSectorCount=6,NodesPerSector=44,BridgeRegionCount=6,BridgeNodesPerRegion=10,CenterNodeCount=42;
    public const int NodeCount=ClassSectorCount*NodesPerSector+BridgeRegionCount*BridgeNodesPerRegion+CenterNodeCount;
    public const int PointCost=1;public const float WeaponSpecificEfficiencyMultiplier=1.60f;
    // Compatibility values for atlas/tests that predate the V2 topology.
    public const int OriginalBranchCount=10,BridgeBranchCount=10,BranchCount=37,OriginalNodesPerBranch=NodesPerSector,NodesPerBranch=NodesPerSector;
    public const int RingNodesPerGap=0,RingNodeCount=0,OriginalNodeCount=ClassSectorCount*NodesPerSector,ExistingNodeCount=NodeCount;
    public const int InnerKeystoneNodeCount=6,OuterKeystoneNodeCount=0,KeystoneNodeCount=6,KeystoneStartId=0;
    public const float BranchAngleDegrees=60f;

    static readonly List<PassiveNodeDefinition> buildingNodes=new(NodeCount);static readonly List<PassiveTreeEdge> buildingEdges=new(NodeCount+80);
    static readonly Dictionary<string,int> stableIds=new(StringComparer.Ordinal);static readonly int[] starts=new int[6],travelEnds=new int[6];
    static readonly string[] classes={PlayerClassIds.Warrior,PlayerClassIds.Ranger,PlayerClassIds.Thief,PlayerClassIds.Mage,PlayerClassIds.Priest,PlayerClassIds.Barbarian};
    static readonly string[] slugs={"warrior","ranger","thief","mage","priest","barbarian"};static readonly float[] angles={90,30,-30,-90,-150,150};
    static readonly string[] weapons={WeaponTypeIds.Sword,WeaponTypeIds.Bow,WeaponTypeIds.Dagger,WeaponTypeIds.Staff,WeaponTypeIds.Sceptre,WeaponTypeIds.TwoHandedAxe};
    static readonly PassiveNodeDefinition[] nodes;static readonly PassiveTreeEdge[] edges;static readonly int[][] adjacency;
    static PassiveTreeDefinition(){for(int i=0;i<6;i++)BuildSector(i);for(int i=0;i<6;i++)BuildBridge(i);BuildCenter();nodes=buildingNodes.ToArray();edges=buildingEdges.ToArray();adjacency=BuildAdjacency();if(nodes.Length!=NodeCount)throw new InvalidOperationException("Passive Tree V2 node count mismatch.");}
    public static IReadOnlyList<PassiveNodeDefinition> Nodes=>nodes;public static IReadOnlyList<PassiveTreeEdge> Edges=>edges;
    public static PassiveNodeDefinition Node(int id)=>id>=0&&id<NodeCount?nodes[id]:throw new ArgumentOutOfRangeException(nameof(id));
    public static bool TryNode(string stable,out PassiveNodeDefinition node){if(stable!=null&&stableIds.TryGetValue(stable,out int id)){node=nodes[id];return true;}node=default;return false;}
    public static int NodeId(string stable)=>stable!=null&&stableIds.TryGetValue(stable,out int id)?id:-1;
    public static int StartNodeId(string classId){for(int i=0;i<classes.Length;i++)if(classes[i]==classId)return starts[i];return -1;}
    public static bool IsClassStart(int id)=>id>=0&&id<NodeCount&&nodes[id].Kind==PassiveNodeKind.ClassStart;
    public static bool IsKeystone(int id)=>id>=0&&id<NodeCount&&nodes[id].Kind==PassiveNodeKind.Keystone;
    public static bool IsRootConnected(int id,string classId){int start=StartNodeId(classId);if(start<0||id<0||id>=NodeCount)return false;foreach(int x in adjacency[start])if(x==id)return true;return false;}
    public static bool IsRootConnected(int id)=>IsRootConnected(id,PlayerClassIds.Warrior);public static IReadOnlyList<int> AdjacentNodeIds(int id)=>adjacency[id];
    public static bool IsOriginalBranch(PassiveBranch branch)=>false;public static bool IsBridgeBranch(PassiveBranch branch)=>false;
    public static bool IsRingBranch(PassiveBranch branch)=>branch==PassiveBranch.EmptyTravel;
    public static int NodesInBranch(PassiveBranch branch){int c=0;foreach(var n in nodes)if(n.Branch==branch)c++;return c;}
    public static int NodeId(PassiveBranch branch,int position){int c=0;foreach(var n in nodes)if(n.Branch==branch&&c++==position)return n.Id;throw new ArgumentOutOfRangeException(nameof(position));}
    public static int TerminalNodeId(PassiveBranch branch)=>NodeId(branch,NodesInBranch(branch)-1);
    public static PassiveKeystone KeystoneFor(PassiveBranch branch){foreach(var n in nodes)if(n.Branch==branch&&n.Keystone!=PassiveKeystone.None)return n.Keystone;return PassiveKeystone.None;}
    public static int KeystoneNodeId(PassiveBranch branch){foreach(var n in nodes)if(n.Branch==branch&&n.Keystone!=PassiveKeystone.None)return n.Id;return -1;}
#if UNITY_EDITOR
    public static int FindIndexForTest(PassiveKeystone keystone){foreach(var n in nodes)if(n.Keystone==keystone)return n.Id;return -1;}
#endif
    public static PassiveBranch OuterKeystoneBranch(int index)=>throw new ArgumentOutOfRangeException(nameof(index));public static PassiveKeystone OuterKeystoneFor(PassiveBranch branch)=>PassiveKeystone.None;public static int OuterKeystoneNodeId(PassiveBranch branch)=>-1;
    [Obsolete("Passive Tree V2 has explicit bridge regions.")] public static PassiveBranch BridgeAtClockwiseGap(int gap)=>PassiveBranch.EmptyTravel;
    [Obsolete("Passive Tree V2 has explicit bridge regions.")] public static void BridgeEndpoints(PassiveBranch branch,out PassiveBranch left,out PassiveBranch right){left=right=PassiveBranch.EmptyTravel;}
    public static string KeystoneName(PassiveKeystone k)=>k switch{PassiveKeystone.BruteForce=>"Titanic Blows",PassiveKeystone.InfernalConversion=>"Infernal Conversion",PassiveKeystone.VenomousTransmutation=>"Venomous Transmutation",PassiveKeystone.ManaShield=>"Mana Shield",PassiveKeystone.LivingCurrent=>"Living Current",PassiveKeystone.RageFinisher=>"Rage Finisher",_=>string.Empty};
    public static string KeystoneEffect(PassiveKeystone k)=>k switch{PassiveKeystone.BruteForce=>"Large physical hits gain power while attack cadence is slower.",PassiveKeystone.InfernalConversion=>"50% of non-Fire hit damage converts to Fire; deal no non-Fire damage.",PassiveKeystone.VenomousTransmutation=>"Hits deal no direct damage; their hit basis becomes Poison damage.",PassiveKeystone.ManaShield=>"50% of damage is taken from Mana before Life; 25% less maximum Life.",PassiveKeystone.LivingCurrent=>"50% of non-Lightning hit damage converts to Lightning; deal no non-Lightning damage.",PassiveKeystone.RageFinisher=>"At 100 Rage, arm the next attack for 100% more damage and consume all Rage.",_=>string.Empty};
    public static string DisplayName(PassiveBranch b)=>b switch{PassiveBranch.Poison=>"Void Damage",PassiveBranch.IncreasedProjectileAmount=>"Additional Projectiles",PassiveBranch.ChanceToHitTwice=>"Hit Twice",PassiveBranch.CriticalChance=>"Critical Chance",PassiveBranch.CriticalMultiplier=>"Critical Multiplier",PassiveBranch.PrecisionChance=>"Projectile Precision",PassiveBranch.PrecisionDamage=>"Precision Damage",_=>b.ToString()};
    public static string GameplayMeaning(PassiveBranch b)=>DisplayName(b);public static bool UsesPercentDisplay(PassiveBranch b)=>b is not(PassiveBranch.LifeRegeneration or PassiveBranch.ManaRegeneration or PassiveBranch.LifeOnHit or PassiveBranch.ManaOnHit or PassiveBranch.LifeOnKill or PassiveBranch.ManaOnKill or PassiveBranch.IncreasedProjectileAmount or PassiveBranch.EmptyTravel);

    static void BuildSector(int sector)
    {
        Vector2 d=Direction(angles[sector]),t=new(-d.y,d.x);var region=(PassiveRegion)sector;string slug=slugs[sector];
        int start=Add($"tree.v2.{slug}.start",Title(slug)+" Start",PassiveBranch.EmptyTravel,PassiveNodeKind.ClassStart,region,d*2520,-1,Array.Empty<PassiveEffect>(),description:"Class anchor; costs no passive point.");starts[sector]=start;int previous=start;
        for(int i=0;i<7;i++){var effects=TravelEffects(sector,i);var branch=BranchFor(effects[0].Stat);int id=Add($"tree.v2.{slug}.travel.{i+1:00}","Attribute Travel",branch,PassiveNodeKind.Travel,region,d*(2300-i*245),previous,effects);Edge(previous,id);previous=id;}travelEnds[sector]=previous;
        for(int cluster=0;cluster<9;cluster++)
        {
            int attach=start+1+cluster%7;bool weaponCluster=cluster is>=4 and<=7;string weapon=weaponCluster?weapons[sector]:string.Empty;var branch=SectorBranch(sector,cluster);
            float side=(cluster%2==0?1:-1)*(260+90*(cluster/2));Vector2 origin=buildingNodes[attach].LayoutPosition+t*side+d*(cluster%3*35);int last=attach;
            for(int step=0;step<4;step++)
            {
                bool notable=step==3,keystone=notable&&cluster==8;var stone=keystone?SectorKeystone(sector):PassiveKeystone.None;var kind=keystone?PassiveNodeKind.Keystone:notable?PassiveNodeKind.Notable:PassiveNodeKind.Small;
                float amount=SmallValue(branch)*(notable?2:1)*(weaponCluster?WeaponSpecificEfficiencyMultiplier:1);string district=weaponCluster?WeaponSlug(weapon):slug;string stable=$"tree.v2.{district}.{slug}.cluster.{cluster+1:00}.{step+1:00}";
                var effects=keystone?Array.Empty<PassiveEffect>():ClusterEffects(branch,amount,sector,cluster,weaponCluster,notable);
                int id=Add(stable,keystone?KeystoneName(stone):notable?DisplayName(branch)+" Mastery":(weaponCluster?WeaponName(weapon)+" ":string.Empty)+DisplayName(branch),branch,kind,region,origin+d*(step*92),last,effects,weapon,stone,TransformMetadata(stable,sector,weaponCluster),keystone?KeystoneEffect(stone):null);Edge(last,id);last=id;
            }
        }
    }
    static void BuildBridge(int bridge)
    {
        int next=(bridge+1)%6,previous=travelEnds[bridge];Vector2 from=buildingNodes[previous].LayoutPosition,to=buildingNodes[travelEnds[next]].LayoutPosition;var region=(PassiveRegion)(6+bridge);
        for(int i=0;i<10;i++){float f=(i+1f)/11;Vector2 p=Vector2.Lerp(from,to,f)*(1+.08f*Mathf.Sin(f*Mathf.PI));var branch=BridgeBranch(bridge,i);int id=Add($"tree.v2.bridge.{region.ToString().ToLowerInvariant()}.{i+1:00}",DisplayName(branch),branch,i==9?PassiveNodeKind.Notable:PassiveNodeKind.Small,region,p,previous,new[]{EffectFor(branch,SmallValue(branch)*(i==9?2:1))});Edge(previous,id);previous=id;}Edge(previous,travelEnds[next]);
    }
    static void BuildCenter()
    {
        int[] inner=new int[6];for(int s=0;s<6;s++){Vector2 d=Direction(angles[s]);int previous=travelEnds[s];for(int i=0;i<5;i++){var branch=i%2==0?PassiveBranch.EmptyTravel:SectorBranch(s,i);int id=Add($"tree.v2.center.{slugs[s]}.{i+1:00}",branch==PassiveBranch.EmptyTravel?"Confluence Travel":DisplayName(branch),branch,i==4?PassiveNodeKind.Notable:PassiveNodeKind.Travel,PassiveRegion.Center,d*(760-i*120),previous,branch==PassiveBranch.EmptyTravel?Array.Empty<PassiveEffect>():new[]{EffectFor(branch,SmallValue(branch))});Edge(previous,id);previous=id;}inner[s]=previous;}
        int[] ring=new int[12];for(int i=0;i<12;i++){var branch=i%3==0?PassiveBranch.CastSpeed:i%3==1?PassiveBranch.Life:PassiveBranch.Mana;ring[i]=Add($"tree.v2.center.ring.{i+1:00}","Central "+DisplayName(branch),branch,i%2==0?PassiveNodeKind.Small:PassiveNodeKind.Travel,PassiveRegion.Center,Direction(90-i*30)*190,-1,new[]{EffectFor(branch,SmallValue(branch))});if(i>0)Edge(ring[i-1],ring[i]);}Edge(ring[11],ring[0]);for(int i=0;i<6;i++)Edge(inner[i],ring[i*2]);
    }
    static int Add(string stable,string name,PassiveBranch branch,PassiveNodeKind kind,PassiveRegion region,Vector2 position,int prerequisite,PassiveEffect[] effects,string weapon=null,PassiveKeystone keystone=PassiveKeystone.None,PassiveExtensionMetadata metadata=null,string description=null)
    {int id=buildingNodes.Count;if(stableIds.ContainsKey(stable))throw new InvalidOperationException("Duplicate passive ID: "+stable);stableIds.Add(stable,id);var size=kind==PassiveNodeKind.Notable?PassiveNodeSize.Medium:kind is PassiveNodeKind.Keystone or PassiveNodeKind.ClassStart?PassiveNodeSize.Large:PassiveNodeSize.Small;buildingNodes.Add(new(id,stable,name,branch,id,size,kind,region,position,prerequisite,effects,weapon,keystone,metadata,description));return id;}
    static void Edge(int a,int b){buildingEdges.Add(new(a,b));}static Vector2 Direction(float a){float r=a*Mathf.Deg2Rad;return new(Mathf.Cos(r),Mathf.Sin(r));}static string Title(string s)=>char.ToUpperInvariant(s[0])+s.Substring(1);static string WeaponSlug(string s)=>s.Replace("weapon.",string.Empty);static string WeaponName(string id)=>WeaponTypeCatalog.TryGet(id,out var x)?x.DisplayName:"Weapon";
    static PassiveExtensionMetadata TransformMetadata(string stable,int sector,bool specialized)=>new(){StableSectionId=stable.Substring(0,stable.LastIndexOf('.')),ClassStartIds=new[]{classes[sector]},SpecializationGroupId=specialized?"weapon":"generic",Affinities=Array.Empty<PassiveAffinity>()};
    static PassiveEffect[] TravelEffects(int s,int i)
    {
        StatTypes a=s switch{0 or 5=>StatTypes.Strength,1 or 2=>StatTypes.Dexterity,_=>StatTypes.Intelligence};
        StatTypes b=s switch{0=>StatTypes.Dexterity,2=>StatTypes.Intelligence,4=>StatTypes.Strength,_=>a};
        var effects=new List<PassiveEffect>(4);
        if(i%3==2&&a!=b){effects.Add(new PassiveEffect(a,3));effects.Add(new PassiveEffect(b,3));}
        else effects.Add(new PassiveEffect(a,5));

        // Every sector offers modest nearby access to broadly useful mechanics.
        // These are intentionally secondary to the attribute travel and much
        // weaker than each region's themed clusters.
        switch(i)
        {
            case 0:effects.Add(new PassiveEffect(StatTypes.LifePercent,2));break;
            case 1:effects.Add(new PassiveEffect(StatTypes.ManaPercent,2));break;
            case 2:effects.Add(new PassiveEffect(StatTypes.CritChance,2));break;
            case 3:effects.Add(new PassiveEffect(StatTypes.AttackSpeed,2));break;
            case 4:effects.Add(new PassiveEffect(StatTypes.LifeOnHit,1));effects.Add(new PassiveEffect(StatTypes.ManaOnHit,1));break;
            case 5:effects.Add(new PassiveEffect(StatTypes.ArmourPercent,2));break;
            case 6:effects.Add(new PassiveEffect(StatTypes.ChanceToHitTwice,1));break;
        }
        return effects.ToArray();
    }
    static PassiveBranch SectorBranch(int s,int c)=>s switch{
        0=>new[]{PassiveBranch.Life,PassiveBranch.Defense,PassiveBranch.ChanceToHitTwice,PassiveBranch.Physical,PassiveBranch.AttackSpeed,PassiveBranch.CriticalChance,PassiveBranch.LifeOnHit,PassiveBranch.BleedChance,PassiveBranch.Physical}[c],
        1=>new[]{PassiveBranch.Projectile,PassiveBranch.AttackSpeed,PassiveBranch.CriticalChance,PassiveBranch.Lightning,PassiveBranch.PrecisionChance,PassiveBranch.PrecisionDamage,PassiveBranch.ProjectileSpeed,PassiveBranch.IncreasedProjectileAmount,PassiveBranch.ShockChance}[c],
        2=>new[]{PassiveBranch.CriticalChance,PassiveBranch.CriticalMultiplier,PassiveBranch.PoisonChance,PassiveBranch.Poison,PassiveBranch.AttackSpeed,PassiveBranch.PoisonChance,PassiveBranch.Poison,PassiveBranch.ManaOnHit,PassiveBranch.PoisonChance}[c],
        3=>new[]{PassiveBranch.Mana,PassiveBranch.ManaRegeneration,PassiveBranch.Fire,PassiveBranch.Cold,PassiveBranch.CastSpeed,PassiveBranch.Mana,PassiveBranch.ManaRegeneration,PassiveBranch.CriticalChance,PassiveBranch.Fire}[c],
        4=>new[]{PassiveBranch.LifeRegeneration,PassiveBranch.ManaRegeneration,PassiveBranch.Defense,PassiveBranch.Cold,PassiveBranch.LifeOnHit,PassiveBranch.ManaOnHit,PassiveBranch.Cold,PassiveBranch.ChillChance,PassiveBranch.Mana}[c],
        _=>new[]{PassiveBranch.Life,PassiveBranch.LifeRegeneration,PassiveBranch.Physical,PassiveBranch.BleedChance,PassiveBranch.RageGeneration,PassiveBranch.RageEffect,PassiveBranch.RageRetention,PassiveBranch.LifeOnHit,PassiveBranch.RageEffect}[c]};
    static PassiveBranch BridgeBranch(int b,int i)=>b switch{0=>i%3==0?PassiveBranch.ChanceToHitTwice:i%3==1?PassiveBranch.AttackSpeed:PassiveBranch.CriticalChance,1=>i%2==0?PassiveBranch.CriticalChance:PassiveBranch.PrecisionChance,2=>i%2==0?PassiveBranch.Poison:PassiveBranch.ManaOnHit,3=>i%2==0?PassiveBranch.ManaRegeneration:PassiveBranch.ChillChance,4=>i%2==0?PassiveBranch.LifeRegeneration:PassiveBranch.LifeOnHit,_=>i%2==0?PassiveBranch.Physical:PassiveBranch.BleedChance};
    static PassiveKeystone SectorKeystone(int s)=>s switch{0=>PassiveKeystone.BruteForce,1=>PassiveKeystone.LivingCurrent,2=>PassiveKeystone.VenomousTransmutation,3=>PassiveKeystone.InfernalConversion,4=>PassiveKeystone.ManaShield,_=>PassiveKeystone.RageFinisher};
    static float SmallValue(PassiveBranch b)=>b switch{PassiveBranch.LifeRegeneration or PassiveBranch.ManaRegeneration or PassiveBranch.LifeOnHit or PassiveBranch.ManaOnHit or PassiveBranch.LifeOnKill or PassiveBranch.ManaOnKill=>2,PassiveBranch.IncreasedProjectileAmount=>1,PassiveBranch.ChanceToHitTwice=>3,PassiveBranch.CriticalMultiplier=>8,PassiveBranch.CriticalChance=>6,PassiveBranch.PrecisionChance=>5,PassiveBranch.PrecisionDamage=>10,PassiveBranch.RageGeneration or PassiveBranch.RageEffect or PassiveBranch.RageRetention=>8,_=>5};
    static PassiveEffect EffectFor(PassiveBranch b,float v)=>new(StatFor(b),v);
    static StatTypes StatFor(PassiveBranch b)=>b switch{PassiveBranch.Defense=>StatTypes.ArmourPercent,PassiveBranch.Life=>StatTypes.LifePercent,PassiveBranch.Mana=>StatTypes.ManaPercent,PassiveBranch.Magic=>StatTypes.MagicDmg,PassiveBranch.Lightning=>StatTypes.LightDmg,PassiveBranch.Fire=>StatTypes.FireDmg,PassiveBranch.Poison=>StatTypes.VoidDmg,PassiveBranch.Projectile=>StatTypes.ProjectileDmg,PassiveBranch.Physical=>StatTypes.PhysDmg,PassiveBranch.Cold=>StatTypes.ColdDmg,PassiveBranch.IncreasedProjectileAmount=>StatTypes.ProjectileAmount,PassiveBranch.AttackSpeed=>StatTypes.AttackSpeed,PassiveBranch.BleedChance=>StatTypes.BleedChance,PassiveBranch.PoisonChance=>StatTypes.PoisonChance,PassiveBranch.ChillChance=>StatTypes.ChillChance,PassiveBranch.IgniteChance=>StatTypes.IgniteChance,PassiveBranch.ShockChance=>StatTypes.ShockChance,PassiveBranch.ChanceToHitTwice=>StatTypes.ChanceToHitTwice,PassiveBranch.LifeRegeneration=>StatTypes.LifeRegeneration,PassiveBranch.ManaRegeneration=>StatTypes.ManaRegeneration,PassiveBranch.CriticalChance=>StatTypes.CritChance,PassiveBranch.CriticalMultiplier=>StatTypes.CritMult,PassiveBranch.LifeOnHit=>StatTypes.LifeOnHit,PassiveBranch.ManaOnHit=>StatTypes.ManaOnHit,PassiveBranch.LifeOnKill=>StatTypes.LifeOnKill,PassiveBranch.ManaOnKill=>StatTypes.ManaOnKill,PassiveBranch.Strength=>StatTypes.Strength,PassiveBranch.Dexterity=>StatTypes.Dexterity,PassiveBranch.Intelligence=>StatTypes.Intelligence,PassiveBranch.CastSpeed=>StatTypes.CastSpeed,PassiveBranch.ProjectileSpeed=>StatTypes.ProjectileSpeed,PassiveBranch.PrecisionChance=>StatTypes.ProjectilePrecisionChance,PassiveBranch.PrecisionDamage=>StatTypes.ProjectilePrecisionMultiplier,PassiveBranch.RageGeneration=>StatTypes.RageGeneration,PassiveBranch.RageEffect=>StatTypes.RageEffect,PassiveBranch.RageRetention=>StatTypes.RageDecayReduction,_=>StatTypes.GenericDmg};
    static PassiveEffect[] ClusterEffects(PassiveBranch branch,float amount,int sector,int cluster,bool weapon,bool notable)
    {
        var effects=new List<PassiveEffect>{EffectFor(branch,amount)};
        if(weapon&&sector==5&&cluster==4)effects.Add(new PassiveEffect(StatTypes.PhysDmg,amount*.5f));
        if(weapon&&sector==5&&cluster==6)effects.Add(new PassiveEffect(StatTypes.BleedChance,amount*.5f));
        if(weapon&&sector==3&&cluster==4)effects.Add(new PassiveEffect(StatTypes.MagicDmg,amount*.5f));
        if(weapon&&sector==3&&cluster==5)effects.Add(new PassiveEffect(StatTypes.ManaOnHit,Mathf.Max(1,amount*.15f)));
        if(weapon&&sector==3&&cluster==7)effects.Add(new PassiveEffect(StatTypes.MagicDmg,amount*.5f));
        if(weapon&&sector==1&&cluster==4)effects.Add(new PassiveEffect(StatTypes.CritChance,amount*.5f));
        if(weapon&&sector==1&&cluster==5)effects.Add(new PassiveEffect(StatTypes.ProjectileDmg,amount*.5f));
        if(weapon&&sector==1&&cluster==6)effects.Add(new PassiveEffect(StatTypes.AttackSpeed,amount*.5f));
        if(notable&&TryCompanion(branch,out var companion))effects.Add(new PassiveEffect(companion,Mathf.Max(1,SmallValue(branch)*.75f)));
        return effects.ToArray();
    }
    static bool TryCompanion(PassiveBranch branch,out StatTypes stat)
    {
        stat=branch switch{PassiveBranch.Life=>StatTypes.LifeRegeneration,PassiveBranch.Mana=>StatTypes.ManaRegeneration,PassiveBranch.Defense=>StatTypes.LifePercent,PassiveBranch.Physical=>StatTypes.BleedChance,PassiveBranch.Fire=>StatTypes.IgniteChance,PassiveBranch.Cold=>StatTypes.ChillChance,PassiveBranch.Lightning=>StatTypes.ShockChance,PassiveBranch.CriticalChance=>StatTypes.CritMult,PassiveBranch.CriticalMultiplier=>StatTypes.CritChance,PassiveBranch.AttackSpeed=>StatTypes.ChanceToHitTwice,PassiveBranch.Projectile=>StatTypes.ProjectileSpeed,PassiveBranch.PrecisionChance=>StatTypes.ProjectilePrecisionMultiplier,PassiveBranch.PrecisionDamage=>StatTypes.ProjectilePrecisionChance,PassiveBranch.RageGeneration=>StatTypes.RageEffect,PassiveBranch.RageEffect=>StatTypes.RageDecayReduction,_=>(StatTypes)(-1)};
        return (int)stat>=0;
    }
    static PassiveBranch BranchFor(StatTypes s)=>s switch{StatTypes.Strength=>PassiveBranch.Strength,StatTypes.Dexterity=>PassiveBranch.Dexterity,StatTypes.Intelligence=>PassiveBranch.Intelligence,_=>PassiveBranch.EmptyTravel};
    static int[][] BuildAdjacency(){var x=new List<int>[NodeCount];for(int i=0;i<NodeCount;i++)x[i]=new();foreach(var e in edges){x[e.A].Add(e.B);x[e.B].Add(e.A);}var r=new int[NodeCount][];for(int i=0;i<NodeCount;i++)r[i]=x[i].ToArray();return r;}
}
