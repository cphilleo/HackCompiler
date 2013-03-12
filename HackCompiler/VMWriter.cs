using System.IO;

namespace HackCompiler
{
    public class VMWriter
    {
        private readonly StringWriter _out;

        public VMWriter(StringWriter writer)
        {
            _out = writer;
        }

        public void Arithmetic(string op)
        {
            _out.WriteLine(op);
        }

        public void Call(string name, int arguments)
        {
            _out.WriteLine("call {0} {1}", name, arguments);
        }

        public void Function(string name, int parameters)
        {
            _out.WriteLine("function {0} {1}", name, parameters);
        }

        public void Label(string label)
        {
            _out.WriteLine("label " + label);
        }

        public void If(string label)
        {
            _out.WriteLine("if-goto " + label);
        }

        public void Goto(string label)
        {
            _out.WriteLine("goto " + label);
        }

        public void Push(string segment, int index)
        {
            _out.WriteLine("push {0} {1}", segment, index);
        }

        public void Pop(string segment, int index)
        {
            _out.WriteLine("pop {0} {1}", segment, index);
        }

        public void Return()
        {
            _out.WriteLine("return");
        }
    }
}