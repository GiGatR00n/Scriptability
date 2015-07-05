using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using System.IO;
using System.Windows.Forms;

namespace Compiler
{
    public partial class ScriptCompiler
    {
        private readonly ParseTree Tree;
        public static List<byte> CompiledPub = new List<byte>();
        private readonly List<GlobalString> GlobalStrings = new List<GlobalString>();
        private readonly List<RefString> RefStrings = new List<RefString>();
        private readonly List<Function> Functions = new List<Function>();
        private readonly List<string> LocalVariables = new List<string>();
        private readonly List<Call> Calls = new List<Call>();
        private readonly List<int> JumpOnTrueExprList = new List<int>();
        private readonly List<Switch> Switches = new List<Switch>();
        private readonly List<Key> ArrayKeys = new List<Key>();
        private byte numofForeachStatements;
        private byte numofParams;
        private readonly string Filename;
        private readonly AnimTree _animTree = new AnimTree();
        private readonly List<int> LoopsStart = new List<int>();
        private readonly List<OP> ContinueHistory = new List<OP>();
        private readonly List<List<int>> BreakHistory = new List<List<int>>();

        public byte[] Compiled
        {
            get { return CompiledPub.ToArray(); }
        }

        public ScriptCompiler(ParseTree tree, string path)
        {
            CompiledPub.Clear();
            Tree = tree;
            Filename = path.Replace(".txt", ".gsc");
        }

        private void SetAlignedWord(byte offset = 0)
        {
            var alignedPos = (int)(CompiledPub.Count + 1 + offset & 0xFFFFFFFE);
            while (CompiledPub.Count < alignedPos)
            {
                CompiledPub.Add(0);
            }
        }

        private void SetAlignedDword(byte offset = 0)
        {
            var alignedPos = (int)(CompiledPub.Count + 3 + offset & 0xFFFFFFFC);
            while (CompiledPub.Count < alignedPos)
            {
                CompiledPub.Add(0);
            }
        }

        private void AddString(string str)
        {
            CompiledPub.AddRange(Encoding.ASCII.GetBytes(str + '\0'));
            GlobalStrings.Add(new GlobalString { Str = str, Position = (ushort)(CompiledPub.Count - str.Length - 1) });
        }

        private void AddRefToCall(string name, byte numofParams, byte flag, ushort include)
        {
            foreach (
                Call call in
                    Calls.Where(
                        call =>
                            call.NamePtr == GetStringPosByName(name) && call.IncludePtr == include && call.Flag == flag &&
                            call.NumOfParams == numofParams)
                )
            {
                call.Refs.Add(CompiledPub.Count - 1);
                call.NumOfRefs++;
                return;
            }
            var refs = new List<int> { CompiledPub.Count - 1 };
            Calls.Add(new Call
            {
                NamePtr = GetStringPosByName(name),
                IncludePtr = include,
                NumOfRefs = 1,
                NumOfParams = numofParams,
                Flag = flag,
                Refs = refs
            });
        }


        private void AddRefToString(string str, byte type)
        {
            foreach (
                RefString refString in
                    RefStrings.Where(refString => refString.NamePtr == GetStringPosByName(str) && refString.Type == type)
                )
            {
                refString.Refs.Add(CompiledPub.Count);
                refString.NumOfRefs++;
                return;
            }
            var refs = new List<int> { CompiledPub.Count };
            RefStrings.Add(new RefString
            {
                NamePtr = GetStringPosByName(str),
                NumOfRefs = 1,
                Refs = refs,
                Type = type
            });
        }


        private void AddUInt(uint i)
        {
            CompiledPub.AddRange(BitConverter.GetBytes(i));
        }

        private void AddInt(int i)
        {
            CompiledPub.AddRange(BitConverter.GetBytes(i));
        }

        private void AddFloat(float f)
        {
            CompiledPub.AddRange(BitConverter.GetBytes(f));
        }

        private void AddUshort(ushort u)
        {
            CompiledPub.AddRange(BitConverter.GetBytes(u));
        }

        private void AddShort(short s)
        {
            CompiledPub.AddRange(BitConverter.GetBytes(s));
        }

        private void WriteFunctionsStructToFile()
        {
            foreach (Function function in Functions)
            {
                AddUInt(function.Crc32);
                AddInt(function.Start);
                AddUshort(function.NamePtr);
                CompiledPub.Add(function.NumofParameters);
                CompiledPub.Add(0);
            }
        }

        private void WriteCallStackToFile()
        {
            foreach (Call call in Calls)
            {
                AddUshort(call.NamePtr);
                AddUshort(call.IncludePtr);
                AddUshort(call.NumOfRefs);
                CompiledPub.Add(call.NumOfParams);
                CompiledPub.Add(call.Flag);
                foreach (int _ref in call.Refs)
                {
                    AddInt(_ref);
                }
            }
        }

        private void WriteRefsToStringsToFile()
        {
            foreach (RefString refString in RefStrings)
            {
                AddUshort(refString.NamePtr);
                CompiledPub.Add(refString.NumOfRefs);
                CompiledPub.Add(refString.Type);
                foreach (int _ref in refString.Refs)
                {
                    AddInt(_ref);
                }
            }
        }

