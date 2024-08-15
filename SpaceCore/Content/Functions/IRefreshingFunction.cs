using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceCore.Content.Functions;
public interface IRefreshingFunction
{
    public bool WouldChangeFromRefresh(FuncCall fcall, PatchContentEngine ce);
}
