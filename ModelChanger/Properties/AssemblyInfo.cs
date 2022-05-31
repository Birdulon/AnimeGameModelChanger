using System;
using System.Reflection;
using MelonLoader;
using ModelChanger;
using BuildInfo = ModelChanger.BuildInfo;
using static Constants.Const;

[assembly: AssemblyTitle(BuildInfo.Description)]
[assembly: AssemblyDescription(BuildInfo.Description)]
[assembly: AssemblyCompany(BuildInfo.Company)]
[assembly: AssemblyProduct(BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + BuildInfo.Author)]
[assembly: AssemblyTrademark(BuildInfo.Company)]
[assembly: AssemblyVersion(AssemblyVersion)]
[assembly: AssemblyFileVersion(AssemblyVersion)]
[assembly: MelonInfo(typeof(Loader), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author, BuildInfo.DownloadLink)]
[assembly: MelonColor(ConsoleColor.Red)]
[assembly: MelonGame(null, null)]