        public bool Init(string rel)
        {
            if (Tree.ParserMessages.Count > 0)
            {
                CompiledPub.Clear();
                AddString(String.Format("Bad syntax in line {0}.", Tree.ParserMessages[0].Location.Line));
                return false;
            }

            CompiledPub.AddRange(new byte[64]);
            AddString(rel);
            PrepareStrings(Tree.Root);
            int includesNodeIndex = Tree.Root.ChildNodes.FindIndex(e => e.Term.Name == "includes");
            int functionsNodeIndex = Tree.Root.ChildNodes.FindIndex(e => e.Term.Name == "functions");
            int animTreesNodeIndex = Tree.Root.ChildNodes.FindIndex(e => e.Term.Name == "usingAnimTree");
            var file = new FileStructure();
            file.PtrToIncludes = CompiledPub.Count;
            if (includesNodeIndex != -1)
            {
                file.NumofIncludes = (byte)Tree.Root.ChildNodes[includesNodeIndex].ChildNodes.Count;
                foreach (ParseTreeNode childNode in Tree.Root.ChildNodes[includesNodeIndex].ChildNodes)
                {
                    EmitInclude(childNode.FindTokenAndGetValue());
                }
            }
            if (animTreesNodeIndex != -1)
            {
                _animTree.NamePtr = GetStringPosByName(Tree.Root.ChildNodes[animTreesNodeIndex].FindTokenAndGetValue());
                _animTree.Unknown = 2;
                file.NumOfAnimTrees = 1;
            }
            file.CodeSectionStart = CompiledPub.Count;
            file.NumOfFunctions = (ushort)Tree.Root.ChildNodes[functionsNodeIndex].ChildNodes.Count;
            if (functionsNodeIndex != -1)
            {
                foreach (ParseTreeNode childNode in Tree.Root.ChildNodes[functionsNodeIndex].ChildNodes)
                {
                    EmitFunction(childNode);
                }
            }
            file.GscFunctions = CompiledPub.Count;
            file.NumOfFunctions = (ushort)Functions.Count;
            WriteFunctionsStructToFile();
            file.ExternalFunctions = CompiledPub.Count;
            file.NumOfExternalFunctions = (ushort)Calls.Count;
            WriteCallStackToFile();
            file.PtrTousingAnimTrees = CompiledPub.Count;
            WriteAnimTreesToFile();
            file.RefStrings = CompiledPub.Count;
            file.NumofRefStrings = (ushort)RefStrings.Count;
            WriteRefsToStringsToFile();
            file.Size = CompiledPub.Count;
            file.Size2 = CompiledPub.Count;
            file.Header = new byte[] { 0x80, 0x47, 0x53, 0x43, 0x0D, 0x0A, 0x00, 0x06 };
            file.Name = 0x40;
//             Directory.CreateDirectory(Path.GetDirectoryName(Filename) + @"\Compiled\");
//             using (var writer = File.Create(Path.GetDirectoryName(Filename) + @"\Compiled\" + Path.GetFileName(Filename)))
//             {
//                 writer.Write(CompiledPub.ToArray(), 0, CompiledPub.Count);
//             }
            return true;
        }

        private void WriteAnimTreesToFile()
        {
            AddUshort(_animTree.NamePtr);
            AddUshort(_animTree.Unknown);
            AddUshort(_animTree.NumOfRefs);
            if (_animTree.NumOfRefs == 0)
                return;
            foreach (int _ref in _animTree.Refs)
            {
                AddInt(_ref);
            }
        }

        private void EmitInclude(string include)
        {
            AddInt(GetStringPosByName(include));
        }

        private void EmitFunction(ParseTreeNode functionNode)
        {
            LocalVariables.Clear();
            numofForeachStatements = 0;
            numofParams = 0;
            foreach (ParseTreeNode parameterNode in functionNode.ChildNodes[1].ChildNodes[0].ChildNodes)
            {
                ParseParameter(parameterNode);
            }
            PrepareLocalVariables(functionNode.ChildNodes[2]);
            var function = new Function();
            function.Start = CompiledPub.Count;
            function.NamePtr = GetStringPosByName(functionNode.ChildNodes[0].Token.ValueString);
            function.NumofParameters = numofParams;
            if (LocalVariables.Count > 0)
            {
                EmitLocalVariables();
            }
            else
            {
                EmitOpcode(OP_checkclearparams);
            }
            ScriptCompile(functionNode.ChildNodes[2]);
            EmitOpcode(OP_End);
            Functions.Add(function);
            EmitCrc32();
            var alignedPos = (int)(CompiledPub.Count + 3 & 0xFFFFFFFE);
            while (CompiledPub.Count < alignedPos - 2)
            {
                CompiledPub.Add(0);
            }
        }

        private void ParseParameter(ParseTreeNode Node)
        {
            if (Node.Term.Name == "identifier")
            {
                LocalVariables.Add(Node.FindTokenAndGetValue().ToLower());
                numofParams++;
            }
            else
            {
                foreach (ParseTreeNode child in Node.ChildNodes)
                {
                    ParseParameter(child);
                }
            }
        }

        private void EmitCrc32()
        {
            var crc32 = new Crc32();
            int start = Functions[Functions.Count - 1].Start;
            crc32.AddData(start, CompiledPub.Count - start);
            Functions[Functions.Count - 1].Crc32 = crc32.GetCrc32();
        }

        private void EmitGetAnimation(ParseTreeNode node)
        {
            EmitOpcode(OP_GetAnimation);
            SetAlignedDword();
            AddInt(GetStringPosByName(node.ChildNodes[1].FindTokenAndGetValue()));
        }

