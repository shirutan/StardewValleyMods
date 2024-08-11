using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using SpaceShared;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;

#if IS_SPACECORE
namespace SpaceCore.Content
{
#else
namespace SpaceShared.Content
{
#endif

    public static class ContentExtensions
    {
        private class CPTokenHolder
        {
            public string LastString { get; set; }
            public IManagedTokenString LastTokenString { get; set; }
        }

        private static ConditionalWeakTable<SourceElement, CPTokenHolder> elementTokens = new();

        public static Token SimplifyToToken(this SourceElement se, ContentEngine ce, bool allowLateResolve = false, bool require = true)
        {
            Token tok = se as Token;
            if (se is Statement statement)
            {
                var simplified = statement.FuncCall.Simplify(ce, allowLateResolve);
                if (allowLateResolve && simplified == null)
                    return null;
                tok = simplified as Token;
            }
            if (tok == null && require)
                throw new ArgumentException($"Source element must simplify to string at {se.FilePath}:{se.Line}:{se.Column}");
            return tok;
        }

        public static SourceElement DoSimplify(this SourceElement se, ContentEngine ce, bool allowLateResolve = false)
        {
            if (se is Statement statement)
            {
                var simplified = statement.FuncCall.Simplify(ce, allowLateResolve);
                if (allowLateResolve && simplified == null)
                    return null;
                return simplified;
            }
            return se;
        }

