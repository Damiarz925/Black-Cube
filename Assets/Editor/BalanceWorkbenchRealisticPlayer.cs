using System;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class RealisticPlayerResult
    {
        public PlayerBuildSnapshot build;
        public PlayerBuildMetrics metrics;
        public int iterations,passivePoints,gearStatesEvaluated;
        public bool satisfiesEnabledFloors;
        public string warning;
    }

    // Alternates the existing production-legal passive search with the full
    // historical-inventory gear search. Each passive pass starts from the same
    // locked allocation so a previous pass cannot permanently reserve points.
    public static class RealisticPlayerOptimizer
    {
        public static RealisticPlayerResult Optimize(PlayerBuildSnapshot source,
            RealisticInventoryResult inventory,OptimizationObjective objective,
            OptimizationConstraints floors,DefenseAdherence adherence,
            int passivePoints,int iterations=2,Func<bool> cancelled=null,
            Action<float> progress=null)
        {
            if(source==null||inventory==null||objective==null)throw new ArgumentNullException();
            floors??=new OptimizationConstraints();
            iterations=Math.Clamp(iterations,1,4);
            passivePoints=Mathf.Clamp(passivePoints,0,100);
            var output=new RealisticPlayerResult{passivePoints=passivePoints};
            var gear=RealisticGearsetOptimizer.Optimize(source,inventory,objective,floors,
                DefenseAdherence.Soft,20,48);
            if(gear.build==null){output.warning=gear.warning;return output;}
            var reference=gear.metrics;
            double bestScore=double.NegativeInfinity;
            PlayerBuildSnapshot bestBuild=null;
            PlayerBuildMetrics bestMetrics=null;
            output.gearStatesEvaluated+=gear.completeSetsEvaluated;
            for(int iteration=0;iteration<iterations;iteration++)
            {
                if(cancelled?.Invoke()==true){output.warning="Cancelled after the last complete gear/passive alternation.";break;}
                var passiveSource=source.Clone();
                passiveSource.equipment=gear.build.equipment;
                int current=iteration;
                var passive=PassiveTreeOptimizer.Optimize(passiveSource,passivePoints,
                    objective,false,1,floors,p=>progress?.Invoke((current+p)/iterations),
                    cancelled,true);
                gear=RealisticGearsetOptimizer.Optimize(passive.build,inventory,objective,
                    floors,DefenseAdherence.Soft,20,48);
                if(gear.build==null){output.warning=gear.warning;break;}
                output.iterations++;
                output.gearStatesEvaluated+=gear.completeSetsEvaluated;
                bool eligible=adherence!=DefenseAdherence.Strict||floors.Accept(gear.metrics);
                double score=RealisticGearsetOptimizer.Score(gear.metrics,reference,objective,floors);
                if(eligible&&score>bestScore)
                {
                    bestScore=score;bestBuild=gear.build.Clone();bestMetrics=gear.metrics;
                }
            }
            output.build=bestBuild;output.metrics=bestMetrics;
            output.satisfiesEnabledFloors=bestMetrics!=null&&floors.Accept(bestMetrics);
            if(bestBuild==null&&string.IsNullOrEmpty(output.warning))
                output.warning="No tested gear/passive combination satisfied the enabled STRICT floors.";
            else if(bestBuild!=null&&!output.satisfiesEnabledFloors)
                output.warning="SOFT defense adherence accepted unmet floors; inspect actual deficits.";
            return output;
        }
    }
}