        private void ScriptCompile(ParseTreeNode Node, bool _ref = false, bool waitTillVar = false)
        {
            switch (Node.Term.Name)
            {
                case "jumpStatement":
                    EmitOpcode(OP_jump);
                    SetAlignedWord();
                    if (Node.ChildNodes[0].Term.Name == "continue")
                    {
                        ContinueHistory.Add(new OP
                        {
                            Start = LoopsStart[LoopsStart.Count - 1],
                            JumpRangePos = CompiledPub.Count
                        });
                        AddShort((short)(LoopsStart[LoopsStart.Count - 1] - (CompiledPub.Count + 2)));
                    }
                    else
                    {
                        BreakHistory[BreakHistory.Count - 1].Add(CompiledPub.Count);
                        AddShort(0);
                    }
                    break;

                case "waitTillFrameEnd":
                    if (Node.ChildNodes[0].Token.ValueString == "waittillframeend")
                        EmitOpcode(OP_waittillFrameEnd);
                    break;

                case "getAnimation":
                    EmitGetAnimation(Node);
                    break;

                case "animTree":
                    _animTree.NumOfRefs++;
                    EmitOpcode(OP_GetInteger);
                    SetAlignedDword();
                    _animTree.Refs.Add(CompiledPub.Count);
                    AddInt(-1);
                    break;

                case "expression":
                    EmitExpression(Node);
                    break;

                case "block":
                    if (Node.ChildNodes.Count > 0)
                    {
                        ScriptCompile(Node.ChildNodes[0]);
                    }
                    break;

                case "blockContent":
                    foreach (ParseTreeNode childNode in Node.ChildNodes[0].ChildNodes)
                    {
                        ScriptCompile(childNode.ChildNodes[0]);
                    }
                    break;

                case "simpleCall":
                    EmitCall(Node.ChildNodes[0].ChildNodes[0], true);
                    break;

                case "call":
                    EmitCall(Node.ChildNodes[0], false);
                    break;

                case "conditionalStatement":
                    EmitconditionalStatement(Node);
                    break;

                case "wait":
                    ScriptCompile(Node.ChildNodes[1]);
                    EmitOpcode(OP_wait);
                    break;

                case "return":
                    if (Node.ChildNodes.Count > 1)
                    {
                        ScriptCompile(Node.ChildNodes[1]);
                        EmitOpcode(OP_Return);
                    }
                    else
                    {
                        EmitOpcode(OP_End);
                    }
                    break;

                case "stringLiteral":
                    EmitGetString(Node.Token.ValueString, false);
                    break;

                case "boolNot":
                    ScriptCompile(Node.ChildNodes[1]);
                    EmitOpcode(OP_BoolNot);
                    break;

                case "size":
                    ScriptCompile(Node.ChildNodes[0]);
                    EmitOpcode(OP_size);
                    break;

                case "isString":
                    EmitGetString(Node.ChildNodes[1].Token.ValueString, true);
                    break;

                case "hashedString":
                    EmitGetHash(Node.ChildNodes[1]);
                    break;

                case "identifier":
                    if (IsObjectOwnerOrBuiltIn(Node.Token.ValueString))
                    {
                        EmitOwner(Node, _ref);
                        break;
                    }
                    EvalLocalVariable(Node.Token.ValueString, _ref, waitTillVar);
                    break;

                case "setVariableField":
                    EmitSetVariableField(Node);
                    break;

                case "directAccess":
                    EmitEvalFieldVariable(Node, _ref);
                    break;

                case "numberLiteral":
                    if (Node.Token.Value is int)
                    {
                        EmitGetInt(int.Parse(Node.Token.ValueString));
                    }
                    else if (Node.Token.Value is double)
                    {
                        EmitGetFloat(float.Parse(Node.Token.ValueString));
                    }
                    break;

                case "array":
                    EmitEvalArray(Node, _ref);
                    break;

                case "ifStatement":
                    EmitIfStatement(Node);
                    break;

                case "whileStatement":
                    EmitWhileStatement(Node);
                    break;

                case "developerScript":
                    EmitSkipDev(Node);
                    break;

                case "forStatement":
                    EmitForStatement(Node);
                    break;

                case "switchStatement":
                    EmitSwitchStatement(Node);
                    break;

                case "foreachStatement":
                    EmitForEachStatement(Node);
                    break;

                case "expr":
                case "expr+":
                    Node.ChildNodes.Reverse();
                    foreach (ParseTreeNode childNode in Node.ChildNodes)
                    {
                        ScriptCompile(childNode, _ref);
                    }
                    break;

                case "statement":
                case "statementBlock":
                case "declaration":
                case "parenExpr":
                    ScriptCompile(Node.ChildNodes[0], _ref);
                    break;

                case "booleanExpression":
                    EmitBooleanExpr(Node);
                    break;

                case "relationalExpression":
                    EmitRelationalExpression(Node);
                    break;

                case "getFunction":
                    EmitGetFunction(Node);
                    break;

                case "vector":
                    EmitVector(Node);
                    break;

                case "boolNotExpr":
                    ScriptCompile(Node.ChildNodes[0]);
                    break;
            }
        }

        private void EmitGetHash(ParseTreeNode node)
        {
            EmitOpcode(OP_GetHash);
            SetAlignedDword();
            AddInt(GetHash(node.Token.ValueString));
        }

        private void EmitExpression(ParseTreeNode node)
        {
            ScriptCompile(node.ChildNodes[0]);
            ScriptCompile(node.ChildNodes[2]);
            switch (node.ChildNodes[1].ChildNodes[0].Term.Name)
            {
                case "+":
                    EmitOpcode(OP_plus);
                    break;

                case "-":
                    EmitOpcode(OP_minus);
                    break;

                case "*":
                    EmitOpcode(OP_multiply);
                    break;

                case "/":
                    EmitOpcode(OP_divide);
                    break;

                case "%":
                    EmitOpcode(OP_mod);
                    break;

                case "&":
                    EmitOpcode(OP_bit_and);
                    break;

                case "|":
                    EmitOpcode(OP_bit_or);
                    break;
            }
        }

        private int GetHash(string str)
        {
            str = str.ToLower();
            return str.ToArray().Aggregate(5381, (current, dvarCh) => 33 * current + Convert.ToByte(dvarCh));
        }

        private void EmitconditionalStatement(ParseTreeNode node)
        {
            ScriptCompile(node.ChildNodes[0]);
            EmitOpcode(OP_JumpOnFalse);
            SetAlignedWord();
            int jmpRangePos = CompiledPub.Count;
            CompiledPub.AddRange(new byte[2]);
            ScriptCompile(node.ChildNodes[2]);
            EmitOpcode(OP_jump);
            SetAlignedWord();
            int opJumpJmpRangePos = CompiledPub.Count;
            CompiledPub.AddRange(new byte[2]);
            CompiledPub.Replace(jmpRangePos,
                BitConverter.GetBytes((short)(CompiledPub.Count - jmpRangePos - 2)).ToList());
            ScriptCompile(node.ChildNodes[4]);
            CompiledPub.Replace(opJumpJmpRangePos,
                BitConverter.GetBytes((short)(CompiledPub.Count - opJumpJmpRangePos - 2)).ToList());
        }

        private void EmitForEachStatement(ParseTreeNode node)
        {
            BreakHistory.Add(new List<int>());
            var arrayKey = new Key { first = ArrayKeys[0].first, second = ArrayKeys[0].second };
            ArrayKeys.RemoveAt(0);
            ScriptCompile(node.ChildNodes[3]);
            EvalLocalVariable(arrayKey.first, true);
            EmitOpcode(OP_SetVariableField);
            EvalLocalVariable(arrayKey.first, false);
            EmitOpcode(OP_GetFirstArrayKey);
            EvalLocalVariable(arrayKey.second, true);
            EmitOpcode(OP_SetVariableField);
            int negJmpPos = CompiledPub.Count;
            EvalLocalVariable(arrayKey.second, false);
            EmitOpcode(OP_isdefined);
            EmitOpcode(OP_JumpOnFalse);
            SetAlignedWord();
            int jmpRangePos = CompiledPub.Count;
            CompiledPub.AddRange(new byte[2]);
            EvalLocalVariable(arrayKey.second, false);
            EvalLocalVariable(arrayKey.first, false);
            EmitOpcode(OP_EvalArray);
            ScriptCompile(node.ChildNodes[1], true);
            EmitOpcode(OP_SetVariableField);
            LoopsStart.Add(CompiledPub.Count);
            int Start = CompiledPub.Count;
            ScriptCompile(node.ChildNodes[4]);
            foreach (OP op in ContinueHistory)
            {
                if (op.Start == Start)
                {
                    CompiledPub.Replace(op.JumpRangePos,
                        BitConverter.GetBytes((short)(CompiledPub.Count - op.JumpRangePos - 2)).ToList());
                    break;
                }
            }
            EvalLocalVariable(arrayKey.second, false);
            EvalLocalVariable(arrayKey.first, false);
            EmitOpcode(OP_GetNextArrayKey);
            EvalLocalVariable(arrayKey.second, true);
            EmitOpcode(OP_SetVariableField);
            EmitOpcode(OP_jump);
            SetAlignedWord();
            AddShort((short)(negJmpPos - (CompiledPub.Count + 2)));
            CompiledPub.Replace(jmpRangePos,
                BitConverter.GetBytes((short)(CompiledPub.Count - jmpRangePos - 2)).ToList());
            EmitBreak();
        }


