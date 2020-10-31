﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Crpg.DumpItemsMod
{
    public class CrpgItemValueModel : DefaultItemValueModel
    {
        public override float GetEquipmentValueFromTier(float itemTierf)
        {
            double a = 0.05;
            double b = 50;
            double c = 100;
            return (float)(a * Math.Pow(itemTierf, 2) + b * itemTierf + c);
        }
    }
}