        public static SourceElement Simplify(this FuncCall fcall, ContentEngine ce, bool allowLateResolve = false)
        {
            if (fcall.Function == "^")
            {
                if (fcall.Parameters.Count < 1)
                    throw new ArgumentException($"Dynamic value function ^ must have one string parameter, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

                var tok = fcall.Parameters[0].SimplifyToToken(ce);
                if (!fcall.Context.Contents.TryGetValue(tok, out SourceElement se))
                {
                    if (fcall.Parameters.Count == 1)
                    {
                        se = new Token()
                        {
                            FilePath = fcall.FilePath,
                            Line = fcall.Line,
                            Column = fcall.Column,
                            Value = $"invalid dynamic value {tok.Value} @ {fcall.FilePath}:{fcall.Line}:{fcall.Column}",
                            IsString = true,
                            Context = fcall.Context,
                        };
                    }
                    else return fcall.Parameters[1];
                }

                return se;
            }
            else if (fcall.Function == "@" || fcall.Function == "@@")
            {
                if (fcall.Parameters.Count != 1)
                    throw new ArgumentException($"Asset path function @ must have exactly one string parameter, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

                string path = Path.Combine(fcall.Function == "@" ? ce.ContentRootFolder : ce.ContentRootFolderActual, Path.GetDirectoryName(fcall.Parameters[0].FilePath), fcall.Parameters[0].SimplifyToToken(ce).Value).Replace('\\', '/');
                List<string> pathParts = new(path.Split('/'));
                for (int i = 1; i < pathParts.Count; ++i)
                {
                    if (pathParts[i] == "..")
                    {
                        pathParts.RemoveAt(i);
                        pathParts.RemoveAt(i - 1);
                    }
                }
                path = string.Join('/', pathParts);

                return new Token()
                {
                    FilePath = fcall.FilePath,
                    Line = fcall.Line,
                    Column = fcall.Column,
                    Value = fcall.Function == "@" ? ce.Helper.ModContent.GetInternalAssetName(path).Name : path,
                    IsString = true,
                    Context = fcall.Context,
                };
            }
            else if (fcall.Function == "%")
            {
                if (fcall.Parameters.Count != 1)
                    throw new ArgumentException($"I18n function $ must have exactly one string parameter, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

                return new Token()
                {
                    FilePath = fcall.FilePath,
                    Line = fcall.Line,
                    Column = fcall.Column,
                    Value = ce.Helper.Translation.Get(fcall.Parameters[0].SimplifyToToken(ce).Value),
                    IsString = true,
                    Context = fcall.Context,
                };
            }
            else if (fcall.Function == "HasMod")
            {
                if (fcall.Parameters.Count != 1)
                    throw new ArgumentException($"HasMod function must have exactly one string parameters, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
                Token modTok = fcall.Parameters[0].SimplifyToToken(ce);

                return new Token()
                {
                    FilePath = fcall.FilePath,
                    Line = fcall.Line,
                    Column = fcall.Column,
                    Value = ce.Helper.ModRegistry.IsLoaded( modTok.Value ) ? "true" : "false",
                    IsString = true,
                    Context = fcall.Context,
                };
            }
            else if (fcall.Function == "Vector2")
            {
                if (fcall.Parameters.Count != 2)
                    throw new ArgumentException($"Vector2 function must have exactly two float parameters, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
                Token tokX = fcall.Parameters[0].SimplifyToToken(ce);
                Token tokY = fcall.Parameters[1].SimplifyToToken(ce);
                if (!float.TryParse(tokX.Value, out float x) || !float.TryParse(tokY.Value, out float y))
                    throw new ArgumentException($"Vector2 function must have exactly two float parameters, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

                return new Block()
                {
                    FilePath = fcall.FilePath,
                    Line = fcall.Line,
                    Column = fcall.Column,
                    Contents =
                    {
                        { new Token() { Value = "X", IsString = true }, tokX },
                        { new Token() { Value = "Y", IsString = true }, tokY },
                    },
                    Context = fcall.Context,
                };
            }
            else if (fcall.Function == "Rectangle")
            {
                if (fcall.Parameters.Count != 4)
                    throw new ArgumentException($"Rectangle function must have exactly four integer parameters, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
                Token tokX = fcall.Parameters[0].SimplifyToToken(ce);
                Token tokY = fcall.Parameters[1].SimplifyToToken(ce);
                Token tokW = fcall.Parameters[2].SimplifyToToken(ce);
                Token tokH = fcall.Parameters[3].SimplifyToToken(ce);
                if (!int.TryParse(tokX.Value, out int x) || !int.TryParse(tokY.Value, out int y) || !int.TryParse(tokX.Value, out int w) || !int.TryParse(tokY.Value, out int h))
                    throw new ArgumentException($"Rectangle function must have exactly four integer parameters, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

                return new Block()
                {
                    FilePath = fcall.FilePath,
                    Line = fcall.Line,
                    Column = fcall.Column,
                    Contents =
                    {
                        { new Token() { Value = "X", IsString = true }, tokX },
                        { new Token() { Value = "Y", IsString = true }, tokY },
                        { new Token() { Value = "Width", IsString = true }, tokW },
                        { new Token() { Value = "Height", IsString = true }, tokH },
                    },
                    Context = fcall.Context,
                };
            }
            else if (fcall.Function == "Color")
            {
                if (fcall.Parameters.Count < 3 || fcall.Parameters.Count > 4)
                    throw new ArgumentException($"Color function must have either three or four integer parameters, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
                Token tokR = fcall.Parameters[0].SimplifyToToken(ce);
                Token tokG = fcall.Parameters[1].SimplifyToToken(ce);
                Token tokB = fcall.Parameters[2].SimplifyToToken(ce);
                Token tokA = fcall.Parameters.Count == 4 ? fcall.Parameters[3].SimplifyToToken(ce) : new Token() { Value = "255", IsString = true };
                if (!int.TryParse(tokR.Value, out int r) || !int.TryParse(tokG.Value, out int g) || !int.TryParse(tokB.Value, out int b) || !int.TryParse(tokA.Value, out int a))
                    throw new ArgumentException($"Color function must have either three or four integer parameters, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

                return new Block()
                {
                    FilePath = fcall.FilePath,
                    Line = fcall.Line,
                    Column = fcall.Column,
                    Contents =
                    {
                        { new Token() { Value = "R", IsString = true }, tokR },
                        { new Token() { Value = "G", IsString = true }, tokG },
                        { new Token() { Value = "B", IsString = true }, tokB },
                        { new Token() { Value = "A", IsString = true }, tokA },
                    },
                    Context = fcall.Context,
                };
            }
            else if (fcall.Function == "Concatenate")
            {
                string str = "";
                foreach (var param in fcall.Parameters)
                {
                    str += param.SimplifyToToken(ce).Value;
                }

                return new Token()
                {
                    FilePath = fcall.FilePath,
                    Line = fcall.Line,
                    Column = fcall.Column,
                    Value = str,
                    IsString = true,
                    Context = fcall.Context,
                };
            }
            else if (fcall.Function == "Combine")
            {
                if (fcall.Parameters.Count == 0)
                {
                    return new Token()
                    {
                        FilePath = fcall.FilePath,
                        Line = fcall.Line,
                        Column = fcall.Column,
                        Value = "~", // null
                        Context = fcall.Context,
                    };
                }

                SourceElement ret = null;
                if (fcall.Parameters[0] is Array)
                {
                    var arr = new Array()
                    {
                        FilePath = fcall.FilePath,
                        Line = fcall.Line,
                        Column = fcall.Column,
                        Context = fcall.Context,
                    };

                    foreach (var param in fcall.Parameters)
                    {
                        arr.Contents.AddRange((param as Array).Contents);
                    }

                    ret = arr;
                }
                else if (fcall.Parameters[0] is Block)
                {
                    var block = new Block()
                    {
                        FilePath = fcall.FilePath,
                        Line = fcall.Line,
                        Column = fcall.Column,
                        Context = fcall.Context,
                    };

                    foreach (var param in fcall.Parameters)
                    {
                        foreach (var pair in (param as Block).Contents)
                            block.Contents.Add(pair.Key, pair.Value);
                    }

                    ret = block;
                }
                else
                {
                    ret = new Token()
                    {
                        FilePath = fcall.FilePath,
                        Line = fcall.Line,
                        Column = fcall.Column,
                        Value = "~", // null
                        Context = fcall.Context,
                    };
                }

                // TODO: Do I need to re-simplify here?

                return ret;
            }
            else if (allowLateResolve && fcall.Function == "Choose")
            {
                var firstParam = fcall.Parameters.ElementAtOrDefault(0)?.DoSimplify(ce, allowLateResolve);
                if (fcall.Parameters.Count != 1 || firstParam is not Array arr)
                    throw new ArgumentException($"Choose function must have only an array parameter, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

                return arr.Contents[ce.Random.Next(arr.Contents.Count)];
            }
            else if (allowLateResolve && fcall.Function == "ChooseWeighted")
            {
                var firstParam = fcall.Parameters.ElementAtOrDefault(0)?.DoSimplify(ce, allowLateResolve);
                if (firstParam is not Array arr)
                    throw new ArgumentException($"ChooseWeighted function must have an array parameter first, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

                if (fcall.Parameters.Count > 2)
                    throw new ArgumentException($"Too many parameters, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
                bool flatten = false;
                if (fcall.Parameters.Count == 2 && fcall.Parameters[1] is not Token { Value: "Flatten", IsString: true })
                    throw new ArgumentException($"Second argument to ChooseWeighted can only be \"Flatten\", at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
                else if (fcall.Parameters.Count == 2)
                    flatten = true;

                List<Weighted<SourceElement>> choices = new();
                foreach (var entry in arr.Contents)
                {
                    double weight = 1.0;
                    if (entry is Block block)
                    {
                        SourceElement weightElem = block.Contents.GetOrDefault(WeightKey, DefaultWeight);
                        Token weightTok = weightElem.SimplifyToToken(ce);
                        if (weightTok != null)
                        {
                            if (!double.TryParse(weightTok.Value, out weight))
                                Log.Warn($"Failed to parse weight value as number, at {weightTok.FilePath}:{weightTok.Line}:{weightTok.Column}");
                        }

                        if (flatten && block != null &&
                            ((weightTok == DefaultWeight && block.Contents.Count == 1) || (weightTok != DefaultWeight && block.Contents.Count == 2)))
                        {
                            SourceElement toAdd = block.Contents.First().Value;
                            if (toAdd == weightTok)
                                toAdd = block.Contents.Skip(1).First().Value;
                            choices.Add(new(weight, toAdd));
                        }
                        else
                        {
                            Block newBlock = new()
                            {
                                FilePath = block.FilePath,
                                Line = block.Line,
                                Column = block.Column,
                                Context = block.Context,
                            };
                            foreach (var entryValEntry in block.Contents)
                            {
                                if (entryValEntry.Value != weightElem)
                                    newBlock.Contents.Add(entryValEntry.Key, entryValEntry.Value);
                            }
                            choices.Add(new(weight, newBlock));
                        }
                    }
                    else
                    {
                        choices.Add(new(weight, entry));
                    }
                }

                return choices.Choose(ce.Random);
            }
            else if (allowLateResolve && fcall.Function == "FilterByCondition")
            {
                var firstParam = fcall.Parameters.ElementAtOrDefault(0)?.DoSimplify(ce, allowLateResolve);
                if (firstParam is not Array arr)
                    throw new ArgumentException($"Choose function must have an array parameter first, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
                if (fcall.Parameters.Count > 2)
                    throw new ArgumentException($"Too many parameters, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
                bool flatten = false;
                if (fcall.Parameters.Count == 2 && fcall.Parameters[1] is not Token { Value: "Flatten", IsString: true })
                    throw new ArgumentException($"Second argument to FilterByCondition can only be \"Flatten\", at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
                else if (fcall.Parameters.Count == 2)
                    flatten = true;

                Array ret = new()
                {
                    FilePath = fcall.FilePath,
                    Line = fcall.Line,
                    Column = fcall.Column,
                    Context = fcall.Context,
                };

                foreach (var entry in arr.Contents)
                {
                    Token cond = DefaultCondition;
                    Block entryVal = entry as Block;
                    SourceElement condElem = null;
                    if (entryVal != null && entryVal.Contents.TryGetValue(ConditionKey, out condElem))
                    {
                        cond = condElem.SimplifyToToken(ce, allowLateResolve);
                        if (cond == null && allowLateResolve)
                            return null;
                    }

                    if (ce.CheckCondition(cond))
                    {
                        if (flatten && entryVal != null &&
                            ((cond == DefaultCondition && entryVal.Contents.Count == 1) || (cond != DefaultCondition && entryVal.Contents.Count == 2)))
                        {
                            SourceElement toAdd = entryVal.Contents.First().Value;
                            if (toAdd == cond)
                                toAdd = entryVal.Contents.Skip(1).First().Value;
                            ret.Contents.Add(toAdd);
                        }
                        else if (entryVal != null)
                        {
                            Block newBlock = new()
                            {
                                FilePath = entryVal.FilePath,
                                Line = entryVal.Line,
                                Column = entryVal.Column,
                                Context = entryVal.Context,
                            };
                            foreach (var entryValEntry in entryVal.Contents)
                            {
                                if (entryValEntry.Value != cond)
                                    newBlock.Contents.Add(entryValEntry.Key, entryValEntry.Value);
                            }
                            ret.Contents.Add(newBlock);
                        }
                        else
                        {
                            ret.Contents.Add(entry);
                        }
                    }
                }

                return ret;
            }
            else if (allowLateResolve && fcall.Function == "CP" && ce is PatchContentEngine pce && pce.cp != null)
            {
                if (pce.cp == null)
                    throw new ArgumentException("Content Patcher API missing?");
                if (fcall.Parameters.Count != 1)
                    throw new ArgumentException($"CP function must have only 1 parameter, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

                var arg = fcall.Parameters[0];
                string argStr = arg.SimplifyToToken(ce).Value;
                var managedTok = elementTokens.GetOrCreateValue(arg);
                if (managedTok.LastString != argStr)
                {
                    managedTok.LastString = argStr;
                    managedTok.LastTokenString = pce.cp.ParseTokenString(pce.Manifest, argStr, pce.cpVersion);
                }
                managedTok.LastTokenString.UpdateContext();

                if (!managedTok.LastTokenString.IsValid)
                    throw new ArgumentException($"Invalid CP token string at {fcall.FilePath}:{fcall.Line}:{fcall.Column}: {managedTok.LastTokenString.ValidationError}");
                if (!managedTok.LastTokenString.IsReady)
                {
                    return null;
                }

                return new Token()
                {
                    FilePath = fcall.FilePath,
                    Line = fcall.Line,
                    Column = fcall.Column,
                    Value = managedTok.LastTokenString.Value,
                    IsString = true,
                    Context = fcall.Context,
                };
            }
            else if (ce.SimplifyExtensionFunctions.TryGetValue(fcall.Function, out var func))
            {
                return func(fcall, ce, allowLateResolve);
            }

            return fcall;
        }

        private static Token ConditionKey = new() { Value = "Condition", IsString = true };
        private static Token DefaultCondition = new() { Value = "true", IsString = true };
        private static Token WeightKey = new() { Value = "Weight", IsString = true };
        private static Token DefaultWeight = new() { Value = "1.0", IsString = true };
    }

    public class ContentEngine
    {
        protected internal IManifest Manifest { get; }
        protected internal IModHelper Helper { get; }

        public string ContentRootFolder { get; private set; }
        public string ContentRootFolderActual { get; private set; }
        public string ContentRootFile { get; }

        private ContentParser Parser { get; }

        protected Array Contents { get; set; }

        public delegate SourceElement SourceElementSimplifyFunction(FuncCall fcall, ContentEngine ce, bool allowLateResolve);
        internal Dictionary<string, SourceElementSimplifyFunction> SimplifyExtensionFunctions { get; } = new();
        public void AddSimplifyExtensionFunction(string name, SourceElementSimplifyFunction func)
        {
            SimplifyExtensionFunctions.Add(name, func);
        }

        public Random Random { get; } = new();

        public ContentEngine(IManifest manifest, IModHelper helper, string contentRootFile)
        {
            Manifest = manifest;
            Helper = helper;
            ContentRootFolder = Path.GetDirectoryName(contentRootFile);
            ContentRootFolderActual = Path.Combine(Helper.DirectoryPath, ContentRootFolder);
            ContentRootFile = contentRootFile;
            Parser = new(Manifest, Helper, ContentRootFolder);
        }

        public virtual bool CheckCondition(Token condition)
        {
            if (condition.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                return true;
            else
                return false;
        }

        public void OnReloadMonitorInstead(string pathModifier, [CallerFilePath] string path = "" )
        {
            if (path == "")
                throw new ArgumentException("No caller file path?");

            Parser.ContentRootFolderActual = ContentRootFolderActual = Path.Combine(Path.GetDirectoryName(path), pathModifier);
        }

        public void Reload()
        {
            Contents = (Array)RecursiveLoad(Path.GetFileName(ContentRootFile), flatten: false);
            PostReload();
        }

        protected virtual void PostReload()
        {
        }

        private SourceElement RecursiveLoad(string file, bool flatten = true, Block ctx = null )
        {
            ctx ??= new();

            Array contents = Parser.Load(file);
            RecursiveLoadImpl( contents, Path.GetDirectoryName( file ), flatten: false, ctx, out SourceElement se );
            contents = (Array)se;

            if (flatten && contents.Contents.Count == 1)
                return contents.Contents[0];

            return contents;
        }

        private bool RecursiveLoadImpl(SourceElement elem, string subfolder, bool flatten, Block ctx, out SourceElement replacement)
        {
            elem.Context = ctx;

            if (elem is Statement statement)
            {
                statement.FuncCall.Context = ctx;
                for (int i = 0; i < statement.FuncCall.Parameters.Count; ++i)
                {
                    statement.FuncCall.Parameters[i].Context = ctx;
                    RecursiveLoadImpl(statement.FuncCall.Parameters[i], subfolder, flatten: true, ctx, out var param);
                    statement.FuncCall.Parameters[i] = param;
                }
                if ( statement.Data != null )
                    statement.Data.Context = ctx;

                if (statement.FuncCall.Function == "Include")
                {
                    if (statement.FuncCall.Parameters.Count < 1 || statement.FuncCall.Parameters[0] is not Token token || !token.IsString)
                    {
                        throw new ArgumentException($"Include at {statement.FuncCall.FilePath}:{statement.FuncCall.Line}:{statement.FuncCall.Column} needs a string for the include path");
                    }
                    if (statement.FuncCall.Parameters.Count >= 2 && statement.FuncCall.Parameters[1] is not Block)
                    {
                        throw new ArgumentException($"Include context at {statement.FuncCall.FilePath}:{statement.FuncCall.Line}:{statement.FuncCall.Column} must be a block");
                    }

                    Block block = statement.FuncCall.Parameters.Count >= 2 ? statement.FuncCall.Parameters[1] as Block : null;

                    Block newCtx = new();
                    newCtx.Contents = new(ctx.Contents);
                    if (block != null)
                    {
                        foreach (var entry in block.Contents)
                        {
                            newCtx.Contents[entry.Key] = entry.Value;
                        }
                    }

                    replacement = RecursiveLoad(token.Value.StartsWith( '/' ) ? token.Value.Substring( 1 ) : Path.Combine(subfolder, token.Value), flatten, newCtx);
                    return true;
                }
                else if (statement.FuncCall.Function == "If")
                {
                    if (statement.FuncCall.Parameters.Count != 1)
                        throw new ArgumentException($"If at {statement.FuncCall.FilePath}:{statement.FuncCall.Line}:{statement.FuncCall.Column} needs a condition");

                    var tok = statement.FuncCall.Parameters[0].SimplifyToToken(this);
                    if (tok.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        RecursiveLoadImpl(statement.Data, subfolder, flatten, ctx, out replacement);
                    }
                    else
                    {
                        replacement = new Token()
                        {
                            FilePath = statement.FilePath,
                            Line = statement.Line,
                            Column = statement.Column,
                            Value = "~",
                            IsString = false,
                            Context = ctx, // Can't recall if this is right or not... probably doesn't matter in this case though.
                        };
                    }
                    return true;
                }
                else
                {
                    SourceElement se = statement.FuncCall.Simplify(this);
                    if (se as FuncCall == statement.FuncCall)
                    {
                        if (statement.Data != null)
                            RecursiveLoadImpl(statement.Data, subfolder, flatten: true, ctx, out se);
                        statement.Data = se;
                    }
                    else
                    {
                        replacement = se;
                        return true;
                    }
                }
            }
            else if ( elem is FuncCall fcall && fcall.Function == "Include" )
            {
                foreach (var param in fcall.Parameters)
                    param.Context = ctx;

                if ( fcall.Parameters.Count < 1 || fcall.Parameters[0] is not Token token || !token.IsString )
                {
                    throw new ArgumentException($"Include at {fcall.FilePath}:{fcall.Line}:{fcall.Column} needs a string for the include path");
                }
                if (fcall.Parameters.Count >= 2 && fcall.Parameters[1] is not Block)
                {
                    throw new ArgumentException($"Include context at {fcall.FilePath}:{fcall.Line}:{fcall.Column} must be a block");
                }

                Block block = fcall.Parameters.Count >= 2 ? fcall.Parameters[1] as Block : null;

                Block newCtx = new();
                newCtx.Contents = new(ctx.Contents);
                if (block != null)
                {
                    foreach (var entry in block.Contents)
                    {
                        newCtx.Contents[entry.Key] = entry.Value;
                    }
                }

                replacement = RecursiveLoad(Path.Combine(subfolder, token.Value), flatten: true, newCtx);
                return true;
            }
            else if ( elem is Block block )
            {
                foreach ( var key in block.Contents.Keys )
                {
                    block.Contents[key].Context = ctx;

                    RecursiveLoadImpl(block.Contents[key], subfolder, flatten: true, ctx, out SourceElement se);
                    block.Contents[key] = se;
                }
            }
            else if (elem is Array array)
            {
                for ( int i = 0; i < array.Contents.Count; ++i )
                {
                    array.Contents[i].Context = ctx;

                    if (RecursiveLoadImpl(array.Contents[i], subfolder, flatten: false, ctx, out SourceElement se ))
                    {
                        array.Contents.RemoveAt(i);
                        // array.Contents.InsertRange(i, (se as Array).Contents);
                        if (se is Array)
                        {
                            array.Contents.InsertRange(i, (se as Array).Contents);
                            i += (se as Array).Contents.Count - 1;
                        }
                        else
                        {
                            array.Contents.Insert(i, se);
                        }
                    }
                    else
                    {
                        array.Contents[i] = se;
                    }
                }
            }

            replacement = elem;
            return false;
        }
    }
}