        private void EmitSwitchStatement(ParseTreeNode node)
        {
            BreakHistory.Add(new List<int>());
            Switches.Clear();
            LoopsStart.Add(CompiledPub.Count);
            ScriptCompile(node.ChildNodes[1]);
            EmitOpcode(OP_switch);
            SetAlignedDword();
            int switchRangePos = CompiledPub.Count;
            AddInt(0);
            foreach (ParseTreeNode caseNode in node.ChildNodes[2].ChildNodes)
            {
                object name = caseNode.ChildNodes[0].ChildNodes.Count > 2
                    ? caseNode.ChildNodes[0].ChildNodes[1].ChildNodes[0].Token.Value
                    : "defaultSwitchStatement";
                Switches.Add(new Switch { Name = name, Pos = CompiledPub.Count });
                if (caseNode.ChildNodes.Count > 0)
                {
                    if (caseNode.ChildNodes.FindIndex(e => e.Term.Name == "blockContent") != -1)
                        ScriptCompile(caseNode.ChildNodes[1]);
                }
            }
            EmitOpcode(OP_endswitch);
            SetAlignedDword();
            CompiledPub.Replace(switchRangePos,
                BitConverter.GetBytes(CompiledPub.Count - switchRangePos - 4).ToList());
            AddInt(Switches.Count);
            foreach (Switch Switch in Switches)
            {
                if (Switch.Name is int)
                {
                    AddUshort(Convert.ToUInt16(Switch.Name));
                    CompiledPub.AddRange(new byte[] { 0x80, 0 });
                }
                else
                {
                    if (Switch.Name as string == "defaultSwitchStatement")
                    {
                        AddInt(0);
                    }
                    else
                    {
                        AddUshort(GetStringPosByName((string)Switch.Name));
                        AddRefToString((string)Switch.Name, 0);
                        AddUshort(0);
                    }
                }
                AddInt(-(CompiledPub.Count + 4 - Switch.Pos));
            }
            LoopsStart.RemoveAt(LoopsStart.Count - 1);
            EmitBreak(false);
        }

        private void EmitForStatement(ParseTreeNode node)
        {
            BreakHistory.Add(new List<int>());
            int negJmpPos = 0;
            int jmpRangePos = 0;
            ParseTreeNode forBodyNode = node.ChildNodes[1];
            int setVariableNodeIndex = forBodyNode.ChildNodes.FindIndex(e => e.Term.Name == "setVariableField");
            int booleanExprNodeIndex = forBodyNode.ChildNodes.FindIndex(e => e.Term.Name == "booleanExpression");
            int forIterateNodeIndex = forBodyNode.ChildNodes.FindIndex(e => e.Term.Name == "forIterate");
            int statementBlockNodeIndex = node.ChildNodes.FindIndex(e => e.Term.Name == "statementBlock");

            if (setVariableNodeIndex != -1)
                ScriptCompile(forBodyNode.ChildNodes[setVariableNodeIndex]);

            negJmpPos = CompiledPub.Count;
            LoopsStart.Add(CompiledPub.Count);
            if (booleanExprNodeIndex != -1)
            {
                EmitBooleanExpr(forBodyNode.ChildNodes[booleanExprNodeIndex]);
                EmitOpcode(OP_JumpOnFalse);
                SetAlignedWord();
                jmpRangePos = CompiledPub.Count;
                AddShort(0);
            }

            if (statementBlockNodeIndex != -1)
                ScriptCompile(node.ChildNodes[statementBlockNodeIndex]);

            if (forIterateNodeIndex != -1)
                EmitSetVariableField(forBodyNode.ChildNodes[forIterateNodeIndex]);

            EmitOpcode(OP_jump);
            SetAlignedWord();
            AddShort((short)(negJmpPos - (CompiledPub.Count + 2)));
            if (jmpRangePos != 0)
                CompiledPub.Replace(jmpRangePos,
                    BitConverter.GetBytes((short)(CompiledPub.Count - jmpRangePos - 2)).ToList());
            EmitBreak();

        }

        private void EmitBreak(bool deleteStartPos = true)
        {
            if (deleteStartPos)
                LoopsStart.RemoveAt(LoopsStart.Count - 1);
            foreach (var _break in BreakHistory[BreakHistory.Count - 1])
            {
                CompiledPub.Replace(_break,
                    BitConverter.GetBytes((short)(CompiledPub.Count - _break - 2)).ToList());
            }
            BreakHistory.RemoveAt(BreakHistory.Count - 1);
        }

        private void EmitSkipDev(ParseTreeNode Node)
        {
            EmitOpcode(OP_skipdev);
            SetAlignedWord();
            int JmpPos = CompiledPub.Count;
            AddShort(0);
            ScriptCompile(Node.ChildNodes[1]);
            CompiledPub.Replace(JmpPos,
                BitConverter.GetBytes((short)(CompiledPub.Count - JmpPos - 2)).ToList());
        }

        private void EmitRelationalExpression(ParseTreeNode node)
        {
            ScriptCompile(node.ChildNodes[0]);
            ScriptCompile(node.ChildNodes[2]);
            switch (node.ChildNodes[1].ChildNodes[0].Term.Name)
            {
                case ">":
                    EmitOpcode(OP_greater);
                    break;

                case ">=":
                    EmitOpcode(OP_greater_equal);
                    break;

                case "<":
                    EmitOpcode(OP_less);
                    break;

                case "<=":
                    EmitOpcode(OP_less_equal);
                    break;

                case "==":
                    EmitOpcode(OP_equality);
                    break;

                case "!=":
                    EmitOpcode(OP_inequality);
                    break;
            }
        }

        private void EmitGetFunction(ParseTreeNode node)
        {
            ushort ptrToInclude = 0x3e;
            int nodeIndex = 0;
            if (node.ChildNodes[0].Term.Name == "gscForFunction")
            {
                ptrToInclude = GetStringPosByName(node.FindTokenAndGetValue());
                nodeIndex = 1;
            }
            EmitOpcode(OP_GetFunction);
            AddRefToCall(node.ChildNodes[nodeIndex].FindTokenAndGetValue(), 0, 1, ptrToInclude);
            SetAlignedDword();
            AddInt(GetStringPosByName(node.ChildNodes[nodeIndex].FindTokenAndGetValue()));
        }

