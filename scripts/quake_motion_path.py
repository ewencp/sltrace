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
from util.progress_bar import ProgressBar

def main():
    if len(sys.argv) < 2:
        print "Specify a file."
        return -1

    trace_file = sys.argv[1]
    output_dir = os.curdir
    if len(sys.argv) > 2: output_dir = sys.argv[2]


    trace = ObjectPathTrace(sys.argv[1])
    trace.fill_parents(report=True)
    roots = trace.roots()

    pb = ProgressBar(len(roots))

    trace_subsets = trace.clusters()
    subtraces = trace.subset_traces(trace_subsets)

    obj_count = 0
    for subtrace in subtraces:
        subtrace_roots = subtrace.roots()

        for objid,mots in subtrace.sim_motions_iter(subtrace_roots):
            # above the actual output to ensure it gets updated
            obj_count += 1
            pb.update(obj_count)
            pb.report()

            for mot in mots:
                mot.squeeze(fudge=.05)

            num_updates = sum( [ len(mot) for mot in mots ] )
            if num_updates <= 1: continue

            outfilename = os.path.join(output_dir, str(objid) + '.txt')
            idx = 1
            with open(outfilename, 'w') as fout:
                for mot in mots:
                    for t,pos in mot:
                        # note that we flip y and z to go from SL coords -> meru coords
                        line = "%d: %f, %f, %f, %d" % (idx, pos[0], pos[2], pos[1], int(t*1000))
                        print >>fout, line

    pb.finish()

    return 0

if __name__ == "__main__":
    sys.exit(main())
