#!/usr/bin/python

# quake_motion_path.py input_trace_file [output_dir]
#
# Generates motion path files in the same format as the Quake motion
# path files used in CBR. Each motion path is stored in a file named
# by the object UUID, stored in the specified output directory, or the
# current working directory if one isn't specified.

import sys
import os, os.path
from motion_path import MotionPath
from object_path import ObjectPathTrace

def main():
    if len(sys.argv) < 2:
        print "Specify a file."
        return -1

    trace_file = sys.argv[1]
    output_dir = os.curdir
    if len(sys.argv) > 2: output_dir = sys.argv[2]

    trace = ObjectPathTrace(sys.argv[1])
    trace.fill_parents(report=True)

    for objid in trace.roots():
        mot = trace.motion(objid).squeeze()
        if len(mot) <= 1: continue

        outfilename = os.path.join(output_dir, str(objid) + '.txt')
        with open(outfilename, 'w') as fout:
            idx = 0
            for t,pos in mot:
                # note that we flip y and z to go from SL coords -> meru coords
                line = "%d: %f, %f, %f, %d" % (idx, pos[0], pos[2], pos[1], int(t*1000))
                print >>fout, line
                idx += 1

    return 0

if __name__ == "__main__":
    sys.exit(main())
