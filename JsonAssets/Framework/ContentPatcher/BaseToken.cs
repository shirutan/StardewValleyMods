using System;
using System.Collections.Generic;
using SpaceShared;

namespace JsonAssets.Framework.ContentPatcher
{
    internal abstract class BaseToken
    {
        /// CP at the moment (in the beta I got) doesn't like public getters
        internal string Type { get; }
        internal string TokenName { get; }
        private int OldGen = -1;

        public bool AllowsInput()
        {
            return true;
        }

        public bool RequiresInput()
        {
            return true;
        }

        public bool CanHaveMultipleValues(string input)
        {
            return false;
        }

        // List the dictionaries of names
        public virtual IEnumerable<string> GetValidInputs()
        {
            switch (this.Type)
            {
                case "O":
                    return Mod.instance.OldObjectIds.Values;
                case "Crop":
                    return Mod.instance.OldCropIds.Values;
                case "FruitTree":
                    return Mod.instance.OldFruitTreeIds.Values;
                case "BC":
                    return Mod.instance.OldBigCraftableIds.Values;
                case "W":
                    return Mod.instance.OldWeaponIds.Values;
                case "H":
                    return Mod.instance.OldHatIds.Values;
                case "S":
                case "P":
                case "Clothing":
                    return Mod.instance.OldClothingIds.Values;
                case "B":
                    return Mod.instance.OldBootsIds.Values;
            }
            return new string[0];
        }

        
        public virtual bool TryValidateInput(string input, out string error)
        {
            error = "";
            if (Mod.instance.ItemTypes.Contains(this.Type))
            {
                if (input.FixIdJA(this.Type) != null)
                    return true;
                else
                {
                    error = $"JA item with type {this.Type} and matching name not found";
                    return false;
                }
            }
            else if (this.Type == "Clothing")
            {
                if (input.FixIdJA("S") == null)
                {
                    if (input.FixIdJA("P") == null)
                    {
                        error = "JA item with type Clothing and matching name not found";
                        return false;
                    }
                    else
                        return true; ;
                }
                else
                    return true;
            }
            else
            {
                error = "Unknown item type";
                return false;
            }
        }

        public virtual bool IsReady()
        {
            return ContentPatcherIntegration.IdsAssigned;
        }

        public abstract IEnumerable<string> GetValues(string input);

        public virtual bool UpdateContext()
        {
            try
            {
                if (this.OldGen != ContentPatcherIntegration.IdsAssignedGen)
                {
                    this.OldGen = ContentPatcherIntegration.IdsAssignedGen;
                    this.UpdateContextImpl();
                    return true;
                }
            }
            catch (Exception e) { Log.Error("exception:"+e); throw e; }
            return false;
        }

        protected BaseToken(string type, string name)
        {
            switch (type)
            {
                case "Object":
                    this.Type = "O";
                    break;
                case "Crop":
                    this.Type = "Crop";
                    break;
                case "FruitTree":
                    this.Type = "FruitTree";
                    break;
                case "BigCraftable":
                    this.Type = "BC";
                    break;
                case "Hat":
                    this.Type = "H";
                    break;
                case "Weapon":
                    this.Type = "W";
                    break;
                case "Pants":
                    this.Type = "P";
                    break;
                case "Shirt":
                    this.Type = "S";
                    break;
                case "Boots":
                    this.Type = "B";
                    break;
                default:
                    this.Type = type;
                    break;
            }
            this.Type = type;
            this.TokenName = this.Type + name;
        }

        protected abstract void UpdateContextImpl();
    }
}
