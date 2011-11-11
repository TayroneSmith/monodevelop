#!/usr/bin/perl
use warnings;
use strict;
use File::Basename qw(dirname basename fileparse);
use File::Spec;
use File::Copy;
use File::Path;

my $root = File::Spec->rel2abs( dirname($0) );

my $gtkFileName = "gtk-2.24-bundle-osx";

chdir $root or die ("Failed to chdir to root dir");

if (!$ENV{UNITY_THISISABUILDMACHINE})
{
	rmtree "monodevelop";
	mkdir "monodevelop";
	system("tar xvfz MonoDevelop.tar.gz -C monodevelop");
}
rmtree "gtk";
system("unzip dependencies/$gtkFileName.zip");
system("mv gtk-2.24-bundle-osx gtk");

chdir "$root/monodevelop/main/build/MacOSX";
# Create MonoDevelop application bundle
system("make mono-bundle") && die ("Failed to make mono-bundle");
# Archive the app for placement in unity installer
unlink "$root/MonoDevelop.app.tar.gz", "$root/MonoDevelop.dmg", "MonoDevelop.app.tar.gz";
system("tar -pczf MonoDevelop.app.tar.gz --exclude=.svn MonoDevelop.app");
move "MonoDevelop.app.tar.gz", $root;
# Create seperate monodevelop installer as well
system("sh make-dmg-bundle.sh");
move "MonoDevelop.dmg", $root;