        private void EmitVector(ParseTreeNode node)
        {
            byte flag = 0;
            string first, second, third;
            if (isSimple(node.ChildNodes[2]) && isSimple(node.ChildNodes[1]) && isSimple(node.ChildNodes[0]))
            {
                first = node.ChildNodes[0].FindTokenAndGetValue();
                second = node.ChildNodes[1].FindTokenAndGetValue();
                third = node.ChildNodes[2].FindTokenAndGetValue();
                if (first == "1")
                    flag |= 0x20;
                else if (first == "-1")
                    flag |= 0x10;

                if (second == "1")
                    flag |= 0x08;
                else if (second == "-1")
                    flag |= 0x04;

                if (third == "1")
                    flag |= 0x02;
                else if (third == "-1")
                    flag |= 0x01;
                EmitOpcode(OP_GetSimpleVector);
                CompiledPub.Add(flag);
                return;
            }
            ScriptCompile(node.ChildNodes[2]);
            ScriptCompile(node.ChildNodes[1]);
            ScriptCompile(node.ChildNodes[0]);
            EmitOpcode(OP_vector);
        }

        private bool isSimple(ParseTreeNode input)
        {
            if (input.ChildNodes[0].Token == null)
                return false;
            switch (input.ChildNodes[0].Token.ValueString)
            {
                case "0":
                case "1":
                case "-1":
                    return true;

                default:
                    return false;
            }
        }

        private void EmitBooleanExpr(ParseTreeNode node)
        {
            ScriptCompile(node.ChildNodes[0]);
            if (node.ChildNodes.Count == 1) return;
            if (node.ChildNodes[1].Term.Name == "&&")
            {
                EmitOpcode(OP_JumpOnFalseExpr);
                SetAlignedWord();
                int jmpRangePos = CompiledPub.Count;
                CompiledPub.AddRange(new byte[2]);
                int statementStart = CompiledPub.Count;
                ScriptCompile(node.ChildNodes[2]);
                CompiledPub.Replace(jmpRangePos,
                    BitConverter.GetBytes((short)(CompiledPub.Count - statementStart)).ToList());
            }
            else if (node.ChildNodes[1].Term.Name == "||")
            {
                EmitOpcode(OP_JumpOnTrueExpr);
                SetAlignedWord();
                JumpOnTrueExprList.Add(CompiledPub.Count);
                CompiledPub.AddRange(new byte[2]);
                ScriptCompile(node.ChildNodes[2]);
            }
            foreach (int i in JumpOnTrueExprList)
            {
                CompiledPub.Replace(i, BitConverter.GetBytes((short)(CompiledPub.Count - i - 2)).ToList());
            }
            JumpOnTrueExprList.Clear();
        }

        private void EmitWhileStatement(ParseTreeNode node)
        {
            BreakHistory.Add(new List<int>());
            LoopsStart.Add(CompiledPub.Count);
            int negJmpPos = CompiledPub.Count;
            EmitBooleanExpr(node.ChildNodes[1]);
            EmitOpcode(OP_JumpOnFalse);
            SetAlignedWord();
            int jmpRangePos = CompiledPub.Count;
            CompiledPub.AddRange(new byte[2]);
            int statementStart = CompiledPub.Count;
            ScriptCompile(node.ChildNodes[2]);
            EmitOpcode(OP_jump);
            SetAlignedWord();
            AddShort((short)(negJmpPos - (CompiledPub.Count + 2)));
            CompiledPub.Replace(jmpRangePos,
                BitConverter.GetBytes((short)(CompiledPub.Count - statementStart)).ToList());
            EmitBreak();
        }

        private void EmitIfStatement(ParseTreeNode node)
        {
            EmitBooleanExpr(node.ChildNodes[1]);
            EmitOpcode(OP_JumpOnFalse);
            SetAlignedWord();
            int jmpRangePos = CompiledPub.Count;
            CompiledPub.AddRange(new byte[2]);
            int statementStart = CompiledPub.Count;
            ScriptCompile(node.ChildNodes[2]);
            if (node.ChildNodes.Count == 4)
            {
                EmitOpcode(OP_jump);
                SetAlignedWord();
                int opJumpJmpRangePos = CompiledPub.Count;
                CompiledPub.AddRange(new byte[2]);
                CompiledPub.Replace(jmpRangePos,
                    BitConverter.GetBytes((short)(CompiledPub.Count - statementStart)).ToList());
                ScriptCompile(node.ChildNodes[3].ChildNodes[1]);
                CompiledPub.Replace(opJumpJmpRangePos,
                    BitConverter.GetBytes((short)(CompiledPub.Count - opJumpJmpRangePos - 2)).ToList());
            }
            else
            {
                CompiledPub.Replace(jmpRangePos,
                    BitConverter.GetBytes((short)(CompiledPub.Count - statementStart)).ToList());
            }
        }

        private void EmitEvalArray(ParseTreeNode node, bool _ref)
        {
            if (node.ChildNodes[0].Term.Name == "[]")
            {
                EmitOpcode(OP_EmptyArray);
                return;
            }
            ScriptCompile(node.ChildNodes[1]);
            ScriptCompile(node.ChildNodes[0], _ref);
            EmitOpcode(!_ref ? OP_EvalArray : OP_EvalArrayRef);
        }

        private void EmitGetInt(int i)
        {
            if (i == 0)
            {
                EmitOpcode(OP_GetZero);
                return;
            }

            bool negative = false;

            if (i < 0)
            {
                i = i * -1;
                negative = true;
            }

            if (i <= byte.MaxValue)
            {
                EmitOpcode(negative ? OP_GetNegByte : OP_GetByte);
                CompiledPub.Add((byte)i);
                return;
            }

            if (i <= ushort.MaxValue)
            {
                EmitOpcode(negative ? OP_GetNegUnsignedShort : OP_GetUnsignedShort);
                SetAlignedWord();
                AddUshort((ushort)i);
                return;
            }

            EmitOpcode(OP_GetInteger);
            SetAlignedDword();
            AddInt(i);
        }

        private void EmitGetFloat(float f)
        {
            EmitOpcode(OP_GetFloat);
            SetAlignedDword();
            AddFloat(f);
        }


