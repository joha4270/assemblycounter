using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Xml;

namespace assemblycounter
{
    public abstract class CodeSegment
    {
        private static int uidcount = 1;
        private int uid;
        public string Tag { get; }

        protected abstract IEnumerable<CodeSegment> GetSelfAndChildren();

        protected internal abstract Dictionary<string, int> GetInstructionsInternal(Dictionary<string, CodeSegment> symbolTable);

        public CodeSegment(string tag)
        {
            Tag = tag;
            uid = uidcount++;
        }

        public Dictionary<string, int> GetInstructions(string entrypointTag)
        {
            var everything = GetSelfAndChildren().ToList();

            var symbolTable = 
                everything
                .Where(x => !string.IsNullOrWhiteSpace(x.Tag))
                .ToDictionary(x => x.Tag);



            return symbolTable[entrypointTag].GetInstructionsInternal(symbolTable);
        }
    }

    public class RawCodeSegment : CodeSegment
    {
        private Dictionary<string, int> instructions = new Dictionary<string, int>();
        
        public RawCodeSegment(IEnumerable<string> assembly, string tag = null) : base(tag)
        {
            if(assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            foreach (var instruction in assembly)
            {
                if (instructions.ContainsKey(instruction))
                    instructions[instruction]++;
                else
                    instructions[instruction] = 1;
                
            }
            
        }

        protected override IEnumerable<CodeSegment> GetSelfAndChildren()
        {
            yield return this;
        }

        protected internal override Dictionary<string, int> GetInstructionsInternal(Dictionary<string, CodeSegment> symbolTable)
        {
            return instructions;
        }
    }

    public class CallingCodeSegment : CodeSegment 
    {
        private string _callTag;

        public CallingCodeSegment(string callTag, string tag = null) : base(tag)
        {
            if(string.IsNullOrWhiteSpace(callTag))
                throw new ArgumentException(nameof(callTag));
            _callTag = callTag;
        }

        protected override IEnumerable<CodeSegment> GetSelfAndChildren()
        {
            yield return this;
        }

        protected internal override Dictionary<string, int> GetInstructionsInternal(Dictionary<string, CodeSegment> symbolTable)
        {
            Dictionary<string, int> results = new Dictionary<string, int>(symbolTable[_callTag].GetInstructionsInternal(symbolTable));
            if (results.ContainsKey("bl"))
            {
                results["bl"] += 1;
            }
            else
            {
                results["bl"] = 1;
            }

            return results;
        }
    }

    public class MultiCodeSegment : CodeSegment, IEnumerable<CodeSegment>
    {
        private List<CodeSegment> _children;

        public MultiCodeSegment(IEnumerable<CodeSegment> children, string tag = null) : base(tag)
        {
            if(children == null)
                throw new ArgumentNullException(nameof(children));
            _children = children.ToList();
        }

        public MultiCodeSegment(string tag = null) : base(tag)
        {
            _children = new List<CodeSegment>();
        }

        protected override IEnumerable<CodeSegment> GetSelfAndChildren()
        {
            yield return this;

            foreach (var child in _children)
            {
                yield return child;   
            }
        }

        protected internal override Dictionary<string, int> GetInstructionsInternal(Dictionary<string, CodeSegment> symbolTable)
        {
            Dictionary<string, int> consolidated = new Dictionary<string, int>();
            foreach (CodeSegment child in _children)
            {
                foreach (var instructions in child.GetInstructionsInternal(symbolTable))
                {
                    if (consolidated.ContainsKey(instructions.Key))
                    {
                        consolidated[instructions.Key] = consolidated[instructions.Key] + instructions.Value;
                    }
                    else
                    {
                        consolidated[instructions.Key] = instructions.Value;
                    }
                }
            }

            return consolidated;
        }

        //Stuff to support list initializer
        

        public void Add(CodeSegment child)
        {
            _children.Add(child);
        }

        IEnumerator<CodeSegment> IEnumerable<CodeSegment>.GetEnumerator()
        {
            return _children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _children).GetEnumerator();
        }
    }

    public class RepeatCodeSegment : CodeSegment
    {
        CodeSegment _inner;
        int _count;

        public RepeatCodeSegment(CodeSegment inner, int count, string tag = null) : base(tag)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _count = count;
        }

        protected override IEnumerable<CodeSegment> GetSelfAndChildren()
        {
            yield return this;
            yield return _inner;
        }

        protected internal override Dictionary<string, int> GetInstructionsInternal(Dictionary<string, CodeSegment> symbolTable)
        {
            return _inner.GetInstructionsInternal(symbolTable)
                .Select(kwp => new KeyValuePair<string, int>(kwp.Key, kwp.Value * _count))
                .ToDictionary(kwp => kwp.Key, kwp => kwp.Value);
        }
    }
}