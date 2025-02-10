using System.Reflection;
using System.Runtime.InteropServices;

using MelonLoader;

using AvatarInfection;

#region MelonLoader

[assembly: MelonInfo(typeof(Core), "AvatarInfection", Core.Version, "HAHOOS", "https://thunderstore.io/c/bonelab/p/HAHOOS/AvatarInfection")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonColor(0, 255, 0, 255)]
[assembly: MelonAuthorColor(0, 255, 165, 0)]

#endregion MelonLoader

#region Info

[assembly: AssemblyTitle("A fusion gamemode where you infect others and they turn into a selected avatar until the gamemode ends!")]
[assembly: AssemblyDescription("A fusion gamemode where you infect others and they turn into a selected avatar until the gamemode ends!")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("HAHOOS")]
[assembly: AssemblyProduct("AvatarInfection")]
[assembly: AssemblyCulture("")]

#region Version

[assembly: AssemblyVersion(Core.Version)]
[assembly: AssemblyFileVersion(Core.Version)]
[assembly: AssemblyInformationalVersion(Core.Version)]

#endregion Version

#endregion Info

#region Other

[assembly: ComVisible(false)]
[assembly: Guid("4bf6c046-91bd-4503-93ca-5ef3b7d6c95e")]

#endregion Other