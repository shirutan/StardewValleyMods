using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace SpaceCore.Content.Functions.StardewSpecific;
internal class ActorFunction : BaseFunction
{
    public ActorFunction()
    : base("Actor")
    {
    }

    public override SourceElement Simplify(FuncCall fcall, ContentEngine ce)
    {
        if (fcall.Parameters.Count != 3)
            throw new ArgumentException($"Actor function must have exactly three parameters (an actor name, a Vector2 position, and a Facing direction), at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");
        Token actorTok = fcall.Parameters[0].SimplifyToToken(ce);
        Token facingTok = fcall.Parameters[2].SimplifyToToken(ce);

        Block posBlock = fcall.Parameters[1].DoSimplify(ce) as Block;
        if (posBlock == null)
            throw new ArgumentException($"Actor function must have exactly three parameters (an actor name, a Vector2 position, and a Facing direction), at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

        return new Token()
        {
            FilePath = fcall.FilePath,
            Line = fcall.Line,
            Column = fcall.Column,
            Value = $"{actorTok.Value} {(posBlock.Contents[Vector2Function.XKey] as Token).Value} {(posBlock.Contents[Vector2Function.YKey] as Token).Value} {facingTok.Value}",
            IsString = true,
            Context = fcall.Context,
            Uid = fcall.Uid,
        };
    }
}
