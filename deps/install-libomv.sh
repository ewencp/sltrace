#!/bin/bash

# checkout
svn co http://openmetaverse.org/svn/omf/libopenmetaverse/tags/0.7.0 libomv

# build
cd libomv
sh runprebuild.sh nant
nant
cd ..

# "install" process
rm -rf installed-libomv
mkdir installed-libomv
cp -r libomv/bin/ installed-libomv
