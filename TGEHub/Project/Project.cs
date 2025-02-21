using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TGELoader.Project
{
    [DataContract(Name = "Game")]
    public class Project
    {
        public static string Extension { get; } = ".tgeloader";

        [DataMember] public string Name { get; private set; }
        [DataMember] public string Path { get; private set; }

        public string FullPath => $"{Path}{Name}{Extension}";

        public Project(string aName, string aPath)
        {
            Name = aName;
            Path = aPath;
        }
    }
}