        private void EmitEvalFieldVariable(ParseTreeNode node, bool _ref)
        {
            EmitObject(node.ChildNodes[0].ChildNodes[0]);
            EmitOpcode(_ref ? OP_EvalFieldVariableRef : OP_EvalFieldVariable);
            SetAlignedWord();
            if (node.ChildNodes[1].Term.Name == "identifier")
            {
                AddRefToString(node.ChildNodes[1].Token.ValueString, 1);
                AddUshort(GetStringPosByName(node.ChildNodes[1].Token.ValueString));
            }
            else
            {
                ScriptCompile(node.ChildNodes[1]);
            }
        }

        private void EmitSetVariableField(ParseTreeNode node)
        {
            if (node.ChildNodes[1].ChildNodes[0].Term.Name != "=" && node.ChildNodes.Count > 2)
            {
                ScriptCompile(node.ChildNodes[0].ChildNodes[0]);
                ScriptCompile(node.ChildNodes[2].ChildNodes[0]);
            }

            switch (node.ChildNodes[1].ChildNodes[0].Term.Name)
            {
                case "++":
                    ScriptCompile(node.ChildNodes[0], true);
                    EmitOpcode(OP_inc);
                    return;

                case "--":
                    ScriptCompile(node.ChildNodes[0], true);
                    EmitOpcode(OP_dec);
                    return;

                case "+=":
                    EmitOpcode(OP_plus);
                    break;

                case "-=":
                    EmitOpcode(OP_minus);
                    break;

                case "*=":
                    EmitOpcode(OP_multiply);
                    break;

                case "/=":
                    EmitOpcode(OP_divide);
                    break;

                case "%=":
                    EmitOpcode(OP_mod);
                    break;

                case "&=":
                    EmitOpcode(OP_bit_and);
                    break;

                case "|=":
                    EmitOpcode(OP_bit_or);
                    break;

                case "=":
                    ScriptCompile(node.ChildNodes[2].ChildNodes[0]);
                    break;
            }
            ScriptCompile(node.ChildNodes[0].ChildNodes[0], true);
            EmitOpcode(OP_SetVariableField);
        }

        private void EvalLocalVariable(string variable, bool _ref, bool waitTillVar = false)
        {
            if (!waitTillVar)
            {
                CompiledPub.Add(_ref ? OP_EvalLocalVariableRefCached : OP_EvalLocalVariableCached);
            }
            else
            {
                EmitOpcode(OP_SafeSetWaittillVariableFieldCached);
            }
            for (byte i = 0; i < LocalVariables.Count; i++)
            {
                if (LocalVariables[i] == variable)
                {
                    CompiledPub.Add(i);
                    break;
                }
            }
        }

        private void EmitGetString(string str, bool isString)
        {
            EmitOpcode(!isString ? OP_GetString : OP_GetIString);
            SetAlignedWord();
            AddRefToString(str, 0);
            AddUshort(GetStringPosByName(str));
        }

        private void EmitOwner(ParseTreeNode node, bool _ref = false)
        {
            if (node.Token == null)
            {
                ScriptCompile(node);
                return;
            }

            switch (node.Token.ValueString)
            {
                case "undefined":
                    EmitOpcode(OP_GetUndefined);
                    break;

                case "true":
                    EmitGetInt(1);
                    break;

                case "false":
                    EmitOpcode(OP_GetZero);
                    break;

                case "self":
                    EmitOpcode(OP_GetSelf);
                    break;

                case "level":
                    EmitOpcode(OP_GetLevel);
                    break;

                case "game":
                    EmitOpcode(_ref ? OP_GetGameRef : OP_GetGame);
                    break;

                case "anim":
                    EmitOpcode(OP_GetAnim);
                    break;

                default:
                    ScriptCompile(node);
                    break;
            }
        }

        private void EmitObject(ParseTreeNode node)
        {
            if (node.Token == null)
            {
                ScriptCompile(node);
                EmitOpcode(OP_CastFieldObject);
                return;
            }

            switch (node.Token.ValueString)
            {
                case "self":
                    EmitOpcode(OP_GetSelfObject);
                    break;

                case "level":
                    EmitOpcode(OP_GetLevelObject);
                    break;

                case "anim":
                    EmitOpcode(OP_GetAnimObject);
                    break;

                default:
                    ScriptCompile(node);
                    EmitOpcode(OP_CastFieldObject);
                    break;
            }
        }

        private void EmitCall(ParseTreeNode callNode, bool decTop)
        {
            switch (callNode.Term.Name)
            {
                case "scriptFunctionCallPointer":
                case "scriptMethodCallPointer":
                case "scriptThreadCallPointer":
                case "scriptMethodThreadCallPointer":
                    EmitCallPointer(callNode, decTop);
                    return;
            }
            int baseCallNodeIndex = callNode.ChildNodes.FindIndex(e => e.Term.Name == "baseCall");
            int parenParamsNodeIndex =
                callNode.ChildNodes[baseCallNodeIndex].ChildNodes.FindIndex(e => e.Term.Name == "parenParameters");
            int functionNameNodeIndex =
                callNode.ChildNodes[baseCallNodeIndex].ChildNodes.FindIndex(e => e.Term.Name == "identifier");
            if (IsObjectOwnerOrBuiltIn(callNode.ChildNodes[baseCallNodeIndex].ChildNodes[0].FindTokenAndGetValue()))
            {
                EmitBuiltIn(callNode);
                return;
            }
            EmitOpcode(OP_PreScriptCall);
            ParseTreeNode parametersNode = null;
            if (callNode.ChildNodes[baseCallNodeIndex].ChildNodes[parenParamsNodeIndex].ChildNodes.Count > 0)
            {
                parametersNode =
                    callNode.ChildNodes[baseCallNodeIndex].ChildNodes[parenParamsNodeIndex].ChildNodes[0];
                parametersNode.ChildNodes.Reverse();
                foreach (ParseTreeNode childNode in parametersNode.ChildNodes)
                {
                    ScriptCompile(childNode);
                }
            }
            byte flag = 0;
            switch (callNode.Term.Name)
            {
                case "scriptFunctionCall":
                    EmitOpcode(OP_ScriptFunctionCall);
                    flag = 2;
                    break;

                case "scriptMethodCall":
                    EmitOwner(callNode.ChildNodes[0]);
                    EmitOpcode(OP_ScriptMethodCall);
                    flag = 4;
                    break;

                case "scriptThreadCall":
                    EmitOpcode(OP_ScriptThreadCall);
                    flag = 3;
                    break;

                case "scriptMethodThreadCall":
                    EmitOwner(callNode.ChildNodes[0]);
                    EmitOpcode(OP_ScriptMethodThreadCall);
                    flag = 5;
                    break;
            }
            string name = callNode.ChildNodes[baseCallNodeIndex].ChildNodes[functionNameNodeIndex].Token.ValueString;
            byte numofParams = parametersNode != null ? (byte)GetNumOfParams(parametersNode) : (byte)0;
            ushort ptrToInclude = 0x3E;
            if (callNode.ChildNodes[baseCallNodeIndex].ChildNodes[0].Term.Name == "gscForFunction")
            {
                ptrToInclude =
                    GetStringPosByName(callNode.ChildNodes[baseCallNodeIndex].ChildNodes[0].FindTokenAndGetValue());
            }
            AddRefToCall(name, numofParams, flag, ptrToInclude);
            SetAlignedDword(1);
            AddInt(GetStringPosByName(name));
            if (decTop)
            {
                EmitOpcode(OP_DecTop);
            }
        }

