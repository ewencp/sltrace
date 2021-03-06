
BIN_DIR=bin

SLTRACE_SOURCES=src/sltrace/Main.cs
SLTRACE_SOURCES+=src/sltrace/AssemblyInfo.cs
SLTRACE_SOURCES+=src/sltrace/Config.cs
SLTRACE_SOURCES+=src/sltrace/TraceSession.cs
SLTRACE_SOURCES+=src/sltrace/ITracer.cs
SLTRACE_SOURCES+=src/sltrace/TracerFactory.cs
SLTRACE_SOURCES+=src/sltrace/IController.cs
SLTRACE_SOURCES+=src/sltrace/ControllerFactory.cs
SLTRACE_SOURCES+=src/sltrace/util/JSON.cs
SLTRACE_SOURCES+=src/sltrace/util/Arguments.cs
SLTRACE_SOURCES+=src/sltrace/tracers/ObjectPathTracer.cs
SLTRACE_SOURCES+=src/sltrace/tracers/RawPacketTracer.cs
SLTRACE_SOURCES+=src/sltrace/controllers/StaticRotatingController.cs
SLTRACE_REFDIRS=bin/
SLTRACE_REFS=OpenMetaverse,OpenMetaverseTypes
SLTRACE_BIN=${BIN_DIR}/sltrace.exe

default :
	make ${BIN_DIR}
	make -C deps
	make sltrace

${BIN_DIR} :
	mkdir ${BIN_DIR}

${SLTRACE_BIN} : ${BIN_DIR} ${SLTRACE_SOURCES}
	gmcs -debug -lib:${SLTRACE_REFDIRS} -r:${SLTRACE_REFS} -out:${SLTRACE_BIN} ${SLTRACE_SOURCES}

sltrace : ${SLTRACE_BIN}

clean :
	rm -f ${SLTRACE_BIN}

docs : doc/Doxyfile doc/*.doc doc/*.html doc/*.css
	doxygen doc/Doxyfile