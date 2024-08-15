using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceCore.Content.Functions;
internal class ChooseFunction : BaseFunction, IRefreshingFunction
{
    public override bool IsLateResolver => true;

    public ChooseFunction()
    :   base( "Choose" )
    {
    }

    public override SourceElement Simplify(FuncCall fcall, ContentEngine ce)
    {
        var firstParam = fcall.Parameters.ElementAtOrDefault(0)?.DoSimplify(ce, true);
        if (fcall.Parameters.Count != 1 || firstParam is not Array arr)
            throw new ArgumentException($"Choose function must have only an array parameter, at {fcall.FilePath}:{fcall.Line}:{fcall.Column}");

        return arr.Contents[ce.Random.Next(arr.Contents.Count)];
    }

    public bool WouldChangeFromRefresh(FuncCall fcall, PatchContentEngine ce)
    {
        return true;
    }
}
