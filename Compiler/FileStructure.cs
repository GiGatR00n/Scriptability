using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class FileStructure
    {
        public byte[] Header
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0, value.ToList());
            }
        }

        public int PtrToIncludes
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0xC, BitConverter.GetBytes(value).ToList());
            }
        }

        public int PtrTousingAnimTrees
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x10, BitConverter.GetBytes(value).ToList());
            }
        }

        public int CodeSectionStart
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x14, BitConverter.GetBytes(value).ToList());
            }
        }

        public int RefStrings
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x18, BitConverter.GetBytes(value).ToList());
            }
        }

        public int GscFunctions
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x1C, BitConverter.GetBytes(value).ToList());
            }
        }

        public int ExternalFunctions
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x20, BitConverter.GetBytes(value).ToList());
            }
        }

        public int Size
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x24, BitConverter.GetBytes(value).ToList());
            }
        }

        public int Size2
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x28, BitConverter.GetBytes(value).ToList());
            }
        }

        public ushort Name
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x30, BitConverter.GetBytes(value).ToList());
            }
        }

        public ushort NumofRefStrings
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x32, BitConverter.GetBytes(value).ToList());
            }
        }

        public ushort NumOfFunctions
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x34, BitConverter.GetBytes(value).ToList());
            }
        }

        public ushort NumOfExternalFunctions
        {
            set
            {
                ScriptCompiler.CompiledPub.Replace(0x36, BitConverter.GetBytes(value).ToList());
            }
        }

        public byte NumofIncludes
        {
            set
            {
                ScriptCompiler.CompiledPub[0x3C] = value;
            }
        }

        public byte NumOfAnimTrees
        {
            set { ScriptCompiler.CompiledPub[0x3D] = value; }
        }
    }
}