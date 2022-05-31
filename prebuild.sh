#!/bin/sh
VER_STRING=$(git describe --dirty --tags)
VER_ASSEMBLY=$(echo $VER_STRING | sed -r -e 's/-g.*//' -e 's/-/./' -e 's/v//')
echo 'namespace Constants{public class Const{public const string GitVersion = "'$VER_STRING'";public const string AssemblyVersion="'$VER_ASSEMBLY'";}}' > ./ModelChanger/GitVersion.cs