        private int GetNumOfParams(ParseTreeNode node)
        {
            int count = 0;
            foreach (ParseTreeNode parameterNode in node.ChildNodes)
            {
                if (parameterNode.Term.Name != "expr" && parameterNode.Term.Name != "expr+")
                    count++;
                else
                    count += GetNumOfParams(parameterNode);
            }
            return count;
        }

        private void EmitBuiltIn(ParseTreeNode function)
        {
            if (function.Term.Name == "scriptMethodCall")
            {
                ParseTreeNodeList parametersNode = function.ChildNodes[1].ChildNodes[1].ChildNodes[0].ChildNodes;
                parametersNode.Reverse();
                switch (function.ChildNodes[1].ChildNodes[0].Token.ValueString)
                {
                    case "waittillmatch":
                    case "waittill":
                        parametersNode.Reverse();
                        ScriptCompile(parametersNode[0].ChildNodes[0]);
                        EmitOwner(function.ChildNodes[0]);
                        EmitOpcode(OP_waittill);
                        if (parametersNode.Count > 1)
                        {
                            foreach (ParseTreeNode parameter in parametersNode)
                            {
                                ParseWaittillVars(parameter, true);
                            }
                        }
                        EmitOpcode(OP_clearparams);
                        break;

                    case "notify":
                        EmitOpcode(OP_voidCodepos);
                        foreach (ParseTreeNode parameter in parametersNode)
                        {
                            ScriptCompile(parameter);
                        }
                        EmitOwner(function.ChildNodes[0]);
                        EmitOpcode(OP_notify);
                        break;

                    case "endon":
                        foreach (ParseTreeNode parameter in parametersNode)
                        {
                            ScriptCompile(parameter.ChildNodes[0]);
                        }
                        EmitOwner(function.ChildNodes[0]);
                        EmitOpcode(OP_endon);
                        break;
                }
            }
            else if (function.Term.Name == "scriptFunctionCall")
            {
                ParseTreeNodeList parametersNode = function.ChildNodes[0].ChildNodes[1].ChildNodes[0].ChildNodes;
                parametersNode.Reverse();
                foreach (ParseTreeNode parameter in parametersNode)
                {
                    ScriptCompile(parameter.ChildNodes[0]);
                }
                switch (function.ChildNodes[0].ChildNodes[0].Token.ValueString)
                {
                    case "isdefined":
                        EmitOpcode(OP_isdefined);
                        break;

                    case "vectorscale":
                        EmitOpcode(OP_vectorscale);
                        break;

                    case "anglestoup":
                        EmitOpcode(OP_anglestoup);
                        break;

                    case "anglestoright":
                        EmitOpcode(OP_anglestoright);
                        break;

                    case "anglestoforward":
                        EmitOpcode(OP_anglestoforward);
                        break;

                    case "angleclamp180":
                        EmitOpcode(OP_angleclamp180);
                        break;

                    case "vectortoangles":
                        EmitOpcode(OP_vectortoangles);
                        break;

                    case "abs":
                        EmitOpcode(OP_abs);
                        break;

                    case "gettime":
                        EmitOpcode(OP_gettime);
                        break;

                    case "getdvar":
                        EmitOpcode(OP_getdvar);
                        break;

                    case "getdvarint":
                        EmitOpcode(OP_getdvarint);
                        break;

                    case "getdvarfloat":
                        EmitOpcode(OP_getdvarfloat);
                        break;

                    case "getdvarvector":
                        EmitOpcode(OP_getdvarvector);
                        break;

                    case "getdvarcolorred":
                        EmitOpcode(OP_getdvarcolorred);
                        break;

                    case "getdvarcolorgreen":
                        EmitOpcode(OP_getdvarcolorgreen);
                        break;

                    case "getdvarcolorblue":
                        EmitOpcode(OP_getdvarcolorblue);
                        break;

                    case "getdvarcoloralpha":
                        EmitOpcode(OP_getdvarcoloralpha);
                        break;

                    case "getfirstarraykey":
                        EmitOpcode(OP_GetFirstArrayKey);
                        break;

                    case "getnextarraykey":
                        EmitOpcode(OP_GetNextArrayKey);
                        break;
                }
            }
        }

        private void EmitCallPointer(ParseTreeNode callNode, bool decTop)
        {
            EmitOpcode(OP_PreScriptCall);
            int baseCallPointerNodeIndex = callNode.ChildNodes.FindIndex(e => e.Term.Name == "baseCallPointer");
            ParseTreeNode parametersNode = callNode.ChildNodes[baseCallPointerNodeIndex].ChildNodes[1].ChildNodes[0];
            byte numofParams = parametersNode != null ? (byte)GetNumOfParams(parametersNode) : (byte)0;
            if (parametersNode != null)
                parametersNode.ChildNodes.Reverse();
            foreach (
                ParseTreeNode childNode in
                    parametersNode.ChildNodes)
            {
                ScriptCompile(childNode);
            }

            switch (callNode.Term.Name)
            {
                case "scriptFunctionCallPointer":
                    ScriptCompile(callNode.ChildNodes[baseCallPointerNodeIndex].ChildNodes[0]);
                    EmitOpcode(OP_ScriptFunctionCallPointer);
                    CompiledPub.Add(numofParams);
                    break;

                case "scriptMethodCallPointer":
                    EmitOwner(callNode.ChildNodes[0]);
                    ScriptCompile(callNode.ChildNodes[baseCallPointerNodeIndex].ChildNodes[0]);
                    EmitOpcode(OP_ScriptMethodCallPointer);
                    CompiledPub.Add(numofParams);
                    break;

                case "scriptThreadCallPointer":
                    ScriptCompile(callNode.ChildNodes[baseCallPointerNodeIndex].ChildNodes[0]);
                    EmitOpcode(OP_ScriptThreadCallPointer);
                    CompiledPub.Add(numofParams);
                    break;

                case "scriptMethodThreadCallPointer":
                    EmitOwner(callNode.ChildNodes[0]);
                    ScriptCompile(callNode.ChildNodes[baseCallPointerNodeIndex].ChildNodes[0]);
                    EmitOpcode(OP_ScriptMethodThreadCallPointer);
                    CompiledPub.Add(numofParams);
                    break;
            }
            if (decTop)
            {
                EmitOpcode(OP_DecTop);
            }
        }

