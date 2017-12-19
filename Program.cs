using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace assemblycounter
{
    partial class Program
    {
        static void Main(string[] args)
        {
            var root = new MultiCodeSegment(args.Select(ParseFile).Cast<MultiCodeSegment>().Select(x => x));

            
            string entry = "ScanADC";
            var result = root.GetInstructions(entry);

            Console.WriteLine($"Counted {result.Select(x => x.Value).Aggregate((x,y)=> x+y)} instructions, {result.Count} unique executed when entering {entry}");
        }

        private const string MARKER_START_MAGIC_STRING = "@START_OF_LOOP ITERATIONS ";

        static CodeSegment ParseFile(string fileName)
        {
            Stack<MultiCodeSegment> codeStack = new Stack<MultiCodeSegment>();
            codeStack.Push(new MultiCodeSegment());
            
            string currentFunctionName = null;
            List<string> functionLines = new List<string>(256);
            HashSet<string> functionKnownSymbols = new HashSet<string>();
            
            
            foreach (string line in File.ReadAllLines(fileName).Select(x => x.TrimStart()))
            {
                //Assembler directive. I don't think we ever need those
                if(line.StartsWith('.') && !line.EndsWith(':')) 
                    continue;
                
                //Seems to be once at first line, never again?
                if(line.StartsWith("**")) 
                    continue;
                
                //This is (in most cases) a start of a new function (or aux data, but no good way to filter)
                if (line.EndsWith(':') && !line.StartsWith('.'))
                {
                    if (currentFunctionName != null)
                    {
                        if (functionLines.Count == 0)
                        {
                            Console.WriteLine($"Reached end of {currentFunctionName}, no code");
                            codeStack.Pop();
                        }
                        else
                        {
                            codeStack.Pop().Add(new RawCodeSegment(functionLines));
                            
                            Assert.That(codeStack.Count == 1);
                            
                        }
                    }
                    
                    currentFunctionName = line.Substring(0, line.Length - 1);

                    var newfunction = new MultiCodeSegment(currentFunctionName);
                    codeStack.Peek().Add(newfunction);
                    codeStack.Push(newfunction);
                    
                    
                    Console.WriteLine($"Parsing function {currentFunctionName}");
                    
                    continue;
                }

                //Something with a comment. We either ignore it or its a embedded loop specifier (Which we need to handle)
                if (line.StartsWith('@'))
                {
                    if (line.StartsWith(MARKER_START_MAGIC_STRING))
                    {
                        //Save code before the loop in the segment loop will also be contained in
                        codeStack.Peek().Add(new RawCodeSegment(functionLines));

                        string countstring = line.Substring(MARKER_START_MAGIC_STRING.Length,
                            line.Length - MARKER_START_MAGIC_STRING.Length - 4);
                        
                        int loopcount = ExpressionParser.EvaluateCountString(countstring);
                        
                        
                        MultiCodeSegment newSegment = new MultiCodeSegment();
                        codeStack.Peek().Add(new RepeatCodeSegment(newSegment, loopcount));
                        codeStack.Push(newSegment);
                    }
                    else if (line.StartsWith("@END_OF_LOOP"))
                    {
                        codeStack.Pop().Add(new RawCodeSegment(functionLines));
                        
                    }
                    //else ignore
                    
                    continue;
                }

                //Inline label. Save them. If we jump to something seen, there might be a problem (backwards jump)
                if (line.StartsWith('.') && line.EndsWith(':'))
                {
                    functionKnownSymbols.Add(line.Substring(1, line.Length - 2));
                    continue;
                }
                    
                //An actual instruction. Should always happen, but sanity checking
                //That everything in first word is lovercase letters
                if (line.Split((char[]) null, StringSplitOptions.None).First().All(char.IsLower))
                {
                    functionLines.Add(line);
                    continue;
                }
                
                Debugger.Break();
                throw new NotImplementedException();
            }

            Assert.That(codeStack.Count == 2);

            //the current function
            codeStack.Pop(); 
            
            return codeStack.Pop();
        }
    }

    internal static class Assert
    {
        public static void That(bool assertion)
        {
            if(!assertion)
                throw new Exception("Assertion Failed!");
        }
    }
}
