using System;
using System.Collections.Generic;
using System.Linq;
using SpaceShared;

namespace JsonAssets.Framework.ContentPatcher
{
    internal class IdToken : BaseToken
    {
        public IdToken(string type)
            : base(type, "Id")
        {
        }

        public override IEnumerable<string> GetValues(string input)
        { 
            if (!this.IsReady())
                return Array.Empty<string>();

            // Use FixIdJA to check if the item exists and if so, return the result
            if (Mod.instance.ItemTypes.Contains(this.Type))
            {
                if (input.FixIdJA(this.Type) != null)
                    return new[] { input.FixIdJA(this.Type) };
                else
                    return Array.Empty<string>();
            }

            // Handle clothing specially because it used to combine pants and shirts
            else if (this.Type == "Clothing")
            {
                if (input.FixIdJA("S") == null)
                {
                    if (input.FixIdJA("P") == null)
                        return Array.Empty<string>();
                    else
                        return new[] { input.FixIdJA("P") };
                }
                else
                    return new[] { input.FixIdJA("S") };
            }
            else
                return Array.Empty<string>();
        }

        protected override void UpdateContextImpl()
        {
        }
    }
}
