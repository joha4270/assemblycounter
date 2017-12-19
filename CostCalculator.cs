using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace assemblycounter
{
    static class CostCalculator
    {
        private static Dictionary<string, int> Cost = new Dictionary<string, int>()
        {
            ["add"] = 1, //Actually more like a half for some, but only if multiple are after eachtoerh (if-im-reading-this-right)
            ["sub"] = 1,
            ["rsb"] = 1, //sub with reversed opands, guessing same as sub
            ["adc"] = 1, //Same 3 ops, with carry
            ["sbc"] = 1,
            ["rsc"] = 1,

            ["mov"] = 1,
            ["cmp"] = 1, //Guess
            ["and"] = 1, //Guess
            ["nop"] = 1, //Guess
            ["str"] = 200, //More like 2, this is worst case L2 cache miss 
            ["lsl"] = 200, //More like 2, this is worst case L2 cache miss
            ["asr"] = 2, //guess
            ["ldr"] = 3,
            ["b"] = 8, //Pipeline is 8 stages deep, going to assume this is the maximum cost. Did not find actual documentation

            ["mul"] = 3,
            ["smull"] = 3,

            ["ldm"] = 10, ["pop"] = 10,     //Second is alias
            ["stm"] = 12, ["push"] = 12,

        };

        public static int WorstCaseCycleCount(Dictionary<string, int> instructions)
        {
            int counter = 0;
            int total = 0;
            foreach (KeyValuePair<string, int> instruction in instructions)
            {
                string opcode = instruction.Key.Split('\t').First();
                string shorter = opcode;

                int cost;
                while (!Cost.TryGetValue(shorter, out cost) && shorter.Length > 0)
                {
                    shorter = opcode.Substring(0, shorter.Length - 1);
                }

                if(cost == 0) Debugger.Break();
                

                total +=cost * instruction.Value;
                counter++;
            }

            return total;
            
        }
    }
}
