using RandomBuff.Core.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuffExtend
{
    /// <summary>
    /// 动态创建Buff的基类
    /// </summary>
    public abstract class RuntimeBuff : IBuff
    {

        public abstract BuffID ID { get; }

        public virtual bool Active => true;

        public virtual bool Triggerable => false;

        public virtual bool Trigger(RainWorldGame game) => false;

        public BuffTimer MyTimer { get; set; }

        public virtual void Update(RainWorldGame game)
        {
            MyTimer?.Update(game);
        }

        public virtual void Destroy() { }


        protected RuntimeBuff() { }
    }

}
