using System;
using System.Collections.Generic;

namespace BlackCube.BalanceWorkbench
{
    public readonly struct SustainableDamageResult
    {
        public readonly string policy;
        public readonly double totalDps,skillDps,starvationFraction;
        public SustainableDamageResult(string policy,double totalDps,double skillDps,double starvationFraction)
        {this.policy=policy;this.totalDps=totalDps;this.skillDps=skillDps;this.starvationFraction=starvationFraction;}
    }

    // Event-driven fast estimator for optimizer search. Finalists still require
    // Combat Lab validation (enemy mitigation and skill-specific ailments).
    public static class SustainableDamageEstimator
    {
        const double Warmup=15,Measurement=45,Epsilon=1e-6;

        public static SustainableDamageResult Best(PlayerBuildMetrics metrics)
        {
            if(metrics==null)throw new ArgumentNullException(nameof(metrics));
            SustainableDamageResult best=Evaluate(metrics,-1,-1);
            if(metrics.skills.Count==0)return best;
            var policies=new List<(int,int)>{(0,-1)};
            if(metrics.skills.Count>1){policies.Add((1,-1));policies.Add((0,1));policies.Add((1,0));}
            foreach(var policy in policies)
            {
                var result=Evaluate(metrics,policy.Item1,policy.Item2);
                if(result.totalDps>best.totalDps+1e-8)best=result;
            }
            return best;
        }

        public static SustainableDamageResult Evaluate(PlayerBuildMetrics metrics,int first,int second)
        {
            if(metrics==null)throw new ArgumentNullException(nameof(metrics));
            var skills=metrics.skills??new List<SkillAnalyticalMetrics>();
            int[] priority={first,second};
            bool chosen(int index)=>index>=0&&index<skills.Count&&(index==first||index==second);
            string policy=first<0?"No Skills":second<0?$"Skill {first+1} Only":
                $"Skill {first+1} → Skill {second+1}";
            double aps=Math.Max(.01,metrics.attacksPerSecond),period=1/aps;
            double manaCap=Math.Max(0,metrics.mana),mana=manaCap,regen=Math.Max(0,metrics.manaRegen),onHit=Math.Max(0,metrics.manaOnHit);
            double t=0,nextAttack=period,total=0,skillDamage=0,zeroTime=0;
            var ready=new double[skills.Count];
            double basicDamage=Math.Max(0,metrics.basicDps)/aps;
            double critFactor=1+Math.Max(0,metrics.critChance)*(Math.Max(1,metrics.critMultiplier)-1);
            double hitTwiceFactor=1+Math.Max(0,metrics.hitTwiceChance);
            int guard=0;
            while(t<Warmup+Measurement && guard++<20000)
            {
                double next=nextAttack;
                for(int i=0;i<skills.Count;i++)
                {
                    if(!chosen(i)||skills[i].castMode!=PlayerSkillCastMode.AutoCooldown.ToString())continue;
                    double cost=Math.Max(0,skills[i].manaCost);
                    if(cost>manaCap+Epsilon)continue;
                    double candidate=Math.Max(t,ready[i]);
                    double available=Math.Min(manaCap,mana+regen*Math.Max(0,candidate-t));
                    if(available+Epsilon<cost)
                    {
                        if(regen<=Epsilon)continue;
                        candidate+=(cost-available)/regen;
                    }
                    next=Math.Min(next,candidate);
                }
                if(next>Warmup+Measurement)next=Warmup+Measurement;
                if(next<t+Epsilon)next=t+Epsilon;
                double dt=next-t;
                if(mana<=Epsilon&&t>=Warmup)zeroTime+=dt;
                mana=Math.Min(manaCap,mana+regen*dt);
                t=next;
                if(t>=Warmup+Measurement)break;
                foreach(int i in priority)
                {
                    if(!chosen(i)||skills[i].castMode!=PlayerSkillCastMode.AutoCooldown.ToString())continue;
                    var s=skills[i];double cost=Math.Max(0,s.manaCost);
                    if(ready[i]>t+Epsilon||mana+Epsilon<cost)continue;
                    mana=Math.Max(0,mana-cost);
                    ready[i]=t+Math.Max(PlayerSkillController.MinimumAutoCooldown,s.effectiveCooldown);
                    double dealt=Math.Max(0,s.averageDamagePerUse)*critFactor*hitTwiceFactor;
                    if(t>=Warmup){total+=dealt;skillDamage+=dealt;}
                    mana=Math.Min(manaCap,mana+onHit*Math.Max(1,s.expectedHits));
                }
                if(nextAttack<=t+Epsilon)
                {
                    nextAttack=t+period;
                    int queued=-1;
                    foreach(int i in priority)
                        if(chosen(i)&&skills[i].castMode==PlayerSkillCastMode.QueuedAttackReplacement.ToString()
                            &&mana+Epsilon>=Math.Max(0,skills[i].manaCost)){queued=i;break;}
                    if(queued<0)
                    {
                        if(t>=Warmup)total+=basicDamage;
                        mana=Math.Min(manaCap,mana+onHit*hitTwiceFactor);
                    }
                    else
                    {
                        var skill=skills[queued];mana=Math.Max(0,mana-Math.Max(0,skill.manaCost));
                        double dealt=Math.Max(0,skill.averageDamagePerUse)*critFactor*hitTwiceFactor;
                        if(t>=Warmup){total+=dealt;skillDamage+=dealt;}
                        mana=Math.Min(manaCap,mana+onHit*Math.Max(1,skill.expectedHits));
                    }
                }
            }
            double ailmentDps=Math.Max(0,metrics.poisonDps)+Math.Max(0,metrics.bleedDps)+Math.Max(0,metrics.igniteDps);
            return new SustainableDamageResult(policy,total/Measurement+ailmentDps,
                skillDamage/Measurement,Math.Clamp(zeroTime/Measurement,0,1));
        }
    }
}
