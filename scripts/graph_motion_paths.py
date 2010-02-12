#!/usr/bin/python

# quake_motion_path.py input_trace_file [output_dir]
#
# Generates motion path files in the same format as the Quake motion
# path files used in CBR. Each motion path is stored in a file named
# by the object UUID, stored in the specified output directory, or the
# current working directory if one isn't specified.

import sys
from motion_path import MotionPath
from object_path import ObjectPathTrace

import matplotlib.path as mpath
import matplotlib.pyplot as plt
import util.colors as colors

def main():
    if len(sys.argv) < 2:
        print "Specify a file."
        return -1

    trace_file = sys.argv[1]

    trace = ObjectPathTrace(sys.argv[1])
    trace.fill_parents(report=True)

    motions = trace.sim_motions(trace.roots())

    Path = mpath.Path
    fig = plt.figure()
    ax = fig.add_subplot(111)

    for objid,mots in motions.items():
        col = colors.get_random_color()

        for mot in mots:
            mot.squeeze(fudge=0.05)
            if len(mot) <= 1: continue

            first_t, first_pos = mot[0]
            coord_seq = [ (pos[0],pos[1]) for t,pos in mot ]
            x,y = zip(*coord_seq)
            line, = ax.plot(x, y, color=col, marker='.')

    ax.grid()
    ax.set_xlim(0,256)
    ax.set_ylim(0,256)
    ax.set_title('object paths')
    plt.show()

    return 0

if __name__ == "__main__":
    sys.exit(main())
