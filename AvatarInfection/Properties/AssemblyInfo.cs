using System.Reflection;
using System.Runtime.InteropServices;

using MelonLoader;

using AvatarInfection;

#region MelonLoader

[assembly: MelonInfo(typeof(Core), "AvatarInfection", Constants.Version, "HAHOOS", "https://thunderstore.io/c/bonelab/p/HAHOOS/AvatarInfection")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonColor(0, 0, 255, 0)]
[assembly: MelonAuthorColor(0, 255, 165, 0)]
[assembly: MelonOptionalDependencies("LabPresence")]

#endregion MelonLoader

#region Info

[assembly: AssemblyTitle("A fusion gamemode where you infect others and they turn into a selected avatar until the gamemode ends!")]
[assembly: AssemblyDescription("A fusion gamemode where you infect others and they turn into a selected avatar until the gamemode ends!")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("HAHOOS")]
[assembly: AssemblyProduct("AvatarInfection")]
[assembly: AssemblyCulture("")]

#region Version

[assembly: AssemblyVersion(Constants.Version)]
[assembly: AssemblyFileVersion(Constants.Version)]
[assembly: AssemblyInformationalVersion(Constants.Version)]

#endregion Version

#endregion Info

#region Other

[assembly: ComVisible(false)]
[assembly: Guid("4bf6c046-91bd-4503-93ca-5ef3b7d6c95e")]

#endregion Other