#!/bin/bash

# checkout
svn co http://openmetaverse.org/svn/omf/libopenmetaverse/tags/0.7.0 libomv

# build
cd libomv
sh runprebuild.sh nant
nant
cd ..

# "install" process
cp -r libomv/bin/* ../bin/
