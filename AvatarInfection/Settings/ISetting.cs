using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvatarInfection.Settings
{
    public interface ISetting
    {
        public string Name { get; }

        public void Save();

        public void Load();
    }
}