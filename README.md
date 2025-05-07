I learned to mod from AwesomeReminders, which includes links to Dinkum modding tutorials, at:
https://github.com/ETcodehome/DinkumAwesomeReminders.git

Useful Dinkum modding references:
https://modding.wiki/en/dinkum/developers
https://modding.wiki/en/dinkum/developers/decompiling
https://harmony.pardeike.net/articles/patching-prefix.html
https://www.nexusmods.com/dinkum/mods/83

How to get a decompiled copy of current Dinkum:
- Open ILSpy (see symlink in X:/dev/dinkum-mod/)
- Instruct ILSpy to open .../Steam/steamapps/common/Dinkum/Dinkum_Data/Managed/Assembly-CSharp.dll
- Right-click the resulting tree-menu entry for Assembly-CSharp > Save Code
- Create a new empty folder somewhere appropriate (for example: X:/dev/dinkum-mod/Dinkum-Release-v6.6.6/)
- Save the file as-is, with the default suggested filename, inside your blank folder. ILSpy will automatically also include every single file referenced by the file we targetted (this is why we started with Assembly-CSharp) AND build out the C# .csproj stuff.
- ???
- Profit!

How to build & test:
- Run `dotnet build` or use the provided build.sh file.
- dotnet will report where the .dll it created went to, probably ~/bin/Debug/netstandard2.0/x.dll, where ~/ is this directory, and x is the project name.
- Manually copy the created .dll into .../Steam/steamapps/common/Dinkum/BepInEx/plugins, probably overwriting the last version you tested.
- Run Dinkum, monitor the BepInEx console window for errors, check if the Dinkum gameplay changes as expected.