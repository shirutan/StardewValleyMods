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
using SpaceCore.Content.Functions;
using SpaceShared;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace SpaceCore.Content
{
    public static class ContentExtensions
    {

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
            if (ce.SimplifyFunctions.TryGetValue(fcall.Function, out var func))
            {
                if (!allowLateResolve && func.IsLateResolver)
                    return fcall;

                return func.Simplify(fcall, ce);
            }

            return fcall;
        }
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

        internal Dictionary<string, BaseFunction> SimplifyFunctions { get; } = new();
        public void AddSimplifyFunction(BaseFunction func)
        {
            SimplifyFunctions.Add(func.Name, func);
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

            AddSimplifyFunction(new AssetPathFunction(absolute: false));
            AddSimplifyFunction(new AssetPathFunction(absolute: true));
            AddSimplifyFunction(new ChooseFunction());
            AddSimplifyFunction(new ChooseWeightedFunction());
            AddSimplifyFunction(new ColorFunction());
            AddSimplifyFunction(new CombineFunction());
            AddSimplifyFunction(new ConcatenateFunction());
            AddSimplifyFunction(new ContextValueFunction());
            AddSimplifyFunction(new FilterByConditionFunction());
            AddSimplifyFunction(new HasModFunction());
            AddSimplifyFunction(new JoinFunction());
            AddSimplifyFunction(new LocalizationFunction());
            AddSimplifyFunction(new RectangleFunction());
            AddSimplifyFunction(new Vector2Function());
            AddSimplifyFunction(new RemoveFunction());
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

        private SourceElement RecursiveLoad(string file, bool flatten = true, Block ctx = null, string uidContext = "" )
        {
            ctx ??= new();

            Array contents = Parser.Load(file, uidContext);
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

                    replacement = RecursiveLoad(token.Value.StartsWith( '/' ) ? token.Value.Substring( 1 ) : Path.Combine(subfolder, token.Value), flatten, newCtx, statement.Uid);
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
                            Uid = statement.Uid,
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

                replacement = RecursiveLoad(Path.Combine(subfolder, token.Value), flatten: true, newCtx, elem.Uid);
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
