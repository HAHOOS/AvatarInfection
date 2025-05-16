using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvatarInfection.Settings
{
    internal interface IServerSetting : ISetting
    {
        public bool IsSynced { get; }

        public void Sync();
    }
}