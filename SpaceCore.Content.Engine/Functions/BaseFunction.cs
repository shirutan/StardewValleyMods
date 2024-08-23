namespace SpaceCore.Content.Functions;

public abstract class BaseFunction
{
    public string Name { get; }
    public virtual bool IsLateResolver => false;

    public BaseFunction(string name)
    {
        this.Name = name;
    }

    public abstract SourceElement Simplify(FuncCall fcall, ContentEngine ce);
}
