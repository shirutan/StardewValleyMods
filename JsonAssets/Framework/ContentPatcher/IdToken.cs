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

        public override IEnumerable<string> GetValidInputs()
        {
            return new string[0];
        }

        public override bool TryValidateInput(string input, out string error)
        {
            error = "";
            return true;
        }

        public override IEnumerable<string> GetValues(string input)
        { 
            if (!this.IsReady())
                return Array.Empty<string>();

            if (Mod.instance.ItemTypes.Contains(this.Type))
            {
                if (input.FixIdJA(this.Type) != null)
                    return new[] { input.FixIdJA(this.Type) };
                else
                    return Array.Empty<string>();
            }
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
