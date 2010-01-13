
default : .installed-libomv


.installed-libomv : .sysdepends
	./install-libomv.sh
	touch .installed-libomv

.sysdepends :
	sudo apt-get install mono-gmcs nant
	touch .sysdepends