using JetBrains.Annotations;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuffExtend
{
    public static class BuffExtend
    {
        /// <summary>
        /// 手动注册
        /// </summary>
        /// <param name="data"></param>
        public static void RegisterStaticData([NotNull] BuffStaticData data)
        {
            if (!BuffConfigManager.staticDatas.ContainsKey(data.BuffID))
            {
                BuffUtils.Log("BuffExtend", $"Register Buff:{data.BuffID} static data by Code ");
                BuffConfigManager.staticDatas.Add(data.BuffID, data);
                BuffConfigManager.buffTypeTable[data.BuffType].Add(data.BuffID);
            }
            else
                BuffUtils.Log("BuffExtend", $"already contains BuffID {data.BuffID}");
        }
    }
}
