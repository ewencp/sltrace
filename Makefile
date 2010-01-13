
BIN_DIR=bin

SLTRACE_SOURCES=src/sltrace/Main.cs src/sltrace/AssemblyInfo.cs src/sltrace/Config.cs
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
	gmcs -lib:${SLTRACE_REFDIRS} -r:${SLTRACE_REFS} -out:${SLTRACE_BIN} ${SLTRACE_SOURCES}

sltrace : ${SLTRACE_BIN}