        private void EmitOpcode(byte opcode)
        {
            CompiledPub.Add(opcode);
        }

        private void EmitLocalVariables()
        {
            EmitOpcode(OP_CreateLocalVariables);
            CompiledPub.Add((byte)LocalVariables.Count);
            foreach (string variable in LocalVariables)
            {
                SetAlignedWord();
                AddRefToString(variable, 1);
                AddUshort(GetStringPosByName(variable));
            }
            LocalVariables.Reverse();
        }

        private ushort GetStringPosByName(string str)
        {
            return
                (from globalString in GlobalStrings where globalString.Str == str select globalString.Position)
                    .FirstOrDefault();
        }

        private bool StringShouldBeWritten(string str)
        {
            return GlobalStrings.All(globalString => globalString.Str != str && !IsObjectOwnerOrBuiltIn(str));
        }

        private bool IsObjectOwnerOrBuiltIn(string str)
        {
            str = str.ToLower();
            switch (str)
            {
                case "true":
                case "false":
                case "waittillmatch":
                case "waittill":
                case "endon":
                case "notify":
                case "vectorscale":
                case "getnextarraykey":
                case "getfirstarraykey":
                case "undefined":
                case "anglestoright":
                case "anglestoforward":
                case "anglelamp180":
                case "vectortoangles":
                case "abs":
                case "gettime":
                case "anglestoup":
                case "getdvar":
                case "isdefined":
                case "anim":
                case "game":
                case "self":
                case "level":
                case "getdvarint":
                case "getdvarfloat":
                case "getdvarvector":
                case "getdvarcolorred":
                case "getdvarcolorgreen":
                case "getdvarcolorblue":
                case "getdvarcoloralpha":
                    return true;
                default:
                    return false;
            }
        }

        private void PrepareLocalVariables(ParseTreeNode node)
        {
            foreach (ParseTreeNode childNode in node.ChildNodes)
            {
                switch (childNode.Term.Name)
                {
                    case "setVariableField":
                        if (childNode.ChildNodes[0].ChildNodes[0].Term.Name == "identifier" &&
                            !LocalVariables.Contains(childNode.FindTokenAndGetValue()))
                            LocalVariables.Add(childNode.FindTokenAndGetValue());
                        break;
                    case "foreachStatement":
                        LocalVariables.Add(ArrayKeys[numofForeachStatements].first);
                        LocalVariables.Add(ArrayKeys[numofForeachStatements].second);
                        numofForeachStatements++;
                        if (!LocalVariables.Contains(childNode.ChildNodes[1].Token.ValueString))
                        {
                            LocalVariables.Add(childNode.ChildNodes[1].Token.ValueString);
                        }
                        PrepareLocalVariables(childNode);
                        break;

                    case "scriptMethodCall":
                        if (childNode.ChildNodes[1].FindTokenAndGetValue() == "waittill")
                        {
                            ParseWaittillVars(childNode.ChildNodes[1].ChildNodes[1].ChildNodes[0]);
                        }
                        break;

                    default:
                        PrepareLocalVariables(childNode);
                        break;
                }
            }
        }

        private void ParseWaittillVars(ParseTreeNode Node, bool compile = false)
        {
            foreach (ParseTreeNode parameter in Node.ChildNodes)
            {
                if (parameter.Term.Name == "identifier" && !LocalVariables.Contains(parameter.Token.ValueString) &&
                    !compile)
                {
                    LocalVariables.Add(parameter.FindTokenAndGetValue());
                }
                else if (parameter.Term.Name == "identifier" && compile)
                {
                    EvalLocalVariable(parameter.FindTokenAndGetValue(), false, true);
                }
                else
                {
                    ParseWaittillVars(parameter, compile);
                }
            }
        }

        private void PrepareStrings(ParseTreeNode node)
        {
            foreach (ParseTreeNode childNode in node.ChildNodes)
            {
                switch (childNode.Term.Name)
                {
                    case "identifier":
                        childNode.Token.Value = childNode.Token.ValueString.ToLower().Replace(@"\", "/");
                        if (!StringShouldBeWritten(childNode.Token.ValueString)) break;
                        AddString(childNode.Token.ValueString);
                        break;

                    case "stringLiteral":
                        if (!StringShouldBeWritten(childNode.Token.ValueString)) break;
                        AddString(childNode.Token.ValueString);
                        break;

                    case "foreachStatement":
                        var rnd = new Random();
                        int number = rnd.Next(1000);
                        string first = string.Format("_a{0}", number);
                        string second = string.Format("_k{0}", number);
                        ArrayKeys.Add(new Key { first = first, second = second });
                        AddString(first);
                        AddString(second);
                        PrepareStrings(childNode);
                        break;

                    case "hashedString":
                        break;

                    default:
                        PrepareStrings(childNode);
                        break;
                }
            }
        }

        public class AnimTree
        {
            public ushort NamePtr { get; set; }
            public ushort Unknown { get; set; }
            public ushort NumOfRefs { get; set; }
            public List<int> Refs { get; set; }
        }

        public class Call
        {
            public ushort NamePtr { get; set; }
            public ushort IncludePtr { get; set; }
            public ushort NumOfRefs { get; set; }
            public byte NumOfParams { get; set; }
            public byte Flag { get; set; }
            public List<int> Refs { get; set; }
        }

        public class Function
        {
            public uint Crc32 { get; set; }
            public int Start { get; set; }
            public ushort NamePtr { get; set; }
            public byte NumofParameters { get; set; }
            public byte Flag { get; set; }
        }

        public class GlobalString
        {
            public string Str { get; set; }
            public ushort Position { get; set; }
        }

        public class Key
        {
            public string first { get; set; }
            public string second { get; set; }
        }

        public class OP
        {
            public int Start { get; set; }
            public int JumpRangePos { get; set; }
        }

        public class RefString
        {
            public List<int> Refs = new List<int>();
            public ushort NamePtr { get; set; }
            public byte NumOfRefs { get; set; }
            public byte Type { get; set; }
        }

        public class Switch
        {
            public object Name { get; set; }
            public int Pos { get; set; }
            public int JumpRangePos { get; set; }
        }


    }
}
