#!/usr/bin/env python
#
# ascii command-line progress bar with percentage and elapsed time display
#
# adapted from Pylot source code (original by Vasil Vangelovski)
# modified by Corey Goldberg - 2010
# modified by Ewen Cheslack-Postava - 2010
#  (original at http://code.google.com/p/corey-projects/source/browse/trunk/python2/progress_bar.py)

import time
import sys

class ProgressBar:
    def __init__(self, duration):
        self.duration = duration
        self.prog_bar = '[]'
        self.fill_char = '#'
        self.width = 40
        self.__update_amount(0)

    def __update_amount(self, new_amount):
        percent_done = int(round((new_amount / 100.0) * 100.0))
        all_full = self.width - 2
        num_hashes = int(round((percent_done / 100.0) * all_full))
        self.prog_bar = '[' + self.fill_char * num_hashes + ' ' * (all_full - num_hashes) + ']'
        pct_place = (len(self.prog_bar) / 2) - len(str(percent_done))
        pct_string = '%i%%' % percent_done
        self.prog_bar = self.prog_bar[0:pct_place] + \
            (pct_string + self.prog_bar[pct_place + len(pct_string):])

    def update(self, finished):
        if (self.duration != 0):
            frac = (finished / float(self.duration))
        else:
            frac = 0
        self.__update_amount(frac * 100.0)
        self.prog_bar += '  %d/%s' % (finished, self.duration)

    def report(self, fp=None):
        if not fp: fp = sys.stdout
        print >>fp, '\r' + str(self.prog_bar),
        fp.flush()

    def finish(self, fp=None, mark_complete=True):
        if mark_complete:
            self.update(self.duration)
            self.report()

        # And clear to next line
        if not fp: fp = sys.stdout
        print >>fp, ''

if __name__ == '__main__':
    p = ProgressBar(60)
    for x in xrange(0,60):
        p.update(x)
        p.report()
        time.sleep(.5)
    p.finish()
