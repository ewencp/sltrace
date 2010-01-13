
default : installed-libomv


installed-libomv : .sysdepends
	./install-libomv.sh

.sysdepends :
	sudo apt-get install mono-gmcs nant
	touch .sysdepends