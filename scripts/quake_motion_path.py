#!/usr/bin/python

# quake_motion_path.py input_trace_file [output_filename] [xoff] [yoff]
#
# Generates a motion path file in the same format as the Quake motion
# path files used in CBR. You may optionally specify the output
# filename and a fixed x and y offset Each motion path is stored in a
# file named by the object UUID, stored in the specified output
# directory, or the current working directory if one isn't specified.

import sys
import os, os.path
import math
import vec3
from motion_path import MotionPath
from object_path import ObjectPathTrace
from util.progress_bar import ProgressBar

def _get_option_or_default(args, idx, default):
    if len(args) < idx+1:
        return default
    return args[idx]

def generate_quake_motion_path(args):
    trace_file = args[0]      #input trace file
    output_filename = _get_option_or_default(args, 1, 'quake.txt')
    xoffset = int(_get_option_or_default(args, 2, 0)) # uniform x translation
    yoffset = int(_get_option_or_default(args, 3, 0)) # uniform y translation

    trace = ObjectPathTrace(trace_file)
    trace.fill_parents(report=True)
    pb = ProgressBar(len(trace.roots()))
    obj_sizes = trace.aggregate_sizes()

    fout = open(output_filename,'w')

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

            idx = 0
            for mot in mots:
                num_updates = sum( [ len(mot) for mot in mots ] )
                mot.squeeze(fudge=.05)
                #if num_updates <= 1: continue

                for t,pos in mot:
                    # note that we flip y and z to go from SL coords -> meru coords

                    bbox = obj_sizes[objid]
                    bbox_rad = vec3.dist(bbox[0], bbox[1])/2.0
                    line = "%s: %f, %f, %f, %d, %f" % (str(objid), xoffset+pos[0], yoffset+pos[1], pos[2], int(t*1000), bbox_rad)
                    print >>fout, line
                    idx += 1

    fout.close()
    pb.finish()

    return 0

def main():
    if len(sys.argv) < 2:
        print "Input file must be specified."
        return -1

    generate_quake_motion_path(sys.argv[1:])

if __name__ == "__main__":
    sys.exit(main())
