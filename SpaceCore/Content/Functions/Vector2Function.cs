using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceCore.Content.Functions;
internal class Vector2Function : BaseFunction
{
    public Vector2Function()
    : base("Vector2")
    {
    }

    public override SourceElement Simplify(FuncCall fcall, ContentEngine ce)
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
            Uid = fcall.Uid,
        };
    }
